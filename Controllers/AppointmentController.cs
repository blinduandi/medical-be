using System.Security.Claims;
using AutoMapper;
using medical_be.Controllers.Base;
using medical_be.Data;
using medical_be.DTOs;
using medical_be.Extensions;
using medical_be.Models;
using medical_be.Shared.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using medical_be.Services;

namespace medical_be.Controllers;

[Route("api/[controller]")]
[Authorize]
public class AppointmentController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IAuditService _auditService;
    private readonly ILogger<AppointmentController> _logger;
    private readonly INotificationService _notificationService;
    private readonly EmailTemplateService _emailTemplateService;

    public AppointmentController(
        ApplicationDbContext context,
        IMapper mapper,
        IAuditService auditService,
        ILogger<AppointmentController> logger,
        INotificationService notificationService,
        EmailTemplateService emailTemplateService)
    {
        _context = context;
        _mapper = mapper;
        _auditService = auditService;
        _logger = logger;
        _notificationService = notificationService;
        _emailTemplateService = emailTemplateService;
    }

    /// <summary>
    /// List appointments with optional filters and pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAppointments(
        [FromQuery] string? doctorId,
        [FromQuery] string? patientId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] AppointmentStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var query = _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(doctorId))
                query = query.Where(a => a.DoctorId == doctorId);
            if (!string.IsNullOrWhiteSpace(patientId))
                query = query.Where(a => a.PatientId == patientId);
            if (from.HasValue)
                query = query.Where(a => a.AppointmentDate >= from.Value);
            if (to.HasValue)
                query = query.Where(a => a.AppointmentDate <= to.Value);
            if (status.HasValue)
                query = query.Where(a => a.Status == status);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.AppointmentDate)
                .Skip((Math.Max(page, 1) - 1) * Math.Clamp(pageSize, 1, 200))
                .Take(Math.Clamp(pageSize, 1, 200))
                .Select(a => new AppointmentDto
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    DoctorId = a.DoctorId,
                    PatientName = a.Patient.FirstName + " " + a.Patient.LastName,
                    DoctorName = a.Doctor.FirstName + " " + a.Doctor.LastName,
                    Specialty = a.Doctor.Specialty.ToString(),
                    AppointmentDate = a.AppointmentDate,
                    Duration = a.Duration,
                    Status = a.Status,
                    Reason = a.Reason,
                    Notes = a.Notes,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return PaginatedResponse(items, page, pageSize, total, "Appointments retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing appointments");
            return InternalServerErrorResponse("Failed to retrieve appointments");
        }
    }

    /// <summary>
    /// Get appointment by id
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var appt = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appt == null)
                return NotFoundResponse("Appointment not found");

            var dto = new AppointmentDto
            {
                Id = appt.Id,
                PatientId = appt.PatientId,
                DoctorId = appt.DoctorId,
                PatientName = appt.Patient.FirstName + " " + appt.Patient.LastName,
                DoctorName = appt.Doctor.FirstName + " " + appt.Doctor.LastName,
                Specialty = appt.Doctor.Specialty.ToString(),
                AppointmentDate = appt.AppointmentDate,
                Duration = appt.Duration,
                Status = appt.Status,
                Reason = appt.Reason,
                Notes = appt.Notes,
                CreatedAt = appt.CreatedAt
            };

            return SuccessResponse(dto, "Appointment retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting appointment {AppointmentId}", id);
            return InternalServerErrorResponse("Failed to retrieve appointment");
        }
    }

    /// <summary>
    /// Create a new appointment
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Doctor,Admin,Patient")]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto)
    {
        try
        {
            var modelValidation = ValidateModel();
            if (modelValidation != null) return modelValidation;

            // Validate users exist
            var patientExists = await _context.Users.AnyAsync(u => u.Id == dto.PatientId);
            var doctorExists = await _context.Users.AnyAsync(u => u.Id == dto.DoctorId);
            if (!patientExists || !doctorExists) return ValidationErrorResponse("Invalid PatientId or DoctorId");

            // Prevent overlapping for doctor
            var start = dto.AppointmentDate;
            var end = dto.AppointmentDate + (dto.Duration == default ? TimeSpan.FromMinutes(30) : dto.Duration);
            // Get relevant appointments from database
            var doctorAppointments = await _context.Appointments
                .Where(a => a.DoctorId == dto.DoctorId &&
                            a.Status != AppointmentStatus.Cancelled &&
                            a.Status != AppointmentStatus.Completed)
                .ToListAsync(); // <-- bring them to memory

            // Check for overlap in memory using TimeSpan
            var overlaps = doctorAppointments.Any(a =>
                a.AppointmentDate < end &&
                a.AppointmentDate + a.Duration > start
            );
            if (overlaps)
                return ValidationErrorResponse("The doctor already has an appointment in this time range");
            

            var appt = new Appointment
            {
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                AppointmentDate = dto.AppointmentDate,
                Duration = dto.Duration == default ? TimeSpan.FromMinutes(30) : dto.Duration,
                Status = AppointmentStatus.Scheduled,
                Reason = dto.Reason,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appt);
            await _context.SaveChangesAsync();

            // Automatically link patient to doctor if not already linked
            var existingRelationship = await _context.PatientDoctors
                .FirstOrDefaultAsync(pd => pd.PatientId == dto.PatientId && pd.DoctorId == dto.DoctorId);

            if (existingRelationship == null)
            {
                var patientDoctorLink = new PatientDoctor
                {
                    PatientId = dto.PatientId,
                    DoctorId = dto.DoctorId,
                    AssignedDate = DateTime.UtcNow,
                    IsActive = true,
                    AssignedBy = "Appointment",
                    Notes = $"Automatically linked via appointment on {dto.AppointmentDate:yyyy-MM-dd}"
                };

                _context.PatientDoctors.Add(patientDoctorLink);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Created patient-doctor relationship: PatientId={PatientId}, DoctorId={DoctorId}", 
                    dto.PatientId, dto.DoctorId);
            }
            else if (!existingRelationship.IsActive)
            {
                // Reactivate if it was previously deactivated
                existingRelationship.IsActive = true;
                existingRelationship.DeactivatedDate = null;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Reactivated patient-doctor relationship: PatientId={PatientId}, DoctorId={DoctorId}", 
                    dto.PatientId, dto.DoctorId);
            }

            var patient = await _context.Users.FindAsync(dto.PatientId);
            var doctor = await _context.Users.FindAsync(dto.DoctorId);

            if (patient != null && !string.IsNullOrEmpty(patient.Email) && doctor != null)
            {
                var placeholders = new Dictionary<string, string>
                {
                    { "patient.FirstName", patient.FirstName },
                    { "patient.LastName", patient.LastName },
                    { "doctor.FirstName", doctor.FirstName },
                    { "doctor.LastName", doctor.LastName },
                    { "doctor.Specialty", doctor.Specialty.ToString() },
                    { "AppointmentDate", appt.AppointmentDate.ToString("f") },
                    { "Reason", appt.Reason ?? "N/A" },
                    { "Notes", appt.Notes ?? "N/A" }
                };

                var body = await _emailTemplateService.GetTemplateAsync("AppointmentConfEmail.html", placeholders);

                await _notificationService.SendEmailAsync(patient.Email, "Appointment Confirmation", body);

                var bodyDoctor = await _emailTemplateService.GetTemplateAsync("AppointmentConfEmailDoctor.html", placeholders);
                await _notificationService.SendEmailAsync(doctor.Email, "Appointment Scheduled", bodyDoctor);
                // reminders
                // 1 day before
                _context.Notifications.Add(new SingleNotification
                {
                    Title = "Appointment reminder",
                    ToEmail = patient.Email,
                    Status = "waiting_for_sending",
                    Body = await _emailTemplateService.GetTemplateAsync("PatientAppReminder.html", placeholders),
                    ScheduledAt = appt.AppointmentDate.AddDays(-1) // 1 day before
                });

                // // 30 minutes before
                // _context.Notifications.Add(new SingleNotification
                // {
                //     Title = "Appointment reminder - 1 hour before",
                //     ToEmail = patient.Email,
                //     Status = "waiting_for_sending",
                //     Body = await _emailTemplateService.GetTemplateAsync("PatientAppReminder.html", placeholders),
                //     ScheduledAt = appt.AppointmentDate.AddHours(-1) // 1 day before
                // });
                _logger.LogInformation("Loading email template: {Template}", "AppointmentConfEmail.html");

            }
            await _auditService.LogAuditAsync(User.GetUserId(), "AppointmentCreated", $"Created appointment for patient {dto.PatientId} (AppointmentId: {appt.Id})", "Appointment", null, Request.GetClientIpAddress());

            // Use the patient and doctor already loaded for email to build response
            var responseDto = new AppointmentDto
            {
                Id = appt.Id,
                PatientId = appt.PatientId,
                DoctorId = appt.DoctorId,
                PatientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : "",
                DoctorName = doctor != null ? $"{doctor.FirstName} {doctor.LastName}" : "",
                Specialty = doctor != null ? doctor.Specialty.ToString() : "",
                AppointmentDate = appt.AppointmentDate,
                Duration = appt.Duration,
                Status = appt.Status,
                Reason = appt.Reason,
                Notes = appt.Notes,
                CreatedAt = appt.CreatedAt
            };

            return SuccessResponse(responseDto, "Appointment created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment");
            return InternalServerErrorResponse("Failed to create appointment");
        }
    }

    /// <summary>
    /// Update an appointment
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAppointmentDto dto)
    {
        try
        {
            var modelValidation = ValidateModel();
            if (modelValidation != null) return modelValidation;

            var appt = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id);
            if (appt == null) return NotFoundResponse("Appointment not found");

            var newDate = dto.AppointmentDate ?? appt.AppointmentDate;
            var newDuration = dto.Duration ?? appt.Duration;

            if (dto.AppointmentDate.HasValue || dto.Duration.HasValue)
            {
                // Overlap check for doctor
                var start = newDate;
                var end = newDate + newDuration;
                var overlaps = await _context.Appointments.AnyAsync(a =>
                    a.Id != id &&
                    a.DoctorId == appt.DoctorId &&
                    a.Status != AppointmentStatus.Cancelled &&
                    a.Status != AppointmentStatus.Completed &&
                    a.AppointmentDate < end &&
                    (a.AppointmentDate + a.Duration) > start);
                if (overlaps)
                    return ValidationErrorResponse("The doctor already has an appointment in this time range");
            }

            appt.AppointmentDate = newDate;
            appt.Duration = newDuration;
            if (dto.Status.HasValue) appt.Status = dto.Status.Value;
            if (dto.Reason != null) appt.Reason = dto.Reason;
            if (dto.Notes != null) appt.Notes = dto.Notes;
            appt.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _auditService.LogAuditAsync(User.GetUserId(), "AppointmentUpdated", $"Updated appointment {id}", "Appointment", null, Request.GetClientIpAddress());

            // Load the updated appointment with navigation properties for response
            var updatedAppt = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id);

            var responseDto = new AppointmentDto
            {
                Id = updatedAppt!.Id,
                PatientId = updatedAppt.PatientId,
                DoctorId = updatedAppt.DoctorId,
                PatientName = updatedAppt.Patient.FirstName + " " + updatedAppt.Patient.LastName,
                DoctorName = updatedAppt.Doctor.FirstName + " " + updatedAppt.Doctor.LastName,
                Specialty = updatedAppt.Doctor.Specialty.ToString(),
                AppointmentDate = updatedAppt.AppointmentDate,
                Duration = updatedAppt.Duration,
                Status = updatedAppt.Status,
                Reason = updatedAppt.Reason,
                Notes = updatedAppt.Notes,
                CreatedAt = updatedAppt.CreatedAt
            };

            return SuccessResponse(responseDto, "Appointment updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating appointment {AppointmentId}", id);
            return InternalServerErrorResponse("Failed to update appointment");
        }
    }

    /// <summary>
    /// Update appointment status
    /// </summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Doctor,Admin,Patient")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] StatusChangeRequest request)
    {
        try
        {
            if (request == null) return ValidationErrorResponse("Invalid payload");
            var appt = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id);
            if (appt == null) return NotFoundResponse("Appointment not found");

            // Basic authorization: patients can only update their own; doctors their own; admins any
            var currentUserId = User.GetUserId();
            var isAdmin = User.IsInRole("Admin");
            var isDoctor = User.IsInRole("Doctor") && appt.DoctorId == currentUserId;
            var isPatient = User.IsInRole("Patient") && appt.PatientId == currentUserId;
            if (!(isAdmin || isDoctor || isPatient))
                return Forbid();

            appt.Status = request.Status;
            appt.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditService.LogAuditAsync(currentUserId, "AppointmentStatusChanged", $"Changed status of appointment {id} to {request.Status}", "Appointment", null, Request.GetClientIpAddress());

            return SuccessResponse(null, "Appointment status updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing appointment status {AppointmentId}", id);
            return InternalServerErrorResponse("Failed to change status");
        }
    }

    /// <summary>
    /// Cancel an appointment (soft cancel by setting status)
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Doctor,Admin, Patient")]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            var appt = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == id);
            if (appt == null) return NotFoundResponse("Appointment not found");

            appt.Status = AppointmentStatus.Cancelled;
            appt.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _auditService.LogAuditAsync(User.GetUserId(), "AppointmentCancelled", $"Cancelled appointment {id}", "Appointment", null, Request.GetClientIpAddress());

            return SuccessResponse(null, "Appointment cancelled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling appointment {AppointmentId}", id);
            return InternalServerErrorResponse("Failed to cancel appointment");
        }
    }

    /// <summary>
    /// Get appointments for current patient
    /// </summary>
    [HttpGet("my")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetMyAppointments([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            var userId = User.GetUserId();
            var query = _context.Appointments
                .AsNoTracking()
                .Include(a => a.Doctor)
                .Where(a => a.PatientId == userId);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.AppointmentDate)
                .Skip((Math.Max(page, 1) - 1) * Math.Clamp(pageSize, 1, 200))
                .Take(Math.Clamp(pageSize, 1, 200))
                .Select(a => new AppointmentDto
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    DoctorId = a.DoctorId,
                    PatientName = string.Empty,
                    DoctorName = a.Doctor.FirstName + " " + a.Doctor.LastName,
                    Specialty = a.Doctor.Specialty.ToString(),
                    AppointmentDate = a.AppointmentDate,
                    Duration = a.Duration,
                    Status = a.Status,
                    Reason = a.Reason,
                    Notes = a.Notes,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return PaginatedResponse(items, page, pageSize, total, "My appointments retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my appointments");
            return InternalServerErrorResponse("Failed to retrieve appointments");
        }
    }

    /// <summary>
    /// Get appointments for current doctor
    /// </summary>
    [HttpGet("doctor/schedule")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> GetDoctorSchedule([FromQuery] DateTime? day = null)
    {
        try
        {
            var doctorId = User.GetUserId();
            var date = (day ?? DateTime.UtcNow).Date;
            var nextDay = date.AddDays(1);

            var items = await _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == doctorId && a.AppointmentDate >= date && a.AppointmentDate < nextDay)
                .OrderBy(a => a.AppointmentDate)
                .Select(a => new AppointmentDto
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    DoctorId = a.DoctorId,
                    PatientName = a.Patient.FirstName + " " + a.Patient.LastName,
                    DoctorName = string.Empty,
                    Specialty = string.Empty,
                    AppointmentDate = a.AppointmentDate,
                    Duration = a.Duration,
                    Status = a.Status,
                    Reason = a.Reason,
                    Notes = a.Notes,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return SuccessResponse(items, "Appointments retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting doctor schedule");
            return InternalServerErrorResponse("Failed to retrieve schedule");
        }
    }

    /// <summary>
    /// Get all appointments for the current doctor
    /// </summary>
    [HttpGet("doctor/all")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> GetAllDoctorAppointments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var doctorId = User.GetUserId();

            var query = _context.Appointments
                .AsNoTracking()
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == doctorId);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(a => a.AppointmentDate)
                .Skip((Math.Max(page, 1) - 1) * Math.Clamp(pageSize, 1, 200))
                .Take(Math.Clamp(pageSize, 1, 200))
                .Select(a => new AppointmentDto
                {
                    Id = a.Id,
                    PatientId = a.PatientId,
                    DoctorId = a.DoctorId,
                    PatientName = a.Patient.FirstName + " " + a.Patient.LastName,
                    DoctorName = string.Empty, // doctor is the current user, no need to repeat
                    Specialty = string.Empty,
                    AppointmentDate = a.AppointmentDate,
                    Duration = a.Duration,
                    Status = a.Status,
                    Reason = a.Reason,
                    Notes = a.Notes,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return PaginatedResponse(items, page, pageSize, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all doctor appointments");
            return InternalServerErrorResponse("Failed to retrieve appointments");
        }
    }

    /// <summary>
    /// Complete appointment and create medical record (Doctor only, within 1 hour after appointment)
    /// </summary>
    [HttpPost("{id:int}/complete")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> CompleteAppointmentWithRecord(int id, [FromBody] CreateMedicalRecordDto medicalRecordDto)
    {
        try
        {
            var doctorId = User.GetUserId();
            
            // Get appointment with navigation properties
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.Id == id && a.DoctorId == doctorId);

            if (appointment == null)
                return NotFoundResponse("Appointment not found or you don't have permission");

            // Check if appointment is already completed or cancelled
            if (appointment.Status == AppointmentStatus.Completed)
                return ValidationErrorResponse("Appointment is already completed");

            if (appointment.Status == AppointmentStatus.Cancelled)
                return ValidationErrorResponse("Cannot complete a cancelled appointment");

            var now = DateTime.UtcNow;
            var appointmentEndTime = appointment.AppointmentDate.Add(appointment.Duration);

            // Check if appointment time has passed
            if (now < appointmentEndTime)
                return ValidationErrorResponse("Cannot complete appointment before it ends");

            // Check if within 1 hour window after appointment end
            var oneHourAfterEnd = appointmentEndTime.AddHours(1);
            if (now > oneHourAfterEnd)
                return ValidationErrorResponse("The window to complete this appointment has closed. You can only add medical records within 1 hour after the appointment ends.");

            // Create medical record
            var medicalRecord = new MedicalRecord
            {
                PatientId = appointment.PatientId,
                DoctorId = doctorId,
                AppointmentId = appointment.Id,
                Diagnosis = medicalRecordDto.Diagnosis,
                Symptoms = medicalRecordDto.Symptoms,
                Treatment = medicalRecordDto.Treatment,
                Prescription = medicalRecordDto.Prescription,
                Notes = medicalRecordDto.Notes,
                RecordDate = medicalRecordDto.RecordDate ?? now,
                CreatedAt = now
            };

            _context.MedicalRecords.Add(medicalRecord);

            // Update appointment status to completed
            appointment.Status = AppointmentStatus.Completed;
            appointment.UpdatedAt = now;

            await _context.SaveChangesAsync();

            await _auditService.LogAuditAsync(doctorId, "AppointmentCompleted", $"Completed appointment {id} and created medical record", "Appointment", null, Request.GetClientIpAddress());

            return SuccessResponse(new 
            { 
                appointmentId = appointment.Id,
                medicalRecordId = medicalRecord.Id,
                message = "Appointment completed and medical record created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing appointment {AppointmentId}", id);
            return InternalServerErrorResponse("Failed to complete appointment");
        }
    }

    /// <summary>
    /// Check if appointment can be completed (is within 1 hour window)
    /// </summary>
    [HttpGet("{id:int}/can-complete")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> CanCompleteAppointment(int id)
    {
        try
        {
            var doctorId = User.GetUserId();
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.Id == id && a.DoctorId == doctorId);

            if (appointment == null)
                return NotFoundResponse("Appointment not found");

            var now = DateTime.UtcNow;
            var appointmentEndTime = appointment.AppointmentDate.Add(appointment.Duration);
            var oneHourAfterEnd = appointmentEndTime.AddHours(1);

            var canComplete = appointment.Status != AppointmentStatus.Completed &&
                            appointment.Status != AppointmentStatus.Cancelled &&
                            now >= appointmentEndTime &&
                            now <= oneHourAfterEnd;

            var minutesRemaining = canComplete ? (int)(oneHourAfterEnd - now).TotalMinutes : 0;

            return SuccessResponse(new
            {
                canComplete,
                status = appointment.Status.ToString(),
                appointmentEndTime,
                windowExpiresAt = oneHourAfterEnd,
                minutesRemaining,
                isPast = now >= appointmentEndTime,
                message = canComplete 
                    ? $"You have {minutesRemaining} minutes to complete this appointment" 
                    : "Appointment cannot be completed at this time"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking appointment completion status {AppointmentId}", id);
            return InternalServerErrorResponse("Failed to check appointment status");
        }
    }


    public class StatusChangeRequest
    {
        public AppointmentStatus Status { get; set; }
    }
}
