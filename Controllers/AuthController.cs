using Microsoft.AspNetCore.Mvc;
using medical_be.DTOs;
using medical_be.Services;
using medical_be.Shared.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace medical_be.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            var result = await _authService.RegisterAsync(registerDto);
            if (result == null)
            {
                return BadRequest(new { message = "Registration failed. Email might already be in use." });
            }

            _logger.LogInformation("User registered successfully: {Email}", registerDto.Email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            var result = await _authService.LoginAsync(loginDto);
            if (result == null)
            {
                return Unauthorized(new { message = "Invalid credentials or account is inactive" });
            }

            _logger.LogInformation("User logged in successfully: {Email}", loginDto.Email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user login");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("verify-mfa")]
    public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaLoginDto verifyMfaDto)
    {
        try
        {
            var result = await _authService.VerifyMfaLoginAsync(verifyMfaDto.Email, verifyMfaDto.Otp);
            if (result == null)
            {
                return Unauthorized(new { message = "Invalid OTP or user not found" });
            }

            _logger.LogInformation("User MFA verification successful: {Email}", verifyMfaDto.Email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during MFA verification");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            var result = await _authService.ChangePasswordAsync(userId, changePasswordDto);
            if (!result)
            {
                return BadRequest(new { message = "Failed to change password. Check your current password." });
            }

            _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
            return Ok(new { message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password change");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user information");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateUserDto updateUserDto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not found" });
            }

            var result = await _authService.UpdateUserAsync(userId, updateUserDto);
            if (result == null)
            {
                return BadRequest(new { message = "Failed to update user information" });
            }

            _logger.LogInformation("User information updated successfully: {UserId}", userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user information");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpPost("assign-role/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignRole(string userId, [FromBody] string roleName)
    {
        try
        {
            var result = await _authService.AssignRoleAsync(userId, roleName);
            if (!result)
            {
                return BadRequest(new { message = "Failed to assign role" });
            }

            _logger.LogInformation("Role {RoleName} assigned to user {UserId}", roleName, userId);
            return Ok(new { message = "Role assigned successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpDelete("remove-role/{userId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveRole(string userId, [FromBody] string roleName)
    {
        try
        {
            var result = await _authService.RemoveRoleAsync(userId, roleName);
            if (!result)
            {
                return BadRequest(new { message = "Failed to remove role" });
            }

            _logger.LogInformation("Role {RoleName} removed from user {UserId}", roleName, userId);
            return Ok(new { message = "Role removed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}
