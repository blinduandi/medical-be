using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using medical_be.Data;
using medical_be.Models;
using medical_be.DTOs;
using medical_be.Services;
using medical_be.Shared.Interfaces;
using medical_be.Extensions;
using medical_be.Controllers.Base;
using AutoMapper;


namespace medical_be.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DoctorController : BaseApiController
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
                    Specialty = u.Specialty,
                    Experience = u.Experience,
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
        [Authorize(Roles = "Admin,Doctor")]
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
                        Specialty = u.Specialty,
                        Experience = u.Experience,
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
                    Specialty = dto.Specialty,
                    Experience = dto.Experience,
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
        [HttpPut("updateDoctor")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<IActionResult> UpdateDoctor([FromBody] DoctorUpdateDto dto)
        {
            try
            {
                // Get the authenticated user's ID from claims
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token.");
                }

                var doctor = await _context.Users
                    .Where(u => u.Id == userId && u.UserRoles.Any(r => r.Role.Name == "Doctor"))
                    .FirstOrDefaultAsync();

                if (doctor == null)
                    return NotFound("Doctor not found.");

                // Update only allowed fields
                if (dto.PhoneNumber != null) doctor.PhoneNumber = dto.PhoneNumber;
                if (dto.Address != null) doctor.Address = dto.Address;
                if (dto.IsActive.HasValue) doctor.IsActive = dto.IsActive.Value;
                if (dto.ClinicId != null) doctor.ClinicId = dto.ClinicId;
                if (dto.Specialty != null) doctor.Specialty = dto.Specialty;
                if (dto.Experience != null) doctor.Experience = dto.Experience;

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
                    Specialty = doctor.Specialty,
                    Experience = doctor.Experience,
                    IsActive = doctor.IsActive,
                    TotalPatients = 0
                };

                return Ok(doctorDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating doctor with userId: {UserId}", User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }


        // DELETE: api/Doctor/{idnp}
        [HttpDelete("deleteDoctor")]
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

        /// <summary>
        /// Get doctor's assigned patients
        /// </summary>
        [HttpGet("my-patients")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> GetMyPatients([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = User.GetUserId();
                
                var query = _context.PatientDoctors
                    .Where(pd => pd.DoctorId == userId && pd.IsActive)
                    .Include(pd => pd.Patient);

                var totalCount = await query.CountAsync();
                var skip = (page - 1) * pageSize;

                var doctorPatients = await query
                    .Skip(skip)
                    .Take(pageSize)
                    .Select(pd => new DoctorPatientDto
                    {
                        Id = pd.Id,
                        PatientId = pd.PatientId,
                        PatientName = pd.Patient.FirstName + " " + pd.Patient.LastName,
                        PatientEmail = pd.Patient.Email ?? "",
                        PatientPhoneNumber = pd.Patient.PhoneNumber,
                        PatientIDNP = pd.Patient.IDNP,
                        BloodType = pd.Patient.BloodType,
                        DateOfBirth = pd.Patient.DateOfBirth,
                        AssignedDate = pd.AssignedDate,
                        IsActive = pd.IsActive,
                        Notes = pd.Notes,
                        AssignedBy = pd.AssignedBy,
                        LastVisit = _context.VisitRecords
                            .Where(v => v.PatientId == pd.PatientId && v.DoctorId == userId)
                            .OrderByDescending(v => v.VisitDate)
                            .Select(v => v.VisitDate)
                            .FirstOrDefault(),
                        TotalVisits = _context.VisitRecords
                            .Count(v => v.PatientId == pd.PatientId && v.DoctorId == userId)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    error = false,
                    message = "Doctor's patients retrieved successfully",
                    data = doctorPatients,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalItems = totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                        hasNextPage = page < Math.Ceiling((double)totalCount / pageSize),
                        hasPreviousPage = page > 1
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patients for doctor: {DoctorId}", User.GetUserId());
                return StatusCode(500, new { message = "An error occurred while retrieving patients" });
            }
        }

        /// <summary>
        /// Search patients by IDNP, name (for doctors to view assigned patients)
        /// </summary>
        [HttpPost("search-my-patients")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> SearchMyPatients([FromBody] PatientSearchDto searchDto)
        {
            try
            {
                var userId = User.GetUserId();

                if (string.IsNullOrWhiteSpace(searchDto.IDNP) && 
                    string.IsNullOrWhiteSpace(searchDto.Name))
                {
                    return BadRequest(new { message = "At least one search parameter is required" });
                }

                var query = _context.PatientDoctors
                    .Where(pd => pd.DoctorId == userId && pd.IsActive)
                    .Include(pd => pd.Patient)
                    .AsQueryable();

                // Apply search filters
                if (!string.IsNullOrWhiteSpace(searchDto.IDNP))
                {
                    query = query.Where(pd => pd.Patient.IDNP.Contains(searchDto.IDNP));
                }

                if (!string.IsNullOrWhiteSpace(searchDto.Name))
                {
                    query = query.Where(pd => 
                        pd.Patient.FirstName.Contains(searchDto.Name) || 
                        pd.Patient.LastName.Contains(searchDto.Name));
                }

                var foundPatients = await query
                    .Select(pd => new DoctorPatientDto
                    {
                        Id = pd.Id,
                        PatientId = pd.PatientId,
                        PatientName = pd.Patient.FirstName + " " + pd.Patient.LastName,
                        PatientEmail = pd.Patient.Email ?? "",
                        PatientPhoneNumber = pd.Patient.PhoneNumber,
                        PatientIDNP = pd.Patient.IDNP,
                        BloodType = pd.Patient.BloodType,
                        DateOfBirth = pd.Patient.DateOfBirth,
                        AssignedDate = pd.AssignedDate,
                        IsActive = pd.IsActive,
                        Notes = pd.Notes,
                        AssignedBy = pd.AssignedBy,
                        LastVisit = _context.VisitRecords
                            .Where(v => v.PatientId == pd.PatientId && v.DoctorId == userId)
                            .OrderByDescending(v => v.VisitDate)
                            .Select(v => v.VisitDate)
                            .FirstOrDefault(),
                        TotalVisits = _context.VisitRecords
                            .Count(v => v.PatientId == pd.PatientId && v.DoctorId == userId)
                    })
                    .Take(20) // Limit results
                    .ToListAsync();

                // Audit log
                await _auditService.LogAuditAsync(
                    userId, 
                    "DoctorPatientSearch", 
                    $"Searched patients with criteria: IDNP={searchDto.IDNP}, Name={searchDto.Name}", 
                    "PatientDoctor", 
                    null, 
                    Request.GetClientIpAddress());

                return Ok(new
                {
                    success = true,
                    error = false,
                    message = "Patient search completed successfully",
                    data = foundPatients
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching patients for doctor: {DoctorId}", User.GetUserId());
                return StatusCode(500, new { message = "An error occurred while searching patients" });
            }
        }

        /// <summary>
        /// Get detailed information about a specific assigned patient
        /// </summary>
        [HttpGet("patient/{patientId}")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> GetPatientDetails(string patientId)
        {
            try
            {
                var userId = User.GetUserId();

                // Verify that this patient is assigned to the doctor
                var patientRelation = await _context.PatientDoctors
                    .Where(pd => pd.DoctorId == userId && pd.PatientId == patientId && pd.IsActive)
                    .Include(pd => pd.Patient)
                    .FirstOrDefaultAsync();

                if (patientRelation == null)
                {
                    return NotFound(new { message = "Patient not found or not assigned to this doctor" });
                }

                // Get patient's detailed information
                var patientDetails = new DoctorPatientDto
                {
                    Id = patientRelation.Id,
                    PatientId = patientRelation.PatientId,
                    PatientName = patientRelation.Patient.FirstName + " " + patientRelation.Patient.LastName,
                    PatientEmail = patientRelation.Patient.Email ?? "",
                    PatientPhoneNumber = patientRelation.Patient.PhoneNumber,
                    PatientIDNP = patientRelation.Patient.IDNP,
                    BloodType = patientRelation.Patient.BloodType,
                    DateOfBirth = patientRelation.Patient.DateOfBirth,
                    AssignedDate = patientRelation.AssignedDate,
                    IsActive = patientRelation.IsActive,
                    Notes = patientRelation.Notes,
                    AssignedBy = patientRelation.AssignedBy,
                    LastVisit = await _context.VisitRecords
                        .Where(v => v.PatientId == patientId && v.DoctorId == userId)
                        .OrderByDescending(v => v.VisitDate)
                        .Select(v => v.VisitDate)
                        .FirstOrDefaultAsync(),
                    TotalVisits = await _context.VisitRecords
                        .CountAsync(v => v.PatientId == patientId && v.DoctorId == userId)
                };

                return Ok(new
                {
                    success = true,
                    error = false,
                    message = "Patient details retrieved successfully",
                    data = patientDetails
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient details for doctor: {DoctorId}, Patient: {PatientId}", User.GetUserId(), patientId);
                return StatusCode(500, new { message = "An error occurred while retrieving patient details" });
            }
        }

                /// <summary>
        /// Get doctor's dashboard data (includes profile and other details)
        /// </summary>
        [HttpGet("dashboard")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var doctorId = User.GetUserId();

                // Fetch profile data
                var profile = await _context.Users
                    .Where(u => u.Id == doctorId)
                    .Select(u => new DoctorProfileDto
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email ?? string.Empty,
                        PhoneNumber = u.PhoneNumber ?? string.Empty,
                        IDNP = u.IDNP,
                        DateOfBirth = u.DateOfBirth,
                        Address = u.Address,
                        IsActive = u.IsActive,
                    })
                    .FirstOrDefaultAsync();

                if (profile == null)
                {
                    return NotFoundResponse("Doctor not found");
                }

                // Return aggregated data
                return SuccessResponse(new
                {
                    Profile = profile,
                }, "Dashboard data retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard data for doctor: {DoctorId}", User.GetUserId());
                return InternalServerErrorResponse("An error occurred while retrieving the dashboard data");
            }
        }
    }
}
