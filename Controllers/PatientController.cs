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
    [Route("api/[controller]")]
    [Authorize]
    public class PatientController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IAuditService _auditService;
        private readonly ILogger<PatientController> _logger;
        private readonly IPatientAccessLogService _patientAccessLogService;

        public PatientController(
            ApplicationDbContext context,
            IMapper mapper,
            IAuditService auditService,
            ILogger<PatientController> logger,
            IPatientAccessLogService patientAccessLogService)
        {
            _context = context;
            _mapper = mapper;
            _auditService = auditService;
            _logger = logger;
            _patientAccessLogService = patientAccessLogService;
        }

        /// <summary>
        /// Search patients by IDNP
        /// </summary>
        [HttpPost("search")]
        [Authorize(Roles = "Doctor,Admin")]
        public async Task<IActionResult> SearchByIDNP([FromBody] PatientSearchDto searchDto)
        {
            try
            {
                var validationResult = ValidateModel();
                if (validationResult != null)
                    return validationResult;

                if (string.IsNullOrWhiteSpace(searchDto.IDNP))
                {
                    return ValidationErrorResponse("IDNP is required");
                }

                var patient = await _context.Users
                    .Where(u => u.IDNP == searchDto.IDNP)
                    .Select(u => new PatientProfileDto
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email ?? string.Empty,
                        PhoneNumber = u.PhoneNumber ?? string.Empty,
                        IDNP = u.IDNP,
                        BloodType = u.BloodType,
                        DateOfBirth = u.DateOfBirth,
                        Address = u.Address,
                        IsActive = u.IsActive
                    })
                    .FirstOrDefaultAsync();

                if (patient == null)
                {
                    return NotFoundResponse("Patient not found");
                }

                // Audit log
                await _auditService.LogAuditAsync(User.GetUserId(), "PatientSearch", $"Searched patient with IDNP: {searchDto.IDNP}", "Patient", null, Request.GetClientIpAddress());

                return SuccessResponse(patient, "Patient found successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching patient by IDNP: {IDNP}", searchDto.IDNP);
                return InternalServerErrorResponse("An error occurred while searching for patient");
            }
        }

        /// <summary>
        /// Get patient's visit records
        /// </summary>
        [HttpGet("{patientId}/visits")]
        [Authorize(Roles = "Doctor,Admin,Patient")]
        public async Task<IActionResult> GetVisitRecords(string patientId)
        {
            try
            {
                var visits = await _context.VisitRecords
                    .Where(v => v.PatientId == patientId)
                    .Include(v => v.Doctor)
                    .OrderByDescending(v => v.VisitDate)
                    .Select(v => new VisitRecordDto
                    {
                        Id = v.Id,
                        PatientId = v.PatientId,
                        DoctorId = v.DoctorId,
                        DoctorName = v.Doctor.FirstName + " " + v.Doctor.LastName,
                        VisitDate = v.VisitDate,
                        Diagnosis = v.Diagnosis,
                        Treatment = v.Treatment,
                        Notes = v.Notes,
                        VisitType = v.VisitType.ToString(),
                        CreatedAt = v.CreatedAt
                    })
                    .ToListAsync();

                return SuccessResponse(visits);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting visit records for patient: {PatientId}", patientId);
                return InternalServerErrorResponse( "Internal server error");
            }
        }

        /// <summary>
        /// Create new visit record
        /// </summary>
        [HttpPost("{patientId}/visits")]
        [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> CreateVisitRecord(string patientId, [FromBody] CreateVisitRecordDto visitDto)
        {
            try
            {
                var doctorId = User.GetUserId();
                
                var visitRecord = new VisitRecord
                {
                    PatientId = patientId,
                    DoctorId = doctorId,
                    VisitDate = visitDto.VisitDate,
                    Symptoms = visitDto.Symptoms, 
                    Diagnosis = visitDto.Diagnosis,
                    Treatment = visitDto.Treatment,
                    Notes = visitDto.Notes ?? string.Empty,
                    VisitType = Enum.Parse<VisitType>(visitDto.VisitType),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.VisitRecords.Add(visitRecord);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAuditAsync(
                    doctorId,
                    "VisitRecordCreated",
                    $"Created visit record for patient: {patientId}",
                    "VisitRecord",
                    visitRecord.Id,
                    Request.GetClientIpAddress()
                );

                var result = _mapper.Map<VisitRecordDto>(visitRecord)!;
                return CreatedAtAction(nameof(GetVisitRecords), new { patientId }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating visit record for patient: {PatientId}", patientId);
                return InternalServerErrorResponse( "Internal server error");
            }
        }

        /// <summary>
        /// Get patient's vaccinations
        /// </summary>
        [HttpGet("{patientId}/vaccinations")]
        [Authorize(Roles = "Doctor,Admin,Patient")]
        public async Task<IActionResult> GetVaccinations(string patientId)
        {
            try
            {
                var vaccinations = await _context.Vaccinations
                    .Where(v => v.PatientId == patientId)
                    .Include(v => v.AdministeredBy)
                    .OrderByDescending(v => v.DateAdministered)
                    .Select(v => new VaccinationDto
                    {
                        Id = v.Id,
                        PatientId = v.PatientId,
                        VaccineName = v.VaccineName,
                        DateAdministered = v.DateAdministered,
                        AdministeredById = v.AdministeredById,
                        DoctorName = v.AdministeredBy != null ? (v.AdministeredBy.FirstName + " " + v.AdministeredBy.LastName) : string.Empty,
                        BatchNumber = v.BatchNumber,
                        Notes = v.Notes,
                        CreatedAt = v.CreatedAt
                    })
                    .ToListAsync();

                return SuccessResponse(vaccinations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting vaccinations for patient: {PatientId}", patientId);
                return InternalServerErrorResponse( "Internal server error");
            }
        }

        /// <summary>
        /// Add vaccination record
        /// </summary>
        [HttpPost("{patientId}/vaccinations")]
        [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> AddVaccination(string patientId, [FromBody] CreateVaccinationDto vaccinationDto)
        {
            try
            {
                var doctorId = User.GetUserId();

                var vaccination = new Vaccination
                {
                    PatientId = patientId,
                    VaccineName = vaccinationDto.VaccineName,
                    DateAdministered = vaccinationDto.DateAdministered,
                    AdministeredById = doctorId,
                    BatchNumber = vaccinationDto.BatchNumber,
                    Manufacturer = vaccinationDto.Manufacturer,
                    Notes = vaccinationDto.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Vaccinations.Add(vaccination);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAuditAsync(
                    doctorId,
                    "VaccinationAdded",
                    $"Added vaccination {vaccinationDto.VaccineName} for patient: {patientId}",
                    "Vaccination",
                    vaccination.Id,
                    Request.GetClientIpAddress()
                );

                var result = _mapper.Map<VaccinationDto>(vaccination)!;
                return CreatedAtAction(nameof(GetVaccinations), new { patientId }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding vaccination for patient: {PatientId}", patientId);
                return InternalServerErrorResponse( "Internal server error");
            }
        }

        /// <summary>
        /// Get patient's allergies
        /// </summary>
        [HttpGet("{patientId}/allergies")]
        [Authorize(Roles = "Doctor,Admin,Patient")]
        public async Task<IActionResult> GetAllergies(string patientId)
        {
            try
            {
                var allergies = await _context.Allergies
                    .Where(a => a.PatientId == patientId)
                    .Include(a => a.RecordedBy)
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => new AllergyDto
                    {
                        Id = a.Id,
                        PatientId = a.PatientId,
                        AllergenName = a.AllergenName,
                        Severity = a.Severity.ToString(),
                        Reaction = a.Reaction,
                        RecordedById = a.RecordedById,
                        Notes = a.Notes,
                        CreatedAt = a.CreatedAt
                    })
                    .ToListAsync();

                return SuccessResponse(allergies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting allergies for patient: {PatientId}", patientId);
                return InternalServerErrorResponse( "Internal server error");
            }
        }

        /// <summary>
        /// Add allergy record
        /// </summary>
        [HttpPost("{patientId}/allergies")]
        [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> AddAllergy(string patientId, [FromBody] CreateAllergyDto allergyDto)
        {
            try
            {
                var doctorId = User.GetUserId();

                var allergy = new Allergy
                {
                    PatientId = patientId,
                    AllergenName = allergyDto.AllergenName,
                    Severity = Enum.Parse<AllergySeverity>(allergyDto.Severity),
                    Reaction = allergyDto.Reaction,
                    RecordedById = doctorId,
                    Notes = allergyDto.Notes,
                    DiagnosedDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Allergies.Add(allergy);
                await _context.SaveChangesAsync();

                // Audit log
                await _auditService.LogAuditAsync(
                    doctorId,
                    "AllergyAdded",
                    $"Added allergy {allergyDto.AllergenName} for patient: {patientId}",
                    "Allergy",
                    allergy.Id,
                    Request.GetClientIpAddress()
                );

                var result = _mapper.Map<AllergyDto>(allergy)!;
                return CreatedAtAction(nameof(GetAllergies), new { patientId }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding allergy for patient: {PatientId}", patientId);
                return InternalServerErrorResponse( "Internal server error");
            }
        }

        /// <summary>
        /// Get patient's dashboard data (includes profile and other details)
        /// </summary>
        [HttpGet("dashboard")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var patientId = User.GetUserId();

                // Fetch profile data
                var profile = await _context.Users
                    .Where(u => u.Id == patientId)
                    .Select(u => new PatientProfileDto
                    {
                        Id = u.Id,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email ?? string.Empty,
                        PhoneNumber = u.PhoneNumber ?? string.Empty,
                        IDNP = u.IDNP,
                        BloodType = u.BloodType,
                        DateOfBirth = u.DateOfBirth,
                        Address = u.Address,
                        IsActive = u.IsActive,
                        ActiveAllergies = _context.Allergies
                            .Where(a => a.PatientId == u.Id && a.IsActive)
                            .Select(a => new AllergyDto
                            {
                                Id = a.Id,
                                AllergenName = a.AllergenName,
                                Severity = a.Severity.ToString(),
                                Reaction = a.Reaction
                            }).ToList(),
                        RecentVaccinations = _context.Vaccinations
                            .Where(v => v.PatientId == u.Id)
                            .OrderByDescending(v => v.DateAdministered)
                            .Take(5)
                            .Select(v => new VaccinationDto
                            {
                                Id = v.Id,
                                PatientId = v.PatientId,
                                VaccineName = v.VaccineName,
                                DateAdministered = v.DateAdministered,
                                BatchNumber = v.BatchNumber,
                                Manufacturer = v.Manufacturer,
                                Notes = v.Notes,
                                AdministeredById = v.AdministeredById,
                                DoctorName = v.AdministeredBy != null ? (v.AdministeredBy.FirstName + " " + v.AdministeredBy.LastName) : null,
                                CreatedAt = v.CreatedAt
                            }).ToList(),
                        TotalVisits = _context.VisitRecords
                            .Count(v => v.PatientId == u.Id)
                    })
                    .FirstOrDefaultAsync();

                if (profile == null)
                {
                    return NotFoundResponse("Patient not found");
                }

                // Fetch all visit records
                var visits = await _context.VisitRecords
                    .Where(v => v.PatientId == patientId)
                    .Include(v => v.Doctor)
                    .OrderByDescending(v => v.VisitDate)
                    .Select(v => new VisitRecordDto
                    {
                        Id = v.Id,
                        VisitDate = v.VisitDate,
                        DoctorName = v.Doctor.FirstName + " " + v.Doctor.LastName,
                        Diagnosis = v.Diagnosis,
                        Notes = v.Notes,
                        Treatment = v.Treatment,
                        Symptoms = v.Symptoms
                    })
                    .ToListAsync();

                // Return aggregated data
                return SuccessResponse(new
                {
                    Profile = profile,
                    Visits = visits
                }, "Dashboard data retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard data for patient: {PatientId}", User.GetUserId());
                return InternalServerErrorResponse("An error occurred while retrieving the dashboard data");
            }
        }

        /// <summary>
        /// Get patient's assigned doctors
        /// </summary>
        [HttpGet("my-doctors")]
        [Authorize(Roles = "Patient,Admin")]
        public async Task<IActionResult> GetMyDoctors()
        {
            try
            {
                var userId = User.GetUserId();
                var patientDoctors = await _context.PatientDoctors
                    .Where(pd => pd.PatientId == userId && pd.IsActive)
                    .Include(pd => pd.Doctor)
                    .Select(pd => new PatientDoctorDto
                    {
                        Id = pd.Id,
                        DoctorId = pd.DoctorId,
                        DoctorName = pd.Doctor.FirstName + " " + pd.Doctor.LastName,
                        DoctorEmail = pd.Doctor.Email ?? "",
                        DoctorPhoneNumber = pd.Doctor.PhoneNumber,
                        ClinicId = pd.Doctor.ClinicId,
                        Specialty = pd.Doctor.Specialty,
                        Experience = pd.Doctor.Experience,
                        AssignedDate = pd.AssignedDate,
                        IsActive = pd.IsActive,
                        Notes = pd.Notes,
                        AssignedBy = pd.AssignedBy
                    })
                    .ToListAsync();

                return SuccessResponse(patientDoctors, "Patient's doctors retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving doctors for patient: {PatientId}", User.GetUserId());
                return InternalServerErrorResponse("An error occurred while retrieving doctors");
            }
        }

        /// <summary>
        /// Search available doctors to add
        /// </summary>
        [HttpGet("available-doctors")]
        [Authorize(Roles = "Patient,Admin")]
        public async Task<IActionResult> GetAvailableDoctors([FromQuery] string? search)
        {
            try
            {
                var userId = User.GetUserId();
                
                // Get currently assigned doctor IDs
                var assignedDoctorIds = await _context.PatientDoctors
                    .Where(pd => pd.PatientId == userId && pd.IsActive)
                    .Select(pd => pd.DoctorId)
                    .ToListAsync();

                var doctorsQuery = _context.Users
                    .Where(u => u.UserRoles.Any(r => r.Role.Name == "Doctor") && u.IsActive);

                // Apply search filter if provided
                if (!string.IsNullOrWhiteSpace(search))
                {
                    doctorsQuery = doctorsQuery.Where(u => 
                        u.FirstName.Contains(search) || 
                        u.LastName.Contains(search) || 
                        u.Email!.Contains(search) ||
                        u.IDNP.Contains(search));
                }

                var availableDoctors = await doctorsQuery
                    .Select(u => new AvailableDoctorDto
                    {
                        Id = u.Id,
                        Name = u.FirstName + " " + u.LastName,
                        Email = u.Email ?? "",
                        PhoneNumber = u.PhoneNumber,
                        ClinicId = u.ClinicId,
                        Specialty = u.Specialty,
                        Experience = u.Experience,
                        IDNP = u.IDNP,
                        IsAlreadyAssigned = assignedDoctorIds.Contains(u.Id)
                    })
                    .Take(20) // Limit results
                    .ToListAsync();

                return SuccessResponse(availableDoctors, "Available doctors retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available doctors for patient: {PatientId}", User.GetUserId());
                return InternalServerErrorResponse("An error occurred while retrieving available doctors");
            }
        }

        /// <summary>
        /// Add a doctor to patient's list
        /// </summary>
        [HttpPost("add-doctor")]
        [Authorize(Roles = "Patient,Admin")]
        public async Task<IActionResult> AddDoctor([FromBody] AddDoctorToPatientDto dto)
        {
            try
            {
                var validationResult = ValidateModel();
                if (validationResult != null)
                    return validationResult;

                var userId = User.GetUserId();

                // Check if doctor exists and is active
                var doctor = await _context.Users
                    .Where(u => u.Id == dto.DoctorId && u.UserRoles.Any(r => r.Role.Name == "Doctor") && u.IsActive)
                    .FirstOrDefaultAsync();

                if (doctor == null)
                {
                    return NotFoundResponse("Doctor not found or inactive");
                }

                // Check if relationship already exists
                var existingRelation = await _context.PatientDoctors
                    .Where(pd => pd.PatientId == userId && pd.DoctorId == dto.DoctorId && pd.IsActive)
                    .FirstOrDefaultAsync();

                if (existingRelation != null)
                {
                    return ValidationErrorResponse("Doctor is already assigned to this patient");
                }

                // Create new relationship
                var patientDoctor = new PatientDoctor
                {
                    PatientId = userId,
                    DoctorId = dto.DoctorId,
                    AssignedDate = DateTime.UtcNow,
                    IsActive = true,
                    Notes = dto.Notes,
                    AssignedBy = "Patient"
                };

                _context.PatientDoctors.Add(patientDoctor);
                await _context.SaveChangesAsync();

                // Log audit
                await _auditService.LogAuditAsync(
                    userId, 
                    "PatientDoctorAdded", 
                    $"Patient added doctor {doctor.FirstName} {doctor.LastName} (ID: {dto.DoctorId})", 
                    "PatientDoctor", 
                    null, 
                    Request.GetClientIpAddress());

                // Return the created relationship
                var response = new PatientDoctorDto
                {
                    Id = patientDoctor.Id,
                    DoctorId = patientDoctor.DoctorId,
                    DoctorName = doctor.FirstName + " " + doctor.LastName,
                    DoctorEmail = doctor.Email ?? "",
                    DoctorPhoneNumber = doctor.PhoneNumber,
                    ClinicId = doctor.ClinicId,
                    Specialty = doctor.Specialty,
                    Experience = doctor.Experience,
                    AssignedDate = patientDoctor.AssignedDate,
                    IsActive = patientDoctor.IsActive,
                    Notes = patientDoctor.Notes,
                    AssignedBy = patientDoctor.AssignedBy
                };

                return SuccessResponse(response, "Doctor added successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding doctor to patient: {PatientId}, Doctor: {DoctorId}", User.GetUserId(), dto.DoctorId);
                return InternalServerErrorResponse("An error occurred while adding the doctor");
            }
        }

        /// <summary>
        /// Remove a doctor from patient's list
        /// </summary>
        [HttpPost("remove-doctor")]
        [Authorize(Roles = "Patient,Admin")]
        public async Task<IActionResult> RemoveDoctor([FromBody] RemoveDoctorFromPatientDto dto)
        {
            try
            {
                var validationResult = ValidateModel();
                if (validationResult != null)
                    return validationResult;

                var userId = User.GetUserId();

                // Find the active relationship
                var patientDoctor = await _context.PatientDoctors
                    .Include(pd => pd.Doctor)
                    .Where(pd => pd.PatientId == userId && pd.DoctorId == dto.DoctorId && pd.IsActive)
                    .FirstOrDefaultAsync();

                if (patientDoctor == null)
                {
                    return NotFoundResponse("Doctor relationship not found or already inactive");
                }

                // Deactivate the relationship
                patientDoctor.IsActive = false;
                patientDoctor.DeactivatedDate = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(dto.Reason))
                {
                    patientDoctor.Notes = string.IsNullOrWhiteSpace(patientDoctor.Notes) 
                        ? $"Removed: {dto.Reason}"
                        : $"{patientDoctor.Notes}\nRemoved: {dto.Reason}";
                }

                await _context.SaveChangesAsync();

                // Log audit
                await _auditService.LogAuditAsync(
                    userId, 
                    "PatientDoctorRemoved", 
                    $"Patient removed doctor {patientDoctor.Doctor.FirstName} {patientDoctor.Doctor.LastName} (ID: {dto.DoctorId}). Reason: {dto.Reason}", 
                    "PatientDoctor", 
                    null, 
                    Request.GetClientIpAddress());

                return SuccessResponse(null, "Doctor removed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing doctor from patient: {PatientId}, Doctor: {DoctorId}", User.GetUserId(), dto.DoctorId);
                return InternalServerErrorResponse("An error occurred while removing the doctor");
            }
        }

        /// <summary>
        /// Get patient's access log summary (who accessed their data and when)
        /// </summary>
        [HttpGet("access-log/summary")]
        [Authorize(Roles = "Patient,Admin")]
        public async Task<IActionResult> GetMyAccessLogSummary()
        {
            try
            {
                var patientId = User.GetUserId();
                var summary = await _patientAccessLogService.GetPatientAccessSummaryAsync(patientId);
                
                return SuccessResponse(summary, "Access log summary retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving access log summary for patient: {PatientId}", User.GetUserId());
                return InternalServerErrorResponse("An error occurred while retrieving access log summary");
            }
        }

        /// <summary>
        /// Get detailed access logs for the patient
        /// </summary>
        [HttpGet("access-log")]
        [Authorize(Roles = "Patient,Admin")]
        public async Task<IActionResult> GetMyAccessLogs([FromQuery] PatientAccessLogQueryDto query)
        {
            try
            {
                var patientId = User.GetUserId();
                
                // Ensure the query is for the current patient only (unless admin)
                if (!User.IsInRole("Admin"))
                {
                    query.PatientId = patientId;
                }
                else if (string.IsNullOrEmpty(query.PatientId))
                {
                    query.PatientId = patientId;
                }

                var accessLogs = await _patientAccessLogService.GetPatientAccessLogsAsync(query);
                
                return SuccessResponse(new
                {
                    accessLogs = accessLogs,
                    pagination = new
                    {
                        currentPage = query.Page,
                        pageSize = query.PageSize,
                        hasMore = accessLogs.Count == query.PageSize
                    }
                }, "Access logs retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving access logs for patient: {PatientId}", User.GetUserId());
                return InternalServerErrorResponse("An error occurred while retrieving access logs");
            }
        }
    }
}
