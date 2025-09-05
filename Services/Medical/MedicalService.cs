using medical_be.DTOs;
using medical_be.Shared.Interfaces;
using medical_be.Models;
using medical_be.Data;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace medical_be.Services.Medical;

public class MedicalService : IMedicalService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<MedicalService> _logger;

    public MedicalService(
        ApplicationDbContext context,
        IMapper mapper,
        IAuditService auditService,
        INotificationService notificationService,
        ILogger<MedicalService> logger)
    {
        _context = context;
        _mapper = mapper;
        _auditService = auditService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<AppointmentDto> CreateAppointmentAsync(CreateAppointmentDto appointmentDto)
    {
        try
        {
            // Validate patient and doctor exist
            var patient = await _context.Users.FindAsync(appointmentDto.PatientId);
            var doctor = await _context.Users.FindAsync(appointmentDto.DoctorId);

            if (patient == null)
                throw new KeyNotFoundException("Patient not found");
            if (doctor == null)
                throw new KeyNotFoundException("Doctor not found");

            // Check for conflicts
            var conflictingAppointment = await _context.Appointments
                .Where(a => a.DoctorId == appointmentDto.DoctorId &&
                           a.AppointmentDate.Date == appointmentDto.AppointmentDate.Date &&
                           Math.Abs((a.AppointmentDate - appointmentDto.AppointmentDate).TotalMinutes) < 30 &&
                           a.Status != AppointmentStatus.Cancelled)
                .FirstOrDefaultAsync();

            if (conflictingAppointment != null)
                throw new InvalidOperationException("Doctor is not available at the requested time");

            var appointment = new Appointment
            {
                PatientId = appointmentDto.PatientId,
                DoctorId = appointmentDto.DoctorId,
                AppointmentDate = appointmentDto.AppointmentDate,
                Duration = appointmentDto.Duration,
                Reason = appointmentDto.Reason,
                Notes = appointmentDto.Notes,
                Status = AppointmentStatus.Scheduled
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            var appointmentDtoResult = await GetAppointmentByIdAsync(appointment.Id);

            // Log audit
            await _auditService.LogActivityAsync("APPOINTMENT_CREATED", Guid.Empty,
                $"Appointment created with Dr. {doctor.FirstName} {doctor.LastName}",
                "Appointment", Guid.Empty);

            // Send notifications
            await _notificationService.SendAppointmentReminderAsync(appointment.Id);

            // Publish event
            _logger.LogInformation("Appointment created: {AppointmentId}", appointment.Id);
            return appointmentDtoResult!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating appointment");
            throw;
        }
    }

    public async Task<AppointmentDto> UpdateAppointmentAsync(int id, UpdateAppointmentDto appointmentDto)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null)
            throw new KeyNotFoundException("Appointment not found");

        if (appointmentDto.AppointmentDate.HasValue)
            appointment.AppointmentDate = appointmentDto.AppointmentDate.Value;
        if (appointmentDto.Duration.HasValue)
            appointment.Duration = appointmentDto.Duration.Value;
        if (!string.IsNullOrWhiteSpace(appointmentDto.Reason))
            appointment.Reason = appointmentDto.Reason;
        if (!string.IsNullOrWhiteSpace(appointmentDto.Notes))
            appointment.Notes = appointmentDto.Notes;
        if (appointmentDto.Status.HasValue)
            appointment.Status = appointmentDto.Status.Value;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogActivityAsync("APPOINTMENT_UPDATED", Guid.Empty,
            $"Appointment {id} updated", "Appointment", Guid.Empty);

        return (await GetAppointmentByIdAsync(id))!;
    }

    public async Task<List<AppointmentDto>> GetUserAppointmentsAsync(string userId)
    {
        var appointments = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.PatientId == userId || a.DoctorId == userId)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync();

        return appointments.Select(a => new AppointmentDto
        {
            Id = a.Id,
            PatientId = a.PatientId,
            DoctorId = a.DoctorId,
            PatientName = $"{a.Patient.FirstName} {a.Patient.LastName}",
            DoctorName = $"{a.Doctor.FirstName} {a.Doctor.LastName}",
            AppointmentDate = a.AppointmentDate,
            Duration = a.Duration,
            Status = a.Status,
            Reason = a.Reason,
            Notes = a.Notes,
            CreatedAt = a.CreatedAt
        }).ToList();
    }

    public async Task<List<AppointmentDto>> GetDoctorAppointmentsAsync(string doctorId)
    {
        var appointments = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Where(a => a.DoctorId == doctorId)
            .OrderBy(a => a.AppointmentDate)
            .ToListAsync();

        return appointments.Select(a => new AppointmentDto
        {
            Id = a.Id,
            PatientId = a.PatientId,
            DoctorId = a.DoctorId,
            PatientName = $"{a.Patient.FirstName} {a.Patient.LastName}",
            DoctorName = $"{a.Doctor.FirstName} {a.Doctor.LastName}",
            AppointmentDate = a.AppointmentDate,
            Duration = a.Duration,
            Status = a.Status,
            Reason = a.Reason,
            Notes = a.Notes,
            CreatedAt = a.CreatedAt
        }).ToList();
    }

    public async Task<AppointmentDto?> GetAppointmentByIdAsync(int id)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            return null;

        return new AppointmentDto
        {
            Id = appointment.Id,
            PatientId = appointment.PatientId,
            DoctorId = appointment.DoctorId,
            PatientName = $"{appointment.Patient.FirstName} {appointment.Patient.LastName}",
            DoctorName = $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}",
            AppointmentDate = appointment.AppointmentDate,
            Duration = appointment.Duration,
            Status = appointment.Status,
            Reason = appointment.Reason,
            Notes = appointment.Notes,
            CreatedAt = appointment.CreatedAt
        };
    }

    public async Task<bool> CancelAppointmentAsync(int id)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            return false;

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogActivityAsync("APPOINTMENT_CANCELLED", Guid.Empty,
            $"Appointment {id} cancelled", "Appointment", Guid.Empty);

        // Publish event
        return true;
    }
}
