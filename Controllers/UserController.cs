using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using medical_be.Services;
using medical_be.DTOs;
using medical_be.Controllers.Base;
using medical_be.Shared.Interfaces;

namespace medical_be.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly IFileService _fileService;
    private readonly ILogger<UserController> _logger;

    public UserController(
        IAuthService authService,
        IFileService fileService,
        ILogger<UserController> logger)
    {
        _authService = authService;
        _fileService = fileService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    [HttpPost("me")]
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return UnauthorizedResponse("User not found");
            }

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFoundResponse("User not found");
            }

            // Get user's profile picture if exists
            var profilePictureSearch = new FileSearchDto
            {
                ModelType = "User",
                ModelId = userId,
                Category = "ProfilePhoto",
                PageSize = 1
            };

            var profilePictures = await _fileService.GetFilesAsync(profilePictureSearch);
            var profilePicture = profilePictures.FirstOrDefault();

            // Get user roles
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            // Create response with profile picture
            var userResponse = new
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                IDNP = user.IDNP,
                DateOfBirth = user.DateOfBirth,
                Address = user.Address,
                BloodType = user.BloodType,
                Gender = user.Gender,
                IsActive = user.IsActive,
                Roles = roles,
                ProfilePicture = profilePicture != null ? new
                {
                    profilePicture.Id,
                    profilePicture.Name,
                    profilePicture.DownloadUrl,
                    profilePicture.ThumbnailUrl
                } : null
            };

            return SuccessResponse(userResponse, "User retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user");
            return InternalServerErrorResponse("An error occurred while retrieving user information");
        }
    }

    /// <summary>
    /// Logout current user (client-side token removal)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} logged out", userId);
            
            return SuccessResponse(null, "Logged out successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return InternalServerErrorResponse("An error occurred during logout");
        }
    }
}
