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
        private readonly IAuthService _authService;
        private readonly IPatientAccessLogService _patientAccessLogService;

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
            IServiceScopeFactory scopeFactory,
            IAuthService authService,
            IPatientAccessLogService patientAccessLogService)
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
            _authService = authService;
            _patientAccessLogService = patientAccessLogService;
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
                _logger.LogInformation(role) ;

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
                    .ToListAsync();

                var usersWithRoles = new List<object>();

                foreach (var userEntity in users)
                {
                    var userRoles = userEntity != null
                        ? await _userManager.GetRolesAsync(userEntity)
                        : new List<string>();

                    if (userRoles.Contains("Doctor"))
                    {
                        // Map to DoctorProfileDto
                        usersWithRoles.Add(new
                        {
                            User = new DoctorProfileDto
                            {
                                Id = userEntity.Id,
                                FirstName = userEntity.FirstName,
                                LastName = userEntity.LastName,
                                Email = userEntity.Email ?? string.Empty,
                                PhoneNumber = userEntity.PhoneNumber ?? string.Empty,
                                IsActive = userEntity.IsActive,
                                Specialty = userEntity.Specialty.ToString(),
                                Experience = userEntity.Experience,
                                ClinicId = userEntity.ClinicId ?? "N/A",
                                DateOfBirth = userEntity.DateOfBirth,
                                Gender = (Gender)userEntity.Gender,
                                Address = userEntity.Address,
                                IDNP = userEntity.IDNP,
                                
                            },
                            Roles = userRoles
                        });
                    }
                    else
                    {
                        // Map to PatientProfileDto
                        usersWithRoles.Add(new
                        {
                            User = new PatientProfileDto
                            {
                                Id = userEntity.Id,
                                FirstName = userEntity.FirstName,
                                LastName = userEntity.LastName,
                                Email = userEntity.Email ?? string.Empty,
                                PhoneNumber = userEntity.PhoneNumber ?? string.Empty,
                                IDNP = userEntity.IDNP,
                                BloodType = userEntity.BloodType,
                                DateOfBirth = userEntity.DateOfBirth,
                                Address = userEntity.Address,
                                IsActive = userEntity.IsActive,
                                Gender = (Gender)userEntity.Gender
                            },
                            Roles = userRoles
                        });
                    }
                }
                _logger.LogInformation("Retrieved users from database: {Users}", users);

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
                    EmailConfirmed = true,
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
                return InternalServerErrorResponse("Internal server error");
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
                return InternalServerErrorResponse("Internal server error");
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
                return InternalServerErrorResponse("Internal server error");
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
                return InternalServerErrorResponse("Internal server error");
            }
        }

        #region Admin Management

        /// <summary>
        /// Get all admins with pagination
        /// </summary>
        [HttpGet("admins")]
        public async Task<IActionResult> GetAdmins([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] bool? isActive = null)
        {
            try
            {
                // Get all users with Admin role
                var adminsQuery = _userManager.Users.AsQueryable();

                // Filter by active status if provided
                if (isActive.HasValue)
                {
                    adminsQuery = adminsQuery.Where(u => u.IsActive == isActive.Value);
                }

                var allUsers = await adminsQuery.ToListAsync();
                var admins = new List<User>();

                // Filter users who have Admin role
                foreach (var user in allUsers)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Admin"))
                    {
                        admins.Add(user);
                    }
                }

                var totalCount = admins.Count;
                var paginatedAdmins = admins
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var adminDtos = paginatedAdmins.Select(admin => new
                {
                    admin.Id,
                    admin.FirstName,
                    admin.LastName,
                    admin.Email,
                    admin.PhoneNumber,
                    admin.IDNP,
                    admin.DateOfBirth,
                    admin.Address,
                    admin.IsActive,
                    admin.CreatedAt,
                    admin.UpdatedAt,
                    Roles = new[] { "Admin" }
                }).ToList();

                return SuccessResponse(new
                {
                    Admins = adminDtos,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }, "Admins retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admins");
                return InternalServerErrorResponse("An error occurred while retrieving admins");
            }
        }

        /// <summary>
        /// Get admin by ID
        /// </summary>
        [HttpGet("admins/{adminId}")]
        public async Task<IActionResult> GetAdminById(string adminId)
        {
            try
            {
                var admin = await _userManager.FindByIdAsync(adminId);
                if (admin == null)
                {
                    return NotFoundResponse("Admin not found");
                }

                var roles = await _userManager.GetRolesAsync(admin);
                if (!roles.Contains("Admin"))
                {
                    return NotFoundResponse("User is not an admin");
                }

                var adminDto = new
                {
                    admin.Id,
                    admin.FirstName,
                    admin.LastName,
                    admin.Email,
                    admin.PhoneNumber,
                    admin.IDNP,
                    admin.DateOfBirth,
                    admin.Gender,
                    admin.Address,
                    admin.IsActive,
                    admin.CreatedAt,
                    admin.UpdatedAt,
                    Roles = roles.ToList()
                };

                return SuccessResponse(adminDto, "Admin retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin: {AdminId}", adminId);
                return InternalServerErrorResponse("An error occurred while retrieving admin");
            }
        }

        /// <summary>
        /// Create new admin
        /// </summary>
        [HttpPost("admins")]
        public async Task<IActionResult> CreateAdmin([FromBody] CreateUserDTO createAdminDto)
        {
            try
            {
                var validationResult = ValidateModel();
                if (validationResult != null)
                    return validationResult;

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(createAdminDto.Email);
                if (existingUser != null)
                {
                    return ErrorResponse("User with this email already exists");
                }

                // Check IDNP uniqueness
                var existingIDNP = await _context.Users.FirstOrDefaultAsync(u => u.IDNP == createAdminDto.IDNP);
                if (existingIDNP != null)
                {
                    return ErrorResponse("User with this IDNP already exists");
                }

                // Validate required fields
                if (!createAdminDto.DateOfBirth.HasValue)
                {
                    return ValidationErrorResponse("Date of birth is required");
                }

                var admin = new User
                {
                    UserName = createAdminDto.Email,
                    Email = createAdminDto.Email,
                    FirstName = createAdminDto.FirstName,
                    LastName = createAdminDto.LastName,
                    PhoneNumber = createAdminDto.PhoneNumber,
                    IDNP = createAdminDto.IDNP,
                    DateOfBirth = createAdminDto.DateOfBirth.Value,
                    Address = createAdminDto.Address,
                    IsActive = true,
                    EmailConfirmed = true,
                    Gender = Gender.Male // Default, can be updated later
                };

                var result = await _userManager.CreateAsync(admin, createAdminDto.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return ValidationErrorResponse("Failed to create admin", errors);
                }

                // Assign Admin role
                await _userManager.AddToRoleAsync(admin, "Admin");

                // Audit log
                await _auditService.LogAuditAsync(User.GetUserId(), "AdminCreated", $"Created admin: {createAdminDto.Email}", "User", null, Request.GetClientIpAddress());

                var adminDto = new
                {
                    admin.Id,
                    admin.FirstName,
                    admin.LastName,
                    admin.Email,
                    admin.PhoneNumber,
                    admin.IDNP,
                    admin.DateOfBirth,
                    admin.Address,
                    admin.IsActive,
                    admin.CreatedAt,
                    Roles = new[] { "Admin" }
                };

                return SuccessResponse(adminDto, "Admin created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating admin: {Email}", createAdminDto.Email);
                return InternalServerErrorResponse("An error occurred while creating admin");
            }
        }

        /// <summary>
        /// Update admin
        /// </summary>
        [HttpPut("admins/{adminId}")]
        public async Task<IActionResult> UpdateAdmin(string adminId, [FromBody] UpdateUserDTO updateAdminDto)
        {
            try
            {
                var validationResult = ValidateModel();
                if (validationResult != null)
                    return validationResult;

                var admin = await _userManager.FindByIdAsync(adminId);
                if (admin == null)
                {
                    return NotFoundResponse("Admin not found");
                }

                var roles = await _userManager.GetRolesAsync(admin);
                if (!roles.Contains("Admin"))
                {
                    return NotFoundResponse("User is not an admin");
                }

                // Check IDNP uniqueness (excluding current admin)
                if (!string.IsNullOrEmpty(updateAdminDto.IDNP) && updateAdminDto.IDNP != admin.IDNP)
                {
                    var existingIDNP = await _context.Users
                        .FirstOrDefaultAsync(u => u.IDNP == updateAdminDto.IDNP && u.Id != adminId);
                    if (existingIDNP != null)
                    {
                        return ErrorResponse("User with this IDNP already exists");
                    }
                }

                // Update admin properties
                admin.FirstName = updateAdminDto.FirstName ?? admin.FirstName;
                admin.LastName = updateAdminDto.LastName ?? admin.LastName;
                admin.PhoneNumber = updateAdminDto.PhoneNumber ?? admin.PhoneNumber;
                admin.IDNP = updateAdminDto.IDNP ?? admin.IDNP;
                admin.DateOfBirth = updateAdminDto.DateOfBirth ?? admin.DateOfBirth;
                admin.Address = updateAdminDto.Address ?? admin.Address;
                admin.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(admin);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return ErrorResponse("Failed to update admin", errors);
                }

                // Audit log
                await _auditService.LogAuditAsync(User.GetUserId(), "AdminUpdated", $"Updated admin: {admin.Email}", "User", null, Request.GetClientIpAddress());

                var adminDto = new
                {
                    admin.Id,
                    admin.FirstName,
                    admin.LastName,
                    admin.Email,
                    admin.PhoneNumber,
                    admin.IDNP,
                    admin.DateOfBirth,
                    admin.Address,
                    admin.IsActive,
                    admin.UpdatedAt,
                    Roles = roles.ToList()
                };

                return SuccessResponse(adminDto, "Admin updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating admin: {AdminId}", adminId);
                return InternalServerErrorResponse("An error occurred while updating admin");
            }
        }

        /// <summary>
        /// Activate/Deactivate admin
        /// </summary>
        [HttpPatch("admins/{adminId}/status")]
        public async Task<IActionResult> ToggleAdminStatus(string adminId, [FromBody] ToggleStatusDTO statusDto)
        {
            try
            {
                var admin = await _userManager.FindByIdAsync(adminId);
                if (admin == null)
                {
                    return NotFoundResponse("Admin not found");
                }

                var roles = await _userManager.GetRolesAsync(admin);
                if (!roles.Contains("Admin"))
                {
                    return NotFoundResponse("User is not an admin");
                }

                // Prevent deactivating self
                var currentUserId = User.GetUserId();
                if (adminId == currentUserId && !statusDto.IsActive)
                {
                    return ErrorResponse("You cannot deactivate your own account");
                }

                admin.IsActive = statusDto.IsActive;
                admin.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(admin);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return ErrorResponse("Failed to update admin status", errors);
                }

                // Audit log
                await _auditService.LogAuditAsync(User.GetUserId(), 
                    statusDto.IsActive ? "AdminActivated" : "AdminDeactivated", 
                    $"{(statusDto.IsActive ? "Activated" : "Deactivated")} admin: {admin.Email}", 
                    "User", null, Request.GetClientIpAddress());

                return SuccessResponse(new { admin.Id, admin.IsActive }, 
                    $"Admin {(statusDto.IsActive ? "activated" : "deactivated")} successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling admin status: {AdminId}", adminId);
                return InternalServerErrorResponse("An error occurred while updating admin status");
            }
        }

        /// <summary>
        /// Delete admin (soft delete)
        /// </summary>
        [HttpDelete("admins/{adminId}")]
        public async Task<IActionResult> DeleteAdmin(string adminId)
        {
            try
            {
                var admin = await _userManager.FindByIdAsync(adminId);
                if (admin == null)
                {
                    return NotFoundResponse("Admin not found");
                }

                var roles = await _userManager.GetRolesAsync(admin);
                if (!roles.Contains("Admin"))
                {
                    return NotFoundResponse("User is not an admin");
                }

                // Prevent deleting self
                var currentUserId = User.GetUserId();
                if (adminId == currentUserId)
                {
                    return ErrorResponse("You cannot delete your own account");
                }

                // Check if this is the last active admin
                var allUsers = await _userManager.Users.Where(u => u.IsActive).ToListAsync();
                var activeAdmins = new List<User>();
                foreach (var user in allUsers)
                {
                    var userRoles = await _userManager.GetRolesAsync(user);
                    if (userRoles.Contains("Admin"))
                    {
                        activeAdmins.Add(user);
                    }
                }

                if (activeAdmins.Count <= 1)
                {
                    return ErrorResponse("Cannot delete the last active admin. Please create another admin first.");
                }

                // Soft delete - deactivate instead of hard delete
                admin.IsActive = false;
                admin.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(admin);

                // Audit log
                await _auditService.LogAuditAsync(User.GetUserId(), "AdminDeleted", $"Deleted admin: {admin.Email}", "User", null, Request.GetClientIpAddress());

                return SuccessResponse(null, "Admin deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting admin: {AdminId}", adminId);
                return InternalServerErrorResponse("An error occurred while deleting admin");
            }
        }

        #endregion

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

                var activeDoctors = (await _userManager.GetUsersInRoleAsync("Doctor"))
                        .Count(d => d.IsActive);


                var statistics = new
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    InactiveUsers = totalUsers - activeUsers,
                    TotalDoctors = totalDoctors,
                    TotalPatients = totalPatients,
                    TotalVisits = totalVisits,
                    ActiveDoctors = activeDoctors,
                    TotalDocuments = totalDocuments,
                    RecentVisits = recentVisits,
                    GeneratedAt = DateTime.UtcNow
                };

                return SuccessResponse(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics");
                return InternalServerErrorResponse("Internal server error");
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

        #region Patient-Doctor Relationship Management

        /// <summary>
        /// Get all patient-doctor relationships with pagination
        /// </summary>
        [HttpGet("patient-doctor-relationships")]
        public async Task<IActionResult> GetPatientDoctorRelationships([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool? isActive = null)
        {
            try
            {
                var query = _context.PatientDoctors
                    .Include(pd => pd.Patient)
                    .Include(pd => pd.Doctor)
                    .AsQueryable();

                if (isActive.HasValue)
                {
                    query = query.Where(pd => pd.IsActive == isActive.Value);
                }

                var totalCount = await query.CountAsync();
                var relationships = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(pd => new
                    {
                        Id = pd.Id,
                        PatientId = pd.PatientId,
                        PatientName = pd.Patient.FirstName + " " + pd.Patient.LastName,
                        PatientIDNP = pd.Patient.IDNP,
                        DoctorId = pd.DoctorId,
                        DoctorName = pd.Doctor.FirstName + " " + pd.Doctor.LastName,
                        DoctorIDNP = pd.Doctor.IDNP,
                        AssignedDate = pd.AssignedDate,
                        IsActive = pd.IsActive,
                        AssignedBy = pd.AssignedBy,
                        Notes = pd.Notes,
                        DeactivatedDate = pd.DeactivatedDate
                    })
                    .ToListAsync();

                return SuccessResponse(new
                {
                    relationships = relationships,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalItems = totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                }, "Patient-doctor relationships retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient-doctor relationships");
                return InternalServerErrorResponse("An error occurred while retrieving relationships");
            }
        }

        /// <summary>
        /// Assign a patient to a doctor (admin override)
        /// </summary>
        [HttpPost("assign-patient-to-doctor")]
        public async Task<IActionResult> AssignPatientToDoctor([FromBody] AdminAssignPatientDoctorDto dto)
        {
            try
            {
                var validationResult = ValidateModel();
                if (validationResult != null)
                    return validationResult;

                // Verify patient exists and is a patient
                var patient = await _context.Users
                    .Where(u => u.Id == dto.PatientId && u.UserRoles.Any(r => r.Role.Name == "Patient"))
                    .FirstOrDefaultAsync();

                if (patient == null)
                {
                    return NotFoundResponse("Patient not found");
                }

                // Verify doctor exists and is a doctor
                var doctor = await _context.Users
                    .Where(u => u.Id == dto.DoctorId && u.UserRoles.Any(r => r.Role.Name == "Doctor"))
                    .FirstOrDefaultAsync();

                if (doctor == null)
                {
                    return NotFoundResponse("Doctor not found");
                }

                // Check if relationship already exists
                var existingRelation = await _context.PatientDoctors
                    .Where(pd => pd.PatientId == dto.PatientId && pd.DoctorId == dto.DoctorId && pd.IsActive)
                    .FirstOrDefaultAsync();

                if (existingRelation != null)
                {
                    return ValidationErrorResponse("Patient is already assigned to this doctor");
                }

                // Create new relationship
                var patientDoctor = new PatientDoctor
                {
                    PatientId = dto.PatientId,
                    DoctorId = dto.DoctorId,
                    AssignedDate = DateTime.UtcNow,
                    IsActive = true,
                    Notes = dto.Notes,
                    AssignedBy = "Admin"
                };

                _context.PatientDoctors.Add(patientDoctor);
                await _context.SaveChangesAsync();

                // Log audit
                await _auditService.LogAuditAsync(
                    User.GetUserId(),
                    "AdminPatientDoctorAssignment",
                    $"Admin assigned patient {patient.FirstName} {patient.LastName} (ID: {dto.PatientId}) to doctor {doctor.FirstName} {doctor.LastName} (ID: {dto.DoctorId})",
                    "PatientDoctor",
                    null,
                    Request.GetClientIpAddress());

                return SuccessResponse(new
                {
                    Id = patientDoctor.Id,
                    PatientName = patient.FirstName + " " + patient.LastName,
                    DoctorName = doctor.FirstName + " " + doctor.LastName,
                    AssignedDate = patientDoctor.AssignedDate,
                    AssignedBy = patientDoctor.AssignedBy
                }, "Patient successfully assigned to doctor");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning patient to doctor");
                return InternalServerErrorResponse("An error occurred while assigning patient to doctor");
            }
        }

        /// <summary>
        /// Remove patient-doctor relationship (admin override)
        /// </summary>
        [HttpPost("remove-patient-doctor-relationship")]
        public async Task<IActionResult> RemovePatientDoctorRelationship([FromBody] AdminRemovePatientDoctorDto dto)
        {
            try
            {
                var validationResult = ValidateModel();
                if (validationResult != null)
                    return validationResult;

                var relationship = await _context.PatientDoctors
                    .Include(pd => pd.Patient)
                    .Include(pd => pd.Doctor)
                    .Where(pd => pd.Id == dto.RelationshipId && pd.IsActive)
                    .FirstOrDefaultAsync();

                if (relationship == null)
                {
                    return NotFoundResponse("Patient-doctor relationship not found or already inactive");
                }

                // Deactivate the relationship
                relationship.IsActive = false;
                relationship.DeactivatedDate = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(dto.Reason))
                {
                    relationship.Notes = string.IsNullOrWhiteSpace(relationship.Notes)
                        ? $"Removed by Admin: {dto.Reason}"
                        : $"{relationship.Notes}\nRemoved by Admin: {dto.Reason}";
                }

                await _context.SaveChangesAsync();

                // Log audit
                await _auditService.LogAuditAsync(
                    User.GetUserId(),
                    "AdminPatientDoctorRemoval",
                    $"Admin removed relationship between patient {relationship.Patient.FirstName} {relationship.Patient.LastName} and doctor {relationship.Doctor.FirstName} {relationship.Doctor.LastName}. Reason: {dto.Reason}",
                    "PatientDoctor",
                    null,
                    Request.GetClientIpAddress());

                return SuccessResponse(null, "Patient-doctor relationship removed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing patient-doctor relationship");
                return InternalServerErrorResponse("An error occurred while removing the relationship");
            }
        }

        /// <summary>
        /// Get statistics about patient-doctor relationships
        /// </summary>
        [HttpGet("patient-doctor-statistics")]
        public async Task<IActionResult> GetPatientDoctorStatistics()
        {
            try
            {
                var totalActiveRelationships = await _context.PatientDoctors
                    .CountAsync(pd => pd.IsActive);

                var totalInactiveRelationships = await _context.PatientDoctors
                    .CountAsync(pd => !pd.IsActive);

                var patientsWithDoctors = await _context.PatientDoctors
                    .Where(pd => pd.IsActive)
                    .Select(pd => pd.PatientId)
                    .Distinct()
                    .CountAsync();

                var doctorsWithPatients = await _context.PatientDoctors
                    .Where(pd => pd.IsActive)
                    .Select(pd => pd.DoctorId)
                    .Distinct()
                    .CountAsync();

                var totalPatients = await _context.Users
                    .Where(u => u.UserRoles.Any(r => r.Role.Name == "Patient"))
                    .CountAsync();

                var totalDoctors = await _context.Users
                    .Where(u => u.UserRoles.Any(r => r.Role.Name == "Doctor"))
                    .CountAsync();

                var averagePatientsPerDoctor = doctorsWithPatients > 0
                    ? (double)patientsWithDoctors / doctorsWithPatients
                    : 0;

                var topDoctorsByPatients = await _context.PatientDoctors
                    .Where(pd => pd.IsActive)
                    .Include(pd => pd.Doctor)
                    .GroupBy(pd => new { pd.DoctorId, pd.Doctor.FirstName, pd.Doctor.LastName })
                    .Select(g => new
                    {
                        DoctorId = g.Key.DoctorId,
                        DoctorName = g.Key.FirstName + " " + g.Key.LastName,
                        PatientCount = g.Count()
                    })
                    .OrderByDescending(x => x.PatientCount)
                    .Take(10)
                    .ToListAsync();

                return SuccessResponse(new
                {
                    TotalActiveRelationships = totalActiveRelationships,
                    TotalInactiveRelationships = totalInactiveRelationships,
                    PatientsWithDoctors = patientsWithDoctors,
                    PatientsWithoutDoctors = totalPatients - patientsWithDoctors,
                    DoctorsWithPatients = doctorsWithPatients,
                    DoctorsWithoutPatients = totalDoctors - doctorsWithPatients,
                    AveragePatientsPerDoctor = Math.Round(averagePatientsPerDoctor, 2),
                    TopDoctorsByPatients = topDoctorsByPatients
                }, "Patient-doctor statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient-doctor statistics");
                return InternalServerErrorResponse("An error occurred while retrieving statistics");
            }
        }

        [HttpGet("dashboard")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var totalDoctors = (await _userManager.GetUsersInRoleAsync("Doctor")).Count;
                var totalPatients = (await _userManager.GetUsersInRoleAsync("Patient")).Count;

                // Count active doctors
                var activeDoctorsCount = await _context.Users.CountAsync(u => u.IsActive && u.UserRoles.Any(r => r.Role.Name == "Doctor"));

                return Ok(new
                {
                    data = new
                    {
                        totalDoctors,
                        activeDoctorsCount,
                        totalPatients
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving admin dashboard data");
                return StatusCode(500, new { message = "An error occurred while retrieving the admin dashboard data" });
            }
        }

        [HttpPost("toggle-status/{id}")]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound();

            // Flip status
            user.IsActive = !user.IsActive;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Status updated", isActive = user.IsActive });
        }

        #endregion

        [HttpPost("CreateDoctor")]
        [Authorize(Roles = "Admin")]
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
                    Specialty = dto.Specialty.ToString(),
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

        [HttpPost("CreatePatient")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreatePatient([FromBody] UserDto dto)
        {
            try
            {
                // Map UserDto to RegisterDto
                var patientRegisterDto = new RegisterDto
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Gender = dto.Gender,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    DateOfBirth = dto.DateOfBirth,
                    Address = dto.Address,
                    IDNP = dto.IDNP,
                    UserRole = UserRegistrationType.Patient,
                    Password = "DefaultPassword123!", // you can generate a temporary password
                    ConfirmPassword = "DefaultPassword123!"
                };

                // Use the AuthService to register the patient
                var result = await _authService.RegisterAsync(patientRegisterDto);

                if (!result.Success)
                {
                    _logger.LogWarning("Failed to create patient: {Email}, Errors: {Errors}", dto.Email, string.Join(", ", result.Errors));
                    return BadRequest(new { message = result.Message, errors = result.Errors });
                }

                var user = result.AuthResponse!.User;

                // Map the returned user to PatientProfileDto
                var patientDto = new PatientProfileDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Gender = (Gender)user.Gender,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    IDNP = user.IDNP,
                    Address = user.Address,
                    DateOfBirth = user.DateOfBirth,
                    IsActive = user.IsActive
                };

                _logger.LogInformation("Patient created successfully: {Email}", user.Email);
                return Ok(patientDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating patient: {Email}", dto.Email);
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }

        [HttpPut("updateDoctor/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateDoctor(string id, [FromBody] DoctorUpdateDto dto)
        {
        try{
            var doctor = await _context.Users
                .Where(u => u.Id == id && u.UserRoles.Any(r => r.Role.Name == "Doctor"))
                .FirstOrDefaultAsync();

            if (doctor == null)
                return NotFound("Doctor not found.");

                // Update only allowed fields
                if (dto.FirstName != null) doctor.FirstName = dto.FirstName;
                if (dto.LastName != null) doctor.LastName = dto.LastName;
                if (dto.Email != null) doctor.Email = dto.Email;
                if (dto.PhoneNumber != null) doctor.PhoneNumber = dto.PhoneNumber;
                if (dto.Address != null) doctor.Address = dto.Address;
                if (dto.IsActive.HasValue) doctor.IsActive = dto.IsActive.Value;
                if (dto.ClinicId != null) doctor.ClinicId = dto.ClinicId;
                if (dto.Specialty.HasValue) doctor.Specialty = dto.Specialty.Value;
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
                    Specialty = doctor.Specialty.ToString(),
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

        [HttpPut("updatePatient/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePatient(string id, [FromBody] UserDto dto)
        {
        try{
            var patient = await _context.Users
                .Where(u => u.Id == id && u.UserRoles.Any(r => r.Role.Name == "Patient"))
                .FirstOrDefaultAsync();

            if (patient == null)
                return NotFound("Patient not found.");

                // Update only allowed fields
                if (dto.PhoneNumber != null) patient.PhoneNumber = dto.PhoneNumber;
                if (dto.FirstName != null) patient.FirstName = dto.FirstName;
                if (dto.LastName != null) patient.LastName = dto.LastName;
                if (dto.Address != null) patient.Address = dto.Address;
                if (dto.Email != null) patient.Email = dto.Email;
                if (dto.BloodType.HasValue) patient.BloodType = dto.BloodType.Value.ToString();

                await _context.SaveChangesAsync();

                // Return updated patient DTO
                var patientDto = new PatientProfileDto
                {
                    Id = patient.Id,
                    FirstName = patient.FirstName,
                    LastName = patient.LastName,
                    Email = patient.Email ?? string.Empty,
                    PhoneNumber = patient.PhoneNumber,
                    IDNP = patient.IDNP,
                    IsActive = patient.IsActive
                };

                return Ok(patientDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating patient with userId: {UserId}", User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }

        [HttpGet("{doctorId}/patient-count")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDoctorPatientCount(string doctorId)
        {
            try
            {
                // Count all active patients for this doctor
                var totalPatients = await _context.PatientDoctors
                    .Where(pd => pd.DoctorId == doctorId && pd.IsActive)
                    .CountAsync();

                return Ok(new
                {
                    success = true,
                    doctorId = doctorId,
                    totalPatients
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting patient count for doctor: {DoctorId}", doctorId);
                return StatusCode(500, new { message = "An error occurred while retrieving patient count" });
            }
        }

        // getting appoiments for doctors
        [HttpGet("{doctorId}/appointments")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAppointmentsForDoctor(
            string doctorId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
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

                return Ok(new
                {
                    success = true,
                    data = items,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalItems = total,
                        totalPages = (int)Math.Ceiling((double)total / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving appointments for doctor {DoctorId}", doctorId);
                return StatusCode(500, new { message = "Failed to retrieve appointments" });
            }
        }



        [HttpDelete("deleteDoctor/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDoctor(string id)
        {
            try
            {
                var doctor = await _context.Users
                    .Include(u => u.DoctorAppointments) // include dependent entities
                    .Where(u => u.Id == id && u.UserRoles.Any(r => r.Role.Name == "Doctor"))
                    .FirstOrDefaultAsync();

                if (doctor == null)
                    return NotFound("Doctor not found.");

                if (doctor.DoctorAppointments.Any())
                    return BadRequest("Cannot delete doctor with existing appointments.");

                _context.Users.Remove(doctor);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Doctor deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting doctor with id: {DoctorId}", id);
                return StatusCode(500, new { message = "Internal server error", details = ex.Message });
            }
        }

        /// <summary>
        /// Get all patient access logs with filtering and pagination
        /// </summary>
        [HttpGet("patient-access-logs")]
        public async Task<IActionResult> GetPatientAccessLogs([FromQuery] PatientAccessLogQueryDto query)
        {
            try
            {
                var accessLogs = await _patientAccessLogService.GetPatientAccessLogsAsync(query);

                var totalCount = await _context.PatientAccessLogs
                    .Where(pal => 
                        (string.IsNullOrEmpty(query.PatientId) || pal.PatientId == query.PatientId) &&
                        (string.IsNullOrEmpty(query.DoctorId) || pal.DoctorId == query.DoctorId) &&
                        (!query.FromDate.HasValue || pal.AccessedAt >= query.FromDate.Value) &&
                        (!query.ToDate.HasValue || pal.AccessedAt <= query.ToDate.Value) &&
                        (string.IsNullOrEmpty(query.AccessType) || pal.AccessType == query.AccessType))
                    .CountAsync();

                return SuccessResponse(new
                {
                    accessLogs = accessLogs,
                    pagination = new
                    {
                        currentPage = query.Page,
                        pageSize = query.PageSize,
                        totalItems = totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / query.PageSize)
                    }
                }, "Patient access logs retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient access logs");
                return InternalServerErrorResponse("An error occurred while retrieving patient access logs");
            }
        }

        /// <summary>
        /// Get access summary for a specific patient
        /// </summary>
        [HttpGet("patient-access-logs/{patientId}/summary")]
        public async Task<IActionResult> GetPatientAccessSummary(string patientId)
        {
            try
            {
                // Verify patient exists
                var patient = await _context.Users
                    .Where(u => u.Id == patientId && u.UserRoles.Any(r => r.Role.Name == "Patient"))
                    .FirstOrDefaultAsync();

                if (patient == null)
                {
                    return NotFoundResponse("Patient not found");
                }

                var summary = await _patientAccessLogService.GetPatientAccessSummaryAsync(patientId);
                
                return SuccessResponse(new
                {
                    patientId = patientId,
                    patientName = patient.FirstName + " " + patient.LastName,
                    summary = summary
                }, "Patient access summary retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving patient access summary for patient {PatientId}", patientId);
                return InternalServerErrorResponse("An error occurred while retrieving patient access summary");
            }
        }

        /// <summary>
        /// Get doctor's access history (what patients they've accessed)
        /// </summary>
        [HttpGet("doctor-access-history/{doctorId}")]
        public async Task<IActionResult> GetDoctorAccessHistory(string doctorId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                // Verify doctor exists
                var doctor = await _context.Users
                    .Where(u => u.Id == doctorId && u.UserRoles.Any(r => r.Role.Name == "Doctor"))
                    .FirstOrDefaultAsync();

                if (doctor == null)
                {
                    return NotFoundResponse("Doctor not found");
                }

                var accessHistory = await _patientAccessLogService.GetDoctorAccessHistoryAsync(doctorId, page, pageSize);
                
                var totalCount = await _context.PatientAccessLogs
                    .CountAsync(pal => pal.DoctorId == doctorId);

                return SuccessResponse(new
                {
                    doctorId = doctorId,
                    doctorName = doctor.FirstName + " " + doctor.LastName,
                    accessHistory = accessHistory,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalItems = totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                }, "Doctor access history retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving doctor access history for doctor {DoctorId}", doctorId);
                return InternalServerErrorResponse("An error occurred while retrieving doctor access history");
            }
        }

        /// <summary>
        /// Get access log statistics
        /// </summary>
        [HttpGet("patient-access-logs/statistics")]
        public async Task<IActionResult> GetAccessLogStatistics()
        {
            try
            {
                var today = DateTime.Today;
                var thisWeek = today.AddDays(-7);
                var thisMonth = today.AddDays(-30);

                var totalAccesses = await _context.PatientAccessLogs.CountAsync();
                var todayAccesses = await _context.PatientAccessLogs
                    .CountAsync(pal => pal.AccessedAt >= today);
                var weekAccesses = await _context.PatientAccessLogs
                    .CountAsync(pal => pal.AccessedAt >= thisWeek);
                var monthAccesses = await _context.PatientAccessLogs
                    .CountAsync(pal => pal.AccessedAt >= thisMonth);

                var topDoctorsByAccess = await _context.PatientAccessLogs
                    .Include(pal => pal.Doctor)
                    .Where(pal => pal.AccessedAt >= thisMonth)
                    .GroupBy(pal => new { pal.DoctorId, pal.Doctor.FirstName, pal.Doctor.LastName })
                    .Select(g => new
                    {
                        DoctorId = g.Key.DoctorId,
                        DoctorName = g.Key.FirstName + " " + g.Key.LastName,
                        AccessCount = g.Count()
                    })
                    .OrderByDescending(x => x.AccessCount)
                    .Take(10)
                    .ToListAsync();

                var accessTypeStats = await _context.PatientAccessLogs
                    .Where(pal => pal.AccessedAt >= thisMonth)
                    .GroupBy(pal => pal.AccessType)
                    .Select(g => new
                    {
                        AccessType = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();

                return SuccessResponse(new
                {
                    TotalAccesses = totalAccesses,
                    TodayAccesses = todayAccesses,
                    WeekAccesses = weekAccesses,
                    MonthAccesses = monthAccesses,
                    TopDoctorsByAccess = topDoctorsByAccess,
                    AccessTypeStatistics = accessTypeStats
                }, "Access log statistics retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving access log statistics");
                return InternalServerErrorResponse("An error occurred while retrieving access log statistics");
            }
        }
    }
}
