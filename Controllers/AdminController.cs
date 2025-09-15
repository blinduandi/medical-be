using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using medical_be.Data;
using medical_be.Models;
using medical_be.DTOs;
using medical_be.Services;
using medical_be.Shared.Interfaces;
using medical_be.Extensions;
using medical_be.Controllers.Base;
using AutoMapper;
using System.Linq;

namespace medical_be.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly IMapper _mapper;
        private readonly IAuditService _auditService;
        private readonly ILogger<AdminController> _logger;
        private readonly INotificationService _notificationService;
        private readonly IPatternDetectionService _patternDetectionService;
        private readonly IDataSeedingService _dataSeedingService;
        private readonly IServiceScopeFactory _scopeFactory;

        public AdminController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            RoleManager<Role> roleManager,
            IMapper mapper,
            IAuditService auditService,
            ILogger<AdminController> logger,
            INotificationService notificationService,
            IPatternDetectionService patternDetectionService,
            IDataSeedingService dataSeedingService,
            IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
            _auditService = auditService;
            _logger = logger;
            _notificationService = notificationService;
            _patternDetectionService = patternDetectionService;
            _dataSeedingService = dataSeedingService;
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        /// Get users count - for testing database connectivity (temporary, no auth required)
        /// </summary>
        [HttpGet("test/users-count")]
        [AllowAnonymous]
        public async Task<IActionResult> GetUsersCount()
        {
            try
            {
                var totalUsers = await _context.Users.CountAsync();
                var users = await _context.Users
                    .Select(u => new
                    {
                        u.Id,
                        u.FirstName,
                        u.LastName,
                        u.Email,
                        u.IsActive,
                        u.CreatedAt
                    })
                    .ToListAsync();

                return SuccessResponse(new
                {
                    TotalUsers = totalUsers,
                    Users = users
                }, "Users data retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users count");
                return InternalServerErrorResponse("An error occurred while retrieving users data");
            }
        }

        /// <summary>
        /// Get all users with pagination
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? role = null)
        {
            try
            {
                var query = _context.Users.AsQueryable();

                // Filter by role if specified
                if (!string.IsNullOrEmpty(role))
                {
                    var roleUsers = await _userManager.GetUsersInRoleAsync(role);
                    var roleUserIds = roleUsers.Select(u => u.Id).ToList();
                    query = query.Where(u => roleUserIds.Contains(u.Id));
                }

                var totalUsers = await query.CountAsync();
                var users = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
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
                    .ToListAsync();

                // Get roles for each user
                var usersWithRoles = new List<object>();
                foreach (var user in users)
                {
                    var userEntity = await _userManager.FindByIdAsync(user.Id);
                    var userRoles = userEntity != null
                        ? await _userManager.GetRolesAsync(userEntity)
                        : new List<string>();

                    usersWithRoles.Add(new
                    {
                        User = user,
                        Roles = userRoles
                    });
                }

                var result = new
                {
                    Users = usersWithRoles,
                    Page = page,
                    PageSize = pageSize,
                    TotalUsers = totalUsers,
                    TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize)
                };

                return PaginatedResponse(usersWithRoles, page, pageSize, totalUsers, "Users retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return InternalServerErrorResponse("An error occurred while retrieving users");
            }
        }

        /// <summary>
        /// Create new user
        /// </summary>
        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDTO createUserDto)
        {
            try
            {
                var validationResult = ValidateModel();
                if (validationResult != null)
                    return validationResult;

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(createUserDto.Email);
                if (existingUser != null)
                {
                    return ErrorResponse("User with this email already exists");
                }

                // Check IDNP uniqueness
                var existingIDNP = await _context.Users.FirstOrDefaultAsync(u => u.IDNP == createUserDto.IDNP);
                if (existingIDNP != null)
                {
                    return ErrorResponse("User with this IDNP already exists");
                }

                // Validate required fields
                if (!createUserDto.DateOfBirth.HasValue)
                {
                    return ValidationErrorResponse("Date of birth is required");
                }

                var user = new User
                {
                    UserName = createUserDto.Email,
                    Email = createUserDto.Email,
                    FirstName = createUserDto.FirstName,
                    LastName = createUserDto.LastName,
                    PhoneNumber = createUserDto.PhoneNumber,
                    IDNP = createUserDto.IDNP,
                    BloodType = createUserDto.BloodType,
                    DateOfBirth = createUserDto.DateOfBirth.Value,
                    Address = createUserDto.Address,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, createUserDto.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return ValidationErrorResponse("Failed to create user", errors);
                }

                // Assign role
                if (!string.IsNullOrEmpty(createUserDto.Role))
                {
                    await _userManager.AddToRoleAsync(user, createUserDto.Role);
                }

                // Audit log
                await _auditService.LogAuditAsync(User.GetUserId(), "UserCreated", $"Created user: {createUserDto.Email}", "User", null, Request.GetClientIpAddress());
                
                var userDto = _mapper.Map<PatientProfileDto>(user);
                return SuccessResponse(userDto, "User created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Email}", createUserDto.Email);
                return InternalServerErrorResponse("An error occurred while creating user");
            }
        }

        /// <summary>
        /// Update user
        /// </summary>
        [HttpPut("users/{userId}")]
        public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserDTO updateUserDto)
        {
            try
            {
                var validationResult = ValidateModel();
                if (validationResult != null)
                    return validationResult;

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFoundResponse("User not found");
                }

                // Check IDNP uniqueness (excluding current user)
                if (!string.IsNullOrEmpty(updateUserDto.IDNP) && updateUserDto.IDNP != user.IDNP)
                {
                    var existingIDNP = await _context.Users
                        .FirstOrDefaultAsync(u => u.IDNP == updateUserDto.IDNP && u.Id != userId);
                    if (existingIDNP != null)
                    {
                        return ErrorResponse("User with this IDNP already exists");
                    }
                }

                // Update user properties
                user.FirstName = updateUserDto.FirstName ?? user.FirstName;
                user.LastName = updateUserDto.LastName ?? user.LastName;
                user.PhoneNumber = updateUserDto.PhoneNumber ?? user.PhoneNumber;
                user.IDNP = updateUserDto.IDNP ?? user.IDNP;
                user.BloodType = updateUserDto.BloodType ?? user.BloodType;
                user.DateOfBirth = updateUserDto.DateOfBirth ?? user.DateOfBirth;
                user.Address = updateUserDto.Address ?? user.Address;
                user.IsActive = updateUserDto.IsActive ?? user.IsActive;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return ErrorResponse("Failed to update user", errors);
                }

                // Audit log
                await _auditService.LogAuditAsync(User.GetUserId(), "UserUpdated", $"Updated user: {user.Email}", "User", null, Request.GetClientIpAddress());

                return SuccessResponse(null, "Operation completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", userId);
                return InternalServerErrorResponse( "Internal server error");
            }
        }

        /// <summary>
        /// Delete user
        /// </summary>
        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFoundResponse("User not found");
                }

                // Check if user has medical records
                var hasRecords = await _context.VisitRecords.AnyAsync(v => v.PatientId == userId || v.DoctorId == userId) ||
                               await _context.Vaccinations.AnyAsync(v => v.PatientId == userId || v.AdministeredById == userId) ||
                               await _context.Allergies.AnyAsync(a => a.PatientId == userId || a.RecordedById == userId) ||
                               await _context.MedicalDocuments.AnyAsync(d => d.PatientId == userId || d.UploadedById == userId);

                if (hasRecords)
                {
                    // Soft delete - deactivate user instead of hard delete
                    user.IsActive = false;
                    await _userManager.UpdateAsync(user);
                }
                else
                {
                    // Hard delete if no medical records
                    await _userManager.DeleteAsync(user);
                }

                // Audit log
                await _auditService.LogAuditAsync(User.GetUserId(), hasRecords ? "UserDeactivated" : "UserDeleted", $"{(hasRecords ? "Deactivated" : "Deleted")} user: {user.Email}", "User", null, Request.GetClientIpAddress());

                return SuccessResponse(null, "Operation completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", userId);
                return InternalServerErrorResponse( "Internal server error");
            }
        }

        /// <summary>
        /// Assign role to user
        /// </summary>
        [HttpPost("users/{userId}/roles")]
        public async Task<IActionResult> AssignRole(string userId, [FromBody] AssignRoleDTO assignRoleDto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFoundResponse("User not found");
                }

                var roleExists = await _roleManager.RoleExistsAsync(assignRoleDto.RoleName);
                if (!roleExists)
                {
                    return ErrorResponse("Role does not exist");
                }

                var result = await _userManager.AddToRoleAsync(user, assignRoleDto.RoleName);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return ErrorResponse("Failed to assign role", errors);
                }

                // Audit log
                await _auditService.LogAuditAsync(User.GetUserId(), "RoleAssigned", $"Assigned role {assignRoleDto.RoleName} to user: {user.Email}", "User", null, Request.GetClientIpAddress());

                return SuccessResponse(null, "Operation completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role to user: {UserId}", userId);
                return InternalServerErrorResponse( "Internal server error");
            }
        }

        /// <summary>
        /// Remove role from user
        /// </summary>
        [HttpDelete("users/{userId}/roles/{roleName}")]
        public async Task<IActionResult> RemoveRole(string userId, string roleName)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFoundResponse("User not found");
                }

                var result = await _userManager.RemoveFromRoleAsync(user, roleName);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return ErrorResponse("Failed to remove role", errors);
                }

                // Audit log
                await _auditService.LogAuditAsync(User.GetUserId(), "RoleRemoved", $"Removed role {roleName} from user: {user.Email}", "User", null, Request.GetClientIpAddress());

                return SuccessResponse(null, "Operation completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role from user: {UserId}", userId);
                return InternalServerErrorResponse( "Internal server error");
            }
        }

        /// <summary>
        /// Get system statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var totalUsers = await _context.Users.CountAsync();
                var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
                var totalDoctors = (await _userManager.GetUsersInRoleAsync("Doctor")).Count;
                var totalPatients = (await _userManager.GetUsersInRoleAsync("Patient")).Count;
                var totalVisits = await _context.VisitRecords.CountAsync();
                var totalDocuments = await _context.MedicalDocuments.CountAsync();

                var recentVisits = await _context.VisitRecords
                    .Where(v => v.VisitDate >= DateTime.UtcNow.AddDays(-30))
                    .CountAsync();

                var statistics = new
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    InactiveUsers = totalUsers - activeUsers,
                    TotalDoctors = totalDoctors,
                    TotalPatients = totalPatients,
                    TotalVisits = totalVisits,
                    TotalDocuments = totalDocuments,
                    RecentVisits = recentVisits,
                    GeneratedAt = DateTime.UtcNow
                };

                return SuccessResponse(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics");
                return InternalServerErrorResponse( "Internal server error");
            }
        }

        #region Machine Learning & Analytics Endpoints

        /// <summary>
        /// Get comprehensive medical analytics with pattern detection
        /// </summary>
        [HttpGet("medical-analytics")]
        public async Task<IActionResult> GetMedicalAnalytics()
        {
            try
            {
                // Enhanced analytics using existing data
                var patients = await _context.Users
                    .Where(u => u.DateOfBirth != default(DateTime))
                    .Include(u => u.PatientVisitRecords)
                    .Include(u => u.PatientAllergies)
                    .Include(u => u.PatientVaccinations)
                    .Include(u => u.LabResults)
                    .Include(u => u.PatientDiagnoses)
                    .Select(u => new
                    {
                        u.Id,
                        u.FirstName,
                        u.LastName,
                        Age = DateTime.Now.Year - u.DateOfBirth.Year,
                        u.BloodType,
                        u.IsActive,
                        VisitCount = u.PatientVisitRecords.Count(),
                        AllergyCount = u.PatientAllergies.Count(),
                        VaccinationCount = u.PatientVaccinations.Count(),
                        DiagnosisCount = u.PatientDiagnoses.Count(),
                        LabResultCount = u.LabResults.Count(),
                        RecentVisits = u.PatientVisitRecords.Count(v => v.VisitDate >= DateTime.UtcNow.AddMonths(-6)),
                        LastVisit = u.PatientVisitRecords.OrderByDescending(v => v.VisitDate).FirstOrDefault(),
                        HasChronicConditions = u.PatientAllergies.Any(a => a.Severity == AllergySeverity.Severe),
                        RecentAbnormalLabs = u.LabResults.Count(lr => lr.TestDate >= DateTime.UtcNow.AddMonths(-3) && 
                                                                     (lr.Status == "HIGH" || lr.Status == "LOW" || lr.Status == "CRITICAL"))
                    })
                    .ToListAsync();

                // Calculate risk scores for all patients
                var highRiskPatients = new List<object>();
                foreach (var patient in patients.Take(100)) // Limit for performance
                {
                    var riskScore = await _patternDetectionService.CalculateRiskScoreAsync(patient.Id);
                    if (riskScore > 0.5)
                    {
                        highRiskPatients.Add(new
                        {
                            patient.Id,
                            patient.FirstName,
                            patient.LastName,
                            patient.Age,
                            RiskScore = Math.Round(riskScore, 3),
                            RiskLevel = GetRiskLevel(riskScore),
                            patient.RecentVisits,
                            patient.AllergyCount,
                            patient.RecentAbnormalLabs
                        });
                    }
                }

                // Blood type risk correlation
                var bloodTypeAnalysis = patients
                    .Where(p => !string.IsNullOrEmpty(p.BloodType))
                    .GroupBy(p => p.BloodType)
                    .Select(g => new
                    {
                        BloodType = g.Key,
                        Count = g.Count(),
                        AvgAge = Math.Round(g.Average(p => p.Age), 1),
                        AvgVisits = Math.Round(g.Average(p => p.VisitCount), 1),
                        HighRiskCount = highRiskPatients.Count(hr => g.Any(p => p.Id == hr.GetType().GetProperty("Id")?.GetValue(hr)?.ToString())),
                        AvgAllergies = Math.Round(g.Average(p => p.AllergyCount), 1),
                        AvgLabResults = Math.Round(g.Average(p => p.LabResultCount), 1)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                // Age-based analysis
                var ageRiskAnalysis = patients
                    .GroupBy(p => (p.Age / 10) * 10)
                    .Select(g => new
                    {
                        AgeGroup = $"{g.Key}-{g.Key + 9}",
                        Count = g.Count(),
                        AvgVisits = Math.Round(g.Average(p => p.VisitCount), 1),
                        HighRiskCount = highRiskPatients.Count(hr => g.Any(p => p.Id == hr.GetType().GetProperty("Id")?.GetValue(hr)?.ToString())),
                        RiskPercentage = g.Count() > 0 ? Math.Round((double)highRiskPatients.Count(hr => g.Any(p => p.Id == hr.GetType().GetProperty("Id")?.GetValue(hr)?.ToString())) / g.Count() * 100, 1) : 0,
                        ChronicConditions = g.Count(p => p.HasChronicConditions),
                        LowVaccinationRate = g.Count(p => p.VaccinationCount < 2),
                        AbnormalLabsAvg = Math.Round(g.Average(p => p.RecentAbnormalLabs), 1)
                    })
                    .OrderBy(x => x.AgeGroup)
                    .ToList();

                // Pattern detection alerts (simplified for immediate results)
                var patternAlerts = new List<object>();

                // Alert 1: Elderly with frequent visits
                var elderlyFrequentVisits = patients.Where(p => p.Age > 70 && p.RecentVisits > 4).ToList();
                if (elderlyFrequentVisits.Count >= 3)
                {
                    patternAlerts.Add(new
                    {
                        AlertType = "ELDERLY_FREQUENT_VISITS",
                        Severity = "HIGH",
                        PatientCount = elderlyFrequentVisits.Count,
                        Message = $"Found {elderlyFrequentVisits.Count} elderly patients (70+) with 4+ visits in last 6 months",
                        Recommendation = "Consider comprehensive health assessments and preventive care planning",
                        ConfidenceScore = Math.Min(elderlyFrequentVisits.Count / 10.0, 1.0)
                    });
                }

                // Alert 2: High abnormal lab rates
                var highAbnormalLabs = patients.Where(p => p.RecentAbnormalLabs > 3).ToList();
                if (highAbnormalLabs.Count >= 5)
                {
                    patternAlerts.Add(new
                    {
                        AlertType = "HIGH_ABNORMAL_LAB_RATE",
                        Severity = "MEDIUM",
                        PatientCount = highAbnormalLabs.Count,
                        Message = $"Found {highAbnormalLabs.Count} patients with 3+ abnormal lab results in last 3 months",
                        Recommendation = "Review lab protocols and follow-up procedures",
                        ConfidenceScore = Math.Min(highAbnormalLabs.Count / 20.0, 1.0)
                    });
                }

                // Alert 3: Low vaccination rates
                var lowVaccination = patients.Where(p => p.VaccinationCount < 2 && p.Age > 18).ToList();
                if (lowVaccination.Count >= 10)
                {
                    patternAlerts.Add(new
                    {
                        AlertType = "LOW_VACCINATION_RATE",
                        Severity = "HIGH",
                        PatientCount = lowVaccination.Count,
                        Message = $"Found {lowVaccination.Count} adult patients with incomplete vaccination records",
                        Recommendation = "Implement vaccination outreach programs",
                        ConfidenceScore = Math.Min(lowVaccination.Count / 50.0, 1.0)
                    });
                }

                var analytics = new
                {
                    Summary = new
                    {
                        TotalPatientsAnalyzed = patients.Count,
                        HighRiskPatients = highRiskPatients.Count,
                        OverallRiskPercentage = patients.Count > 0 ? Math.Round((double)highRiskPatients.Count / patients.Count * 100, 1) : 0,
                        AverageAge = Math.Round(patients.Average(p => p.Age), 1),
                        PatientsWithAllergies = patients.Count(p => p.AllergyCount > 0),
                        UnderVaccinated = patients.Count(p => p.VaccinationCount < 2),
                        TotalLabResults = patients.Sum(p => p.LabResultCount),
                        TotalDiagnoses = patients.Sum(p => p.DiagnosisCount)
                    },
                    BloodTypeAnalysis = bloodTypeAnalysis,
                    AgeRiskAnalysis = ageRiskAnalysis,
                    PatternAlerts = patternAlerts,
                    TopRiskPatients = highRiskPatients.OrderByDescending(p => p.GetType().GetProperty("RiskScore")?.GetValue(p)).Take(20),
                    HealthMetrics = new
                    {
                        AvgVisitsPerPatient = Math.Round(patients.Average(p => p.VisitCount), 1),
                        AvgAllergiesPerPatient = Math.Round(patients.Average(p => p.AllergyCount), 1),
                        AvgVaccinationsPerPatient = Math.Round(patients.Average(p => p.VaccinationCount), 1),
                        AvgLabResultsPerPatient = Math.Round(patients.Average(p => p.LabResultCount), 1),
                        PatientsWith5PlusVisits = patients.Count(p => p.VisitCount >= 5),
                        PatientsWithChronicConditions = patients.Count(p => p.HasChronicConditions)
                    },
                    GeneratedAt = DateTime.UtcNow,
                    NextAnalysisScheduled = DateTime.UtcNow.AddHours(6)
                };

                await _auditService.LogAuditAsync(User.GetUserId(), "MedicalAnalyticsGenerated", 
                    $"Generated medical analytics for {patients.Count} patients with {patternAlerts.Count} alerts", 
                    "Analytics", null, Request.GetClientIpAddress());

                return SuccessResponse(analytics, "Medical analytics completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating medical analytics");
                return InternalServerErrorResponse("An error occurred while generating medical analytics");
            }
        }

        /// <summary>
        /// Manually trigger comprehensive pattern detection analysis
        /// </summary>
        [HttpPost("trigger-pattern-analysis")]
        public async Task<IActionResult> TriggerPatternAnalysis()
        {
            try
            {
                _logger.LogInformation("Manual pattern detection triggered by admin: {UserId}", User.GetUserId());

                var result = await _patternDetectionService.RunCompleteAnalysisAsync();

                await _auditService.LogAuditAsync(User.GetUserId(), "PatternAnalysisTriggered", 
                    $"Manual pattern analysis completed - {result.Summary.TotalAlerts} alerts, {result.Summary.TotalHighRiskPatients} high-risk patients", 
                    "Analytics", null, Request.GetClientIpAddress());

                return SuccessResponse(result, "Pattern analysis completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering pattern analysis");
                return InternalServerErrorResponse("An error occurred while running pattern analysis");
            }
        }

        /// <summary>
        /// Get real-time health monitoring alerts
        /// </summary>
        [HttpGet("health-alerts")]
        public async Task<IActionResult> GetHealthAlerts()
        {
            try
            {
                var alerts = await _patternDetectionService.DetectPatternsAsync();

                var result = new
                {
                    Alerts = alerts,
                    Summary = new
                    {
                        TotalAlerts = alerts.Count,
                        HighSeverityAlerts = alerts.Count(a => a.Severity == "HIGH"),
                        MediumSeverityAlerts = alerts.Count(a => a.Severity == "MEDIUM"),
                        LowSeverityAlerts = alerts.Count(a => a.Severity == "LOW")
                    },
                    GeneratedAt = DateTime.UtcNow
                };

                return SuccessResponse(result, "Health alerts retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting health alerts");
                return InternalServerErrorResponse("An error occurred while retrieving health alerts");
            }
        }

        /// <summary>
        /// Get predictive health insights using ML
        /// </summary>
        [HttpGet("predictive-insights")]
        public async Task<IActionResult> GetPredictiveInsights()
        {
            try
            {
                var insights = await _patternDetectionService.GetPredictiveInsightsAsync();
                return SuccessResponse(insights, "Predictive insights generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating predictive insights");
                return InternalServerErrorResponse("An error occurred while generating predictive insights");
            }
        }

        /// <summary>
        /// Get detailed analytics for a specific patient
        /// </summary>
        [HttpGet("patients/{patientId}/analytics")]
        public async Task<IActionResult> GetPatientAnalytics(string patientId)
        {
            try
            {
                var analytics = await _patternDetectionService.GetPatientAnalyticsAsync(patientId);
                return SuccessResponse(analytics, "Patient analytics retrieved successfully");
            }
            catch (ArgumentException)
            {
                return NotFoundResponse("Patient not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting patient analytics for patient: {PatientId}", patientId);
                return InternalServerErrorResponse("An error occurred while retrieving patient analytics");
            }
        }

        /// <summary>
        /// Get correlation analysis between different health metrics
        /// </summary>
        [HttpGet("correlation-analysis")]
        public async Task<IActionResult> GetCorrelationAnalysis()
        {
            try
            {
                var correlations = await _patternDetectionService.AnalyzeCorrelationsAsync();
                return SuccessResponse(correlations, "Correlation analysis completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing correlation analysis");
                return InternalServerErrorResponse("An error occurred while performing correlation analysis");
            }
        }

        /// <summary>
        /// Get seasonal health trends
        /// </summary>
        [HttpGet("seasonal-trends")]
        public async Task<IActionResult> GetSeasonalTrends()
        {
            try
            {
                var trends = await _patternDetectionService.AnalyzeSeasonalTrendsAsync();
                return SuccessResponse(trends, "Seasonal trends analysis completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing seasonal trends");
                return InternalServerErrorResponse("An error occurred while analyzing seasonal trends");
            }
        }

        /// <summary>
        /// Seed large dataset with 10,000 users and comprehensive medical data
        /// </summary>
    [HttpPost("seed-large-dataset")]
    [AllowAnonymous]
        public async Task<IActionResult> SeedLargeDataset([FromQuery] int userCount = 10000)
        {
            try
            {
                var isAlreadySeeded = await _dataSeedingService.IsDataAlreadySeededAsync();
                if (isAlreadySeeded)
                {
                    return SuccessResponse(new { Message = "Dataset already seeded", UserCount = await _context.Users.CountAsync() }, 
                        "Dataset already exists");
                }

                _logger.LogInformation("Starting large dataset seeding with {UserCount} users", userCount);

                // Run seeding in background to avoid timeout (create a fresh DI scope)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var seeder = scope.ServiceProvider.GetRequiredService<IDataSeedingService>();
                        await seeder.SeedLargeDatasetAsync(userCount);
                        _logger.LogInformation("Large dataset seeding completed successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during background seeding");
                    }
                });

                await _auditService.LogAuditAsync(User.GetUserId(), "LargeDatasetSeedingStarted", 
                    $"Started seeding {userCount} users with medical data", 
                    "DataSeeding", null, Request.GetClientIpAddress());

                return SuccessResponse(new { Message = $"Started seeding {userCount} users in background", UserCount = userCount }, 
                    "Large dataset seeding started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting large dataset seeding");
                return InternalServerErrorResponse("An error occurred while starting dataset seeding");
            }
        }

        /// <summary>
        /// Get seeding status
        /// </summary>
    [HttpGet("seeding-status")]
    [AllowAnonymous]
        public async Task<IActionResult> GetSeedingStatus()
        {
            try
            {
                var userCount = await _context.Users.CountAsync();
                var labResultCount = await _context.LabResults.CountAsync();
                var diagnosisCount = await _context.Diagnoses.CountAsync();
                var visitCount = await _context.VisitRecords.CountAsync();
                var allergyCount = await _context.Allergies.CountAsync();
                var vaccinationCount = await _context.Vaccinations.CountAsync();

                var status = new
                {
                    TotalUsers = userCount,
                    TotalLabResults = labResultCount,
                    TotalDiagnoses = diagnosisCount,
                    TotalVisits = visitCount,
                    TotalAllergies = allergyCount,
                    TotalVaccinations = vaccinationCount,
                    IsLargeDatasetSeeded = userCount >= 1000,
                    AverageDataPerUser = userCount > 0 ? new
                    {
                        LabResults = Math.Round((double)labResultCount / userCount, 1),
                        Diagnoses = Math.Round((double)diagnosisCount / userCount, 1),
                        Visits = Math.Round((double)visitCount / userCount, 1),
                        Allergies = Math.Round((double)allergyCount / userCount, 1),
                        Vaccinations = Math.Round((double)vaccinationCount / userCount, 1)
                    } : null,
                    LastUpdated = DateTime.UtcNow
                };

                return SuccessResponse(status, "Seeding status retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting seeding status");
                return InternalServerErrorResponse("An error occurred while retrieving seeding status");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Calculate risk score for a patient
        /// </summary>
        private double CalculateRiskScore(int age, int recentVisits, int allergyCount, int vaccinationCount, bool hasChronicConditions, int abnormalLabs)
        {
            double score = 0;

            // Age factor (0-0.3)
            if (age > 80) score += 0.3;
            else if (age > 70) score += 0.2;
            else if (age > 60) score += 0.1;

            // Visit frequency factor (0-0.25)
            if (recentVisits > 6) score += 0.25;
            else if (recentVisits > 4) score += 0.15;
            else if (recentVisits > 2) score += 0.1;

            // Allergy factor (0-0.15)
            if (allergyCount > 5) score += 0.15;
            else if (allergyCount > 3) score += 0.1;
            else if (allergyCount > 1) score += 0.05;

            // Vaccination factor (0-0.1)
            if (vaccinationCount == 0) score += 0.1;
            else if (vaccinationCount == 1) score += 0.05;

            // Chronic conditions factor (0-0.15)
            if (hasChronicConditions) score += 0.15;

            // Abnormal labs factor (0-0.15)
            if (abnormalLabs > 5) score += 0.15;
            else if (abnormalLabs > 3) score += 0.1;
            else if (abnormalLabs > 1) score += 0.05;

            return Math.Min(score, 1.0);
        }

        /// <summary>
        /// Get risk level based on score
        /// </summary>
        private string GetRiskLevel(double riskScore)
        {
            if (riskScore >= 0.8) return "CRITICAL";
            if (riskScore >= 0.6) return "HIGH";
            if (riskScore >= 0.4) return "MEDIUM";
            if (riskScore >= 0.2) return "LOW";
            return "MINIMAL";
        }

        #endregion
    }
}
