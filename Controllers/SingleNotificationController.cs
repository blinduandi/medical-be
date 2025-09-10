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
            var notification = new SingleNotification
            {
                Title = $"Appointment with Dr. {dto.DoctorName}",
                Body = $"You have an appointment on {dto.AppointmentDate:dd/MM/yyyy HH:mm}.",
                ToEmail = dto.PatientId, // Replace with actual patient email if available
                Status = "waiting_for_sending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Ok(notification);
        }

        // CREATE notification from VisitRecordDto (instead of HealthDTO)
        [HttpPost("visit-record")]
        public async Task<IActionResult> CreateFromVisitRecord([FromBody] VisitRecordDto dto)
        {
            var notification = new SingleNotification
            {
                Title = "New Visit Record",
                Body = $"You have a new visit record dated {dto.VisitDate:dd/MM/yyyy}. Diagnosis: {dto.Diagnosis}",
                ToEmail = dto.PatientId, // Replace with actual patient email if available
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
                Body = $"Hello {dto.FirstName} {dto.LastName}, your registration was successful.",
                ToEmail = dto.Email,
                Status = "waiting_for_sending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Ok(notification);
        }

    }
}
