using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using medical_be.Data;
using medical_be.Models;
using medical_be.DTOs;
using medical_be.Services;
using medical_be.Shared.Interfaces;
using medical_be.Extensions;
using AutoMapper;


namespace medical_be.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DoctorController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IAuditService _auditService;
        private readonly ILogger<DoctorController> _logger;
        private readonly IAuthService _authService;

        public DoctorController(
            ApplicationDbContext context,
            IMapper mapper,
            IAuditService auditService,
            ILogger<DoctorController> logger,
            IAuthService authService)
        {
            _context = context;
            _mapper = mapper;
            _auditService = auditService;
            _logger = logger;
            _authService = authService;
        }

        // GET: api/doctors
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DoctorProfileDto>>> GetDoctors()
        {
            var doctors = await _context.Users
                .Where(u => u.UserRoles.Any(r => r.Role.Name == "Doctor"))
                .Select(u => new DoctorProfileDto
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Email = u.Email ?? string.Empty,
                    PhoneNumber = u.PhoneNumber ?? string.Empty,
                    IDNP = u.IDNP,
                    ClinicId = u.ClinicId,
                    TotalPatients = 0,
                    LastActivity = null,
                    DateOfBirth = u.DateOfBirth,
                    Address = u.Address,
                    IsActive = u.IsActive
                })
                .ToListAsync();

            if (doctors == null || doctors.Count == 0)
            {
                _logger.LogWarning("No doctors found in the system.");
                return NotFound("No doctors found");
            }
            return Ok(doctors);
        }

        /// <summary>
        /// Search doctors by IDNP
        /// </summary>
        [HttpPost("search")]
        //   [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SearchByIDNP([FromBody] DoctorSearchDto searchDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchDto.IDNP))
                {
                    return BadRequest("IDNP is required");
                }

                var doctor = await _context.Users
                    .Where(u => u.IDNP == searchDto.IDNP)
                    .Select(u => new DoctorProfileDto
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email ?? string.Empty,
                        PhoneNumber = u.PhoneNumber ?? string.Empty,
                        IDNP = u.IDNP,
                        ClinicId = u.ClinicId,
                        TotalPatients = 0,
                        LastActivity = null,
                        DateOfBirth = u.DateOfBirth,
                        Address = u.Address,
                        IsActive = u.IsActive
                    })
                    .FirstOrDefaultAsync();

                if (doctor == null)
                {
                    return NotFound("Doctor not found");
                }

                // Audit log
                await _auditService.LogAuditAsync(User.GetUserId(), "DoctorSearch", $"Searched doctor with IDNP: {searchDto.IDNP}", "Doctor", null, Request.GetClientIpAddress());

                return Ok(doctor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching doctor by IDNP: {IDNP}", searchDto.IDNP);
                return StatusCode(500, "Internal server error");
            }
        }
        

        // POST: api/Doctor
        [HttpPost("CreateDoctor")]
        public async Task<IActionResult> CreateDoctor([FromBody] DoctorCreateDto dto)
        {
            try
            {
                // Map DoctorCreateDto to RegisterDto
                var doctorRegisterDto = new RegisterDto
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Gender = dto.Gender,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    DateOfBirth = dto.DateOfBirth,
                    Address = dto.Address,
                    IDNP = dto.IDNP,
                    UserRole = UserRegistrationType.Doctor,
                    Password = "DefaultPassword123!", // you can generate a temporary password
                    ConfirmPassword = "DefaultPassword123!"
                };

                // Use the AuthService to register the doctor
                var result = await _authService.RegisterAsync(doctorRegisterDto);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to create doctor: {Email}, Errors: {Errors}", dto.Email, string.Join(", ", result.Errors));
                    return BadRequest(new { message = result.Message, errors = result.Errors });
                }

                var user = result.AuthResponse!.User;

                // Map the returned user to DoctorProfileDto
                var doctorDto = new DoctorProfileDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Gender = user.Gender,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    IDNP = user.IDNP,
                    ClinicId = dto.ClinicId,
                    IsActive = user.IsActive,
                    TotalPatients = 0,
                    LastActivity = null
                };

                _logger.LogInformation("Doctor created successfully: {Email}", user.Email);
                return Ok(doctorDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating doctor: {Email}", dto.Email);
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }

        // PUT: api/Doctor/{idnp}
        [HttpPut("{idnp}")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateDoctor(string idnp, [FromBody] DoctorUpdateDto dto)
        {
            var doctor = await _context.Users
                .Where(u => u.IDNP == idnp && u.UserRoles.Any(r => r.Role.Name == "Doctor"))
                .FirstOrDefaultAsync();

            if (doctor == null)
                return NotFound("Doctor not found.");

            // Update only allowed fields
            if (dto.PhoneNumber != null) doctor.PhoneNumber = dto.PhoneNumber;
            if (dto.Address != null) doctor.Address = dto.Address;
            if (dto.IsActive.HasValue) doctor.IsActive = dto.IsActive.Value;

            await _context.SaveChangesAsync();

            // Return updated doctor DTO
            var doctorDto = new DoctorProfileDto
            {
                Id = doctor.Id,
                FirstName = doctor.FirstName,
                LastName = doctor.LastName,
                Email = doctor.Email ?? string.Empty,
                PhoneNumber = doctor.PhoneNumber,
                IDNP = doctor.IDNP,
                ClinicId = doctor.ClinicId,
                IsActive = doctor.IsActive,
                TotalPatients = 0
            };

            return Ok(doctorDto);
        }


        // DELETE: api/Doctor/{idnp}
        [HttpDelete("{idnp}")]
        public async Task<IActionResult> DeleteDoctor(string idnp)
        {
            var doctor = await _context.Users
                .Where(u => u.IDNP == idnp && u.UserRoles.Any(r => r.Role.Name == "Doctor"))
                .FirstOrDefaultAsync();

            if (doctor == null) return NotFound("Doctor not found");

            _context.Users.Remove(doctor);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
