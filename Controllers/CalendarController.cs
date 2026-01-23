using System.Security.Claims;
using medical_be.Controllers.Base;
using medical_be.Data;
using medical_be.DTOs;
using medical_be.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace medical_be.Controllers;

[Route("api/[controller]")]
[Authorize]
public class CalendarController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CalendarController> _logger;

    public CalendarController(
        ApplicationDbContext context,
        ILogger<CalendarController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get calendar events (appointments) for a date range
    /// </summary>
    /// <param name="start">Start date of the calendar view</param>
    /// <param name="end">End date of the calendar view</param>
    /// <param name="doctorId">Optional filter by doctor ID</param>
    /// <param name="patientId">Optional filter by patient ID</param>
    /// <param name="status">Optional filter by appointment status</param>
    /// <returns>Calendar view with events and summary</returns>
    [HttpGet]
    public async Task<IActionResult> GetCalendar(
        [FromQuery] DateTime? start,
        [FromQuery] DateTime? end,
        [FromQuery] string? doctorId,
        [FromQuery] string? patientId,
        [FromQuery] AppointmentStatus? status)
    {
        try
        {
            // Default to current month if no dates provided
            var startDate = start ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var endDate = end ?? startDate.AddMonths(1).AddDays(-1);

            // Ensure end date includes the full day
            endDate = endDate.Date.AddDays(1).AddTicks(-1);

            var query = _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.AppointmentDate >= startDate && a.AppointmentDate <= endDate)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(doctorId))
                query = query.Where(a => a.DoctorId == doctorId);
            if (!string.IsNullOrWhiteSpace(patientId))
                query = query.Where(a => a.PatientId == patientId);
            if (status.HasValue)
                query = query.Where(a => a.Status == status);

            var appointments = await query
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            var events = appointments.Select(a => new CalendarEventDto
            {
                Id = a.Id,
                Title = $"{a.Patient.FirstName} {a.Patient.LastName} - {a.Reason ?? "Appointment"}",
                Start = a.AppointmentDate,
                End = a.AppointmentDate.Add(a.Duration),
                PatientId = a.PatientId,
                DoctorId = a.DoctorId,
                PatientName = $"{a.Patient.FirstName} {a.Patient.LastName}",
                DoctorName = $"{a.Doctor.FirstName} {a.Doctor.LastName}",
                Specialty = a.Doctor.Specialty.ToString(),
                Status = a.Status,
                Reason = a.Reason,
                Notes = a.Notes,
                Color = GetStatusColor(a.Status),
                AllDay = false
            }).ToList();

            var summary = new CalendarSummaryDto
            {
                Scheduled = appointments.Count(a => a.Status == AppointmentStatus.Scheduled),
                Confirmed = appointments.Count(a => a.Status == AppointmentStatus.Confirmed),
                InProgress = appointments.Count(a => a.Status == AppointmentStatus.InProgress),
                Completed = appointments.Count(a => a.Status == AppointmentStatus.Completed),
                Cancelled = appointments.Count(a => a.Status == AppointmentStatus.Cancelled),
                NoShow = appointments.Count(a => a.Status == AppointmentStatus.NoShow)
            };

            var calendarView = new CalendarViewDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalAppointments = appointments.Count,
                Events = events,
                Summary = summary
            };

            return SuccessResponse(calendarView, "Calendar retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving calendar");
            return InternalServerErrorResponse("An error occurred while retrieving calendar data");
        }
    }

    /// <summary>
    /// Get calendar events for the current user (doctor or patient)
    /// </summary>
    /// <param name="start">Start date of the calendar view</param>
    /// <param name="end">End date of the calendar view</param>
    /// <param name="status">Optional filter by appointment status</param>
    /// <returns>Calendar view with events for the current user</returns>
    [HttpGet("my")]
    public async Task<IActionResult> GetMyCalendar(
        [FromQuery] DateTime? start,
        [FromQuery] DateTime? end,
        [FromQuery] AppointmentStatus? status)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return UnauthorizedResponse("User not found");

            _logger.LogInformation("Calendar request for userId: {UserId}", userId);

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFoundResponse("User not found");

            var isDoctor = user.UserRoles.Any(ur => ur.Role.Name == "Doctor");
            _logger.LogInformation("User {UserId} is doctor: {IsDoctor}, Roles: {Roles}", 
                userId, isDoctor, string.Join(", ", user.UserRoles.Select(ur => ur.Role.Name)));

            // Default to current month if no dates provided
            var startDate = start ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var endDate = end ?? startDate.AddMonths(1).AddDays(-1);
            endDate = endDate.Date.AddDays(1).AddTicks(-1);

            _logger.LogInformation("Searching appointments between {Start} and {End}", startDate, endDate);

            // First check total appointments without user filter
            var totalAppointments = await _context.Appointments
                .Where(a => a.AppointmentDate >= startDate && a.AppointmentDate <= endDate)
                .CountAsync();
            _logger.LogInformation("Total appointments in date range: {Count}", totalAppointments);

            var query = _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.AppointmentDate >= startDate && a.AppointmentDate <= endDate)
                .AsQueryable();

            // Show appointments where user is either doctor or patient
            _logger.LogInformation("Filtering by userId as DoctorId OR PatientId: {UserId}", userId);
            query = query.Where(a => a.DoctorId == userId || a.PatientId == userId);

            if (status.HasValue)
                query = query.Where(a => a.Status == status);

            var appointments = await query
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            _logger.LogInformation("Found {Count} appointments for user {UserId}", appointments.Count, userId);

            var events = appointments.Select(a => new CalendarEventDto
            {
                Id = a.Id,
                Title = isDoctor 
                    ? $"{a.Patient.FirstName} {a.Patient.LastName} - {a.Reason ?? "Appointment"}"
                    : $"Dr. {a.Doctor.FirstName} {a.Doctor.LastName} - {a.Reason ?? "Appointment"}",
                Start = a.AppointmentDate,
                End = a.AppointmentDate.Add(a.Duration),
                PatientId = a.PatientId,
                DoctorId = a.DoctorId,
                PatientName = $"{a.Patient.FirstName} {a.Patient.LastName}",
                DoctorName = $"{a.Doctor.FirstName} {a.Doctor.LastName}",
                Specialty = a.Doctor.Specialty.ToString(),
                Status = a.Status,
                Reason = a.Reason,
                Notes = a.Notes,
                Color = GetStatusColor(a.Status),
                AllDay = false
            }).ToList();

            var summary = new CalendarSummaryDto
            {
                Scheduled = appointments.Count(a => a.Status == AppointmentStatus.Scheduled),
                Confirmed = appointments.Count(a => a.Status == AppointmentStatus.Confirmed),
                InProgress = appointments.Count(a => a.Status == AppointmentStatus.InProgress),
                Completed = appointments.Count(a => a.Status == AppointmentStatus.Completed),
                Cancelled = appointments.Count(a => a.Status == AppointmentStatus.Cancelled),
                NoShow = appointments.Count(a => a.Status == AppointmentStatus.NoShow)
            };

            var calendarView = new CalendarViewDto
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalAppointments = appointments.Count,
                Events = events,
                Summary = summary
            };

            return SuccessResponse(calendarView, "Calendar retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user calendar");
            return InternalServerErrorResponse("An error occurred while retrieving calendar data");
        }
    }

    /// <summary>
    /// Get calendar events for a specific day
    /// </summary>
    /// <param name="date">The date to get appointments for</param>
    /// <param name="doctorId">Optional filter by doctor ID</param>
    /// <param name="patientId">Optional filter by patient ID</param>
    /// <returns>List of calendar events for the specified day</returns>
    [HttpGet("day")]
    public async Task<IActionResult> GetDayCalendar(
        [FromQuery] DateTime date,
        [FromQuery] string? doctorId,
        [FromQuery] string? patientId)
    {
        try
        {
            var startOfDay = date.Date;
            var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

            var query = _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.AppointmentDate >= startOfDay && a.AppointmentDate <= endOfDay)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(doctorId))
                query = query.Where(a => a.DoctorId == doctorId);
            if (!string.IsNullOrWhiteSpace(patientId))
                query = query.Where(a => a.PatientId == patientId);

            var appointments = await query
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            var events = appointments.Select(a => new CalendarEventDto
            {
                Id = a.Id,
                Title = $"{a.Patient.FirstName} {a.Patient.LastName} - {a.Reason ?? "Appointment"}",
                Start = a.AppointmentDate,
                End = a.AppointmentDate.Add(a.Duration),
                PatientId = a.PatientId,
                DoctorId = a.DoctorId,
                PatientName = $"{a.Patient.FirstName} {a.Patient.LastName}",
                DoctorName = $"{a.Doctor.FirstName} {a.Doctor.LastName}",
                Specialty = a.Doctor.Specialty.ToString(),
                Status = a.Status,
                Reason = a.Reason,
                Notes = a.Notes,
                Color = GetStatusColor(a.Status),
                AllDay = false
            }).ToList();

            return SuccessResponse(new
            {
                Date = date.Date,
                TotalAppointments = events.Count,
                Events = events
            }, "Day calendar retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving day calendar");
            return InternalServerErrorResponse("An error occurred while retrieving day calendar data");
        }
    }

    /// <summary>
    /// Get calendar events for a specific week
    /// </summary>
    /// <param name="date">Any date within the desired week</param>
    /// <param name="doctorId">Optional filter by doctor ID</param>
    /// <param name="patientId">Optional filter by patient ID</param>
    /// <returns>Calendar view with events for the specified week</returns>
    [HttpGet("week")]
    public async Task<IActionResult> GetWeekCalendar(
        [FromQuery] DateTime date,
        [FromQuery] string? doctorId,
        [FromQuery] string? patientId)
    {
        try
        {
            // Get start of week (Monday)
            var dayOfWeek = (int)date.DayOfWeek;
            var startOfWeek = date.Date.AddDays(-(dayOfWeek == 0 ? 6 : dayOfWeek - 1));
            var endOfWeek = startOfWeek.AddDays(7).AddTicks(-1);

            var query = _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.AppointmentDate >= startOfWeek && a.AppointmentDate <= endOfWeek)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(doctorId))
                query = query.Where(a => a.DoctorId == doctorId);
            if (!string.IsNullOrWhiteSpace(patientId))
                query = query.Where(a => a.PatientId == patientId);

            var appointments = await query
                .OrderBy(a => a.AppointmentDate)
                .ToListAsync();

            var events = appointments.Select(a => new CalendarEventDto
            {
                Id = a.Id,
                Title = $"{a.Patient.FirstName} {a.Patient.LastName} - {a.Reason ?? "Appointment"}",
                Start = a.AppointmentDate,
                End = a.AppointmentDate.Add(a.Duration),
                PatientId = a.PatientId,
                DoctorId = a.DoctorId,
                PatientName = $"{a.Patient.FirstName} {a.Patient.LastName}",
                DoctorName = $"{a.Doctor.FirstName} {a.Doctor.LastName}",
                Specialty = a.Doctor.Specialty.ToString(),
                Status = a.Status,
                Reason = a.Reason,
                Notes = a.Notes,
                Color = GetStatusColor(a.Status),
                AllDay = false
            }).ToList();

            var summary = new CalendarSummaryDto
            {
                Scheduled = appointments.Count(a => a.Status == AppointmentStatus.Scheduled),
                Confirmed = appointments.Count(a => a.Status == AppointmentStatus.Confirmed),
                InProgress = appointments.Count(a => a.Status == AppointmentStatus.InProgress),
                Completed = appointments.Count(a => a.Status == AppointmentStatus.Completed),
                Cancelled = appointments.Count(a => a.Status == AppointmentStatus.Cancelled),
                NoShow = appointments.Count(a => a.Status == AppointmentStatus.NoShow)
            };

            return SuccessResponse(new CalendarViewDto
            {
                StartDate = startOfWeek,
                EndDate = endOfWeek,
                TotalAppointments = appointments.Count,
                Events = events,
                Summary = summary
            }, "Week calendar retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving week calendar");
            return InternalServerErrorResponse("An error occurred while retrieving week calendar data");
        }
    }

    /// <summary>
    /// Get available time slots for a doctor on a specific date
    /// </summary>
    /// <param name="doctorId">Doctor ID</param>
    /// <param name="date">Date to check availability</param>
    /// <param name="durationMinutes">Appointment duration in minutes (default 30)</param>
    /// <returns>List of available time slots</returns>
    [HttpGet("availability")]
    public async Task<IActionResult> GetDoctorAvailability(
        [FromQuery] string doctorId,
        [FromQuery] DateTime date,
        [FromQuery] int durationMinutes = 30)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(doctorId))
                return ValidationErrorResponse("Doctor ID is required");

            var doctor = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == doctorId);
            
            if (doctor == null || !doctor.UserRoles.Any(ur => ur.Role.Name == "Doctor"))
                return NotFoundResponse("Doctor not found");

            var startOfDay = date.Date.AddHours(8);  // Assuming work starts at 8 AM
            var endOfDay = date.Date.AddHours(18);   // Assuming work ends at 6 PM
            var duration = TimeSpan.FromMinutes(durationMinutes);

            // Get existing appointments for the day
            var existingAppointments = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.DoctorId == doctorId 
                    && a.AppointmentDate >= date.Date 
                    && a.AppointmentDate < date.Date.AddDays(1)
                    && a.Status != AppointmentStatus.Cancelled)
                .OrderBy(a => a.AppointmentDate)
                .Select(a => new { Start = a.AppointmentDate, End = a.AppointmentDate.Add(a.Duration) })
                .ToListAsync();

            // Calculate available slots
            var availableSlots = new List<object>();
            var currentSlot = startOfDay;

            while (currentSlot.Add(duration) <= endOfDay)
            {
                var slotEnd = currentSlot.Add(duration);
                var isAvailable = !existingAppointments.Any(a => 
                    (currentSlot >= a.Start && currentSlot < a.End) ||
                    (slotEnd > a.Start && slotEnd <= a.End) ||
                    (currentSlot <= a.Start && slotEnd >= a.End));

                if (isAvailable)
                {
                    availableSlots.Add(new
                    {
                        Start = currentSlot,
                        End = slotEnd,
                        FormattedTime = currentSlot.ToString("HH:mm") + " - " + slotEnd.ToString("HH:mm")
                    });
                }

                currentSlot = currentSlot.AddMinutes(30); // 30-minute intervals
            }

            return SuccessResponse(new
            {
                DoctorId = doctorId,
                DoctorName = $"{doctor.FirstName} {doctor.LastName}",
                Date = date.Date,
                TotalSlots = availableSlots.Count,
                AvailableSlots = availableSlots
            }, "Availability retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving doctor availability");
            return InternalServerErrorResponse("An error occurred while retrieving availability");
        }
    }

    /// <summary>
    /// Get color based on appointment status for calendar display
    /// </summary>
    private static string GetStatusColor(AppointmentStatus status)
    {
        return status switch
        {
            AppointmentStatus.Scheduled => "#3B82F6",   // Blue
            AppointmentStatus.Confirmed => "#10B981",   // Green
            AppointmentStatus.InProgress => "#F59E0B",  // Amber
            AppointmentStatus.Completed => "#6B7280",   // Gray
            AppointmentStatus.Cancelled => "#EF4444",   // Red
            AppointmentStatus.NoShow => "#8B5CF6",      // Purple
            _ => "#6B7280"                               // Default gray
        };
    }
}
