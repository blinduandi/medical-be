using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using medical_be.Data;
using medical_be.Models;
using medical_be.DTOs;
using medical_be.Controllers.Base;
using AutoMapper;

namespace medical_be.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SingleNotificationController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public SingleNotificationController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // GET all notifications
        [HttpGet]
        public async Task<IActionResult> GetAllNotifications()
        {
            var notifications = await _context.Notifications
                .Include(n => n.Campaign)
                .ToListAsync();

            return Ok(notifications);
        }

        // GET notification by ID
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetNotification(long id)
        {
            var notification = await _context.Notifications
                .Include(n => n.Campaign)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (notification == null)
                return NotFound();

            return Ok(notification);
        }

        // CREATE notification from AppointmentDto
        [HttpPost("appointment")]
        public async Task<IActionResult> CreateFromAppointment([FromBody] AppointmentDto dto)
        {
            var patient = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == dto.PatientId);
            if (patient == null || string.IsNullOrEmpty(patient.Email))
            {
                return NotFound("Patient not found or has no email.");
            }
            var notification = new SingleNotification
            {
                Title = $"Appointment with Dr. {dto.DoctorName}",
                Body = $"You have an appointment on {dto.AppointmentDate:dd/MM/yyyy HH:mm}.",
                ToEmail = patient.Email, // Replace with actual patient email if available
                Status = "waiting_for_sending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ScheduledAt = dto.AppointmentDate.AddDays(-1) // Notify 1 hour before appointment
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Ok(notification);
        }

        // CREATE notification from VisitRecordDto (instead of HealthDTO)
        [HttpPost("visit-record")]
        public async Task<IActionResult> CreateFromVisitRecord([FromBody] VisitRecordDto dto)
        {
            // Look up the patient by ID
            var patient = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == dto.PatientId);
            if (patient == null || string.IsNullOrEmpty(patient.Email))
            {
                return NotFound("Patient not found or has no email.");
            }

            var notification = new SingleNotification
            {
                Title = "New Visit Record",
                Body = $"You have a new visit record dated {dto.VisitDate:dd/MM/yyyy}. Diagnosis: {dto.Diagnosis}",
                ToEmail = patient.Email, // <-- actual email here
                Status = "waiting_for_sending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Ok(notification);
        }


        // CREATE notification from RegisterDto (instead of AuthDto)
        [HttpPost("registration")]
        public async Task<IActionResult> CreateFromRegistration([FromBody] RegisterDto dto)
        {

            var notification = new SingleNotification
            {
                Title = "Welcome to MedTrack!",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Body = $@"
                    <h2>Welcome to Medical System!</h2>
                    <p>Dear {dto.FirstName} {dto.LastName},</p>
                    <p>Thank you for registering with our medical system. Your account has been created successfully.</p>
                    <p><strong>Your Details:</strong></p>
                    <ul>
                        <li>Email: {dto.Email}</li>
                        <li>Registration Date: {DateTime.UtcNow:MMMM dd, yyyy}</li>
                    </ul>
                    <p>You can now:</p>
                    <ul>
                        <li>Schedule appointments with doctors</li>
                        <li>View your medical records</li>
                        <li>Manage your profile</li>
                    </ul>
                    <p>If you have any questions, please contact our support team.</p>
                    <p>Best regards,<br>Medical System Team</p>
                ",   // <-- use the full body
                ToEmail = dto.Email,
                Status = "waiting_for_sending",
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Ok(notification);
        }


    }
}
