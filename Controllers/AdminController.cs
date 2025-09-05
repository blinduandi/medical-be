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
using AutoMapper;

namespace medical_be.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IMapper _mapper;
        private readonly IAuditService _auditService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IMapper mapper,
            IAuditService auditService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
            _auditService = auditService;
            _logger = logger;
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

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return StatusCode(500, "Internal server error");
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
                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(createUserDto.Email);
                if (existingUser != null)
                {
                    return BadRequest("User with this email already exists");
                }

                // Check IDNP uniqueness
                var existingIDNP = await _context.Users.FirstOrDefaultAsync(u => u.IDNP == createUserDto.IDNP);
                if (existingIDNP != null)
                {
                    return BadRequest("User with this IDNP already exists");
                }

                // Validate required fields
                if (!createUserDto.DateOfBirth.HasValue)
                {
                    return BadRequest("Date of birth is required");
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
                    return BadRequest(result.Errors);
                }

                // Assign role
                if (!string.IsNullOrEmpty(createUserDto.Role))
                {
                    await _userManager.AddToRoleAsync(user, createUserDto.Role);
                }

                // Audit log
                await _auditService.LogAuditAsync(User.GetUserId(), "UserCreated", $"Created user: {createUserDto.Email}", "User", null, Request.GetClientIpAddress());

                var userDto = _mapper.Map<PatientProfileDto>(user);
                return CreatedAtAction(nameof(GetUsers), userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {Email}", createUserDto.Email);
                return StatusCode(500, "Internal server error");
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
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                // Check IDNP uniqueness (excluding current user)
                if (!string.IsNullOrEmpty(updateUserDto.IDNP) && updateUserDto.IDNP != user.IDNP)
                {
                    var existingIDNP = await _context.Users
                        .FirstOrDefaultAsync(u => u.IDNP == updateUserDto.IDNP && u.Id != userId);
                    if (existingIDNP != null)
                    {
                        return BadRequest("User with this IDNP already exists");
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
                    return BadRequest(result.Errors);
                }

                // Audit log
                await _auditService.LogAuditAsync(User.GetUserId(), "UserUpdated", $"Updated user: {user.Email}", "User", null, Request.GetClientIpAddress());

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserId}", userId);
                return StatusCode(500, "Internal server error");
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
                    return NotFound("User not found");
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

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", userId);
                return StatusCode(500, "Internal server error");
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
                    return NotFound("User not found");
                }

                var roleExists = await _roleManager.RoleExistsAsync(assignRoleDto.RoleName);
                if (!roleExists)
                {
                    return BadRequest("Role does not exist");
                }

                var result = await _userManager.AddToRoleAsync(user, assignRoleDto.RoleName);
                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors);
                }

                // Audit log
                await _auditService.LogAuditAsync(User.GetUserId(), "RoleAssigned", $"Assigned role {assignRoleDto.RoleName} to user: {user.Email}", "User", null, Request.GetClientIpAddress());

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role to user: {UserId}", userId);
                return StatusCode(500, "Internal server error");
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
                    return NotFound("User not found");
                }

                var result = await _userManager.RemoveFromRoleAsync(user, roleName);
                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors);
                }

                // Audit log
                await _auditService.LogAuditAsync(User.GetUserId(), "RoleRemoved", $"Removed role {roleName} from user: {user.Email}", "User", null, Request.GetClientIpAddress());

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role from user: {UserId}", userId);
                return StatusCode(500, "Internal server error");
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

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
