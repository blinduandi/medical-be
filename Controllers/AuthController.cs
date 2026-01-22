using Microsoft.AspNetCore.Mvc;
using medical_be.DTOs;
using medical_be.Services;
using medical_be.Shared.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using medical_be.Controllers.Base;
using medical_be.Helpers;

namespace medical_be.Controllers;

[Route("api/[controller]")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly IFileService _fileService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IFileService fileService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _fileService = fileService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            var validationResult = ValidateModel();
            if (validationResult != null)
                return validationResult;

            var result = await _authService.RegisterAsync(registerDto);
            
            if (!result.Success)
            {
                return ValidationErrorResponse(result.Message, result.Errors);
            }

            _logger.LogInformation("User registered successfully: {Email}", registerDto.Email);
            return SuccessResponse(result.AuthResponse, result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return InternalServerErrorResponse("An error occurred during registration");
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            var validationResult = ValidateModel();
            if (validationResult != null)
                return validationResult;

            var result = await _authService.LoginAsync(loginDto);
            if (result == null)
            {
                return UnauthorizedResponse("Invalid credentials or account is inactive");
            }

            _logger.LogInformation("User logged in successfully: {Email}", loginDto.Email);
            return SuccessResponse(result, "Login successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user login");
            return InternalServerErrorResponse("An error occurred during login");
        }
    }

    [HttpPost("verify-mfa")]
    public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaLoginDto verifyMfaDto)
    {
        try
        {
            var validationResult = ValidateModel();
            if (validationResult != null)
                return validationResult;

            var result = await _authService.VerifyMfaLoginAsync(verifyMfaDto.Email, verifyMfaDto.Otp);
            if (result == null)
            {
                return UnauthorizedResponse("Invalid OTP or user not found");
            }

            _logger.LogInformation("User MFA verification successful: {Email}", verifyMfaDto.Email);
            return SuccessResponse(result, "MFA verification successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during MFA verification");
            return InternalServerErrorResponse("An error occurred during MFA verification");
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        try
        {
            var validationResult = ValidateModel();
            if (validationResult != null)
                return validationResult;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return UnauthorizedResponse("User not found");
            }

            var result = await _authService.ChangePasswordAsync(userId, changePasswordDto);
            if (!result)
            {
                return ErrorResponse("Failed to change password. Check your current password.");
            }

            _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
            return SuccessResponse(null, "Password changed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password change");
            return InternalServerErrorResponse("An error occurred during password change");
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

            // Create response with profile picture
            var userResponse = new
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IDNP = user.IDNP,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                Address = user.Address,
                IsActive = user.IsActive,
                Roles = user.Roles,
                ProfilePicture = profilePicture
            };

            return SuccessResponse(userResponse, "User information retrieved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user information");
            return InternalServerErrorResponse("An error occurred while retrieving user information");
        }
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateUserDto updateUserDto)
    {
        try
        {
            var validationResult = ValidateModel();
            if (validationResult != null)
                return validationResult;

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return UnauthorizedResponse("User not found");
            }

            var result = await _authService.UpdateUserAsync(userId, updateUserDto);
            if (result == null)
            {
                return ErrorResponse("Failed to update user information");
            }

            _logger.LogInformation("User information updated successfully: {UserId}", userId);
            return SuccessResponse(result, "User information updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user information");
            return InternalServerErrorResponse("An error occurred while updating user information");
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
                return ErrorResponse("Failed to assign role");
            }

            _logger.LogInformation("Role {RoleName} assigned to user {UserId}", roleName, userId);
            return SuccessResponse(null, "Role assigned successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role");
            return InternalServerErrorResponse("An error occurred while assigning role");
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
                return ErrorResponse("Failed to remove role");
            }

            _logger.LogInformation("Role {RoleName} removed from user {UserId}", roleName, userId);
            return SuccessResponse(null, "Role removed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role");
            return InternalServerErrorResponse("An error occurred while removing role");
        }
    }

    [HttpPost("get-verification-code")]
    public async Task<IActionResult> GetVerificationCode([FromBody] GetVerificationCodeDto dto)
    {
        try
        {
            var validationResult = ValidateModel();
            if (validationResult != null)
                return validationResult;

            var result = await _authService.GenerateVerificationCodeAsync(dto.Email);
            if (result)
            {
                _logger.LogInformation("Verification code sent to email: {Email}", dto.Email);
                return SuccessResponse(null, "Verification code sent to your email");
            }

            return ErrorResponse("Failed to send verification code. Please check if the email is registered.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating verification code");
            return InternalServerErrorResponse("An error occurred while generating verification code");
        }
    }

    [HttpPost("verify-code")]
    public async Task<IActionResult> VerifyCode([FromBody] VerifyCodeDto dto)
    {
        try
        {
            var validationResult = ValidateModel();
            if (validationResult != null)
                return validationResult;

            var result = await _authService.VerifyCodeAsync(dto.Email, dto.Code);
            
            if (result.IsValid)
            {
                _logger.LogInformation("Email verification successful for: {Email}", dto.Email);
                return SuccessResponse(result, "Email verification successful");
            }

            _logger.LogWarning("Email verification failed for: {Email}. Reason: {Message}", dto.Email, result.Message);
            return ErrorResponse(result.Message ?? "Email verification failed", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying code");
            return InternalServerErrorResponse("An error occurred while verifying code");
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        try
        {
            var validationResult = ValidateModel();
            if (validationResult != null)
                return validationResult;

            var result = await _authService.ForgotPasswordAsync(dto.Email);
            
            // Always return success to prevent email enumeration
            _logger.LogInformation("Password reset requested for email: {Email}", dto.Email);
            return SuccessResponse(null, "If the email exists, a password reset code has been sent.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing forgot password request");
            return InternalServerErrorResponse("An error occurred while processing your request");
        }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        try
        {
            var validationResult = ValidateModel();
            if (validationResult != null)
                return validationResult;

            var result = await _authService.ResetPasswordAsync(dto);
            
            if (result.Success)
            {
                _logger.LogInformation("Password reset successful for email: {Email}", dto.Email);
                return SuccessResponse(result, result.Message);
            }

            _logger.LogWarning("Password reset failed for email: {Email}. Reason: {Message}", dto.Email, result.Message);
            return ErrorResponse(result.Message, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password");
            return InternalServerErrorResponse("An error occurred while resetting password");
        }
    }

    [HttpPost("change-password-with-token")]
    public async Task<IActionResult> ChangePasswordWithToken([FromBody] ChangePasswordWithTokenDto dto)
    {
        try
        {
            var validationResult = ValidateModel();
            if (validationResult != null)
                return validationResult;

            var result = await _authService.ChangePasswordWithTokenAsync(dto);
            
            if (result.Success)
            {
                _logger.LogInformation("Password changed with token for email: {Email}", dto.Email);
                return SuccessResponse(result, result.Message);
            }

            _logger.LogWarning("Password change with token failed for email: {Email}. Reason: {Message}", dto.Email, result.Message);
            return ErrorResponse(result.Message, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password with token");
            return InternalServerErrorResponse("An error occurred while changing password");
        }
    }

    // Development endpoint to get validation documentation
    [HttpGet("validation-docs")]
    public IActionResult GetValidationDocumentation([FromQuery] string? model = null)
    {
        try
        {
            return model?.ToLower() switch
            {
                "register" => ApiResponse.GetValidationDocumentation<RegisterDto>(true),
                "login" => ApiResponse.GetValidationDocumentation<LoginDto>(true),
                "changepassword" => ApiResponse.GetValidationDocumentation<ChangePasswordDto>(true),
                "updateuser" => ApiResponse.GetValidationDocumentation<UpdateUserDto>(true),
                "getverificationcode" => ApiResponse.GetValidationDocumentation<GetVerificationCodeDto>(true),
                "verifycode" => ApiResponse.GetValidationDocumentation<VerifyCodeDto>(true),
                "forgotpassword" => ApiResponse.GetValidationDocumentation<ForgotPasswordDto>(true),
                "resetpassword" => ApiResponse.GetValidationDocumentation<ResetPasswordDto>(true),
                "changepasswordwithtoken" => ApiResponse.GetValidationDocumentation<ChangePasswordWithTokenDto>(true),
                _ => SuccessResponse(new
                {
                    availableModels = new[]
                    {
                        "register", "login", "changepassword", "updateuser", 
                        "getverificationcode", "verifycode", "forgotpassword", "resetpassword",
                        "changepasswordwithtoken"
                    }
                }, "Available validation documentation models")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting validation documentation");
            return InternalServerErrorResponse("An error occurred while retrieving validation documentation");
        }
    }
}
