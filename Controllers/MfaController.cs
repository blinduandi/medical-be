using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using medical_be.Data;
using medical_be.Models;
using medical_be.Services;
using medical_be.Shared.Interfaces;
using medical_be.Extensions;
using medical_be.Controllers.Base;
using System.ComponentModel.DataAnnotations;

namespace medical_be.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class MfaController : BaseApiController
    {
        private readonly UserManager<User> _userManager;
        private readonly IOtpService _otpService;
        private readonly IAuditService _auditService;
        private readonly ILogger<MfaController> _logger;
        private readonly IAuthService _authService;

        public MfaController(
            UserManager<User> userManager,
            IOtpService otpService,
            IAuditService auditService,
            ILogger<MfaController> logger,
            IAuthService authService)
        {
            _userManager = userManager;
            _otpService = otpService;
            _auditService = auditService;
            _logger = logger;
            _authService = authService;
        }

        /// <summary>
        /// Enable MFA for current user
        /// </summary>
        [HttpPost("enable")]
        public async Task<IActionResult> EnableMfa([FromBody] EnableMfaRequest request)
        {
            try
            {
                var validationResult = ValidateModel();
                if (validationResult != null)
                    return validationResult;

                var userId = User.GetUserId();
                var user = await _userManager.FindByIdAsync(userId);
                
                if (user == null)
                {
                    return NotFoundResponse("User not found");
                }

                if (user.IsMFAEnabled)
                {
                    return ErrorResponse("MFA is already enabled");
                }

                // Update phone number if provided
                if (!string.IsNullOrEmpty(request.PhoneNumber))
                {
                    user.PhoneNumber = request.PhoneNumber;
                    await _userManager.UpdateAsync(user);
                }

                // Send OTP to verify phone number
                if (string.IsNullOrEmpty(user.PhoneNumber))
                {
                    return ErrorResponse("Phone number is required for MFA");
                }

                var otpSent = await _otpService.SendOtpAsync(userId, user.PhoneNumber);
                if (!otpSent)
                {
                    return InternalServerErrorResponse( "Failed to send OTP");
                }

                // Audit log
                await _auditService.LogAuditAsync(userId, "MfaEnableAttempt", "Attempted to enable MFA", "User", null, Request.GetClientIpAddress());

                return SuccessResponse(null, "OTP sent to your phone number. Please verify to enable MFA.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling MFA for user: {UserId}", User.GetUserId());
                return InternalServerErrorResponse( "Internal server error");
            }
        }

        /// <summary>
        /// Verify OTP and complete MFA setup
        /// </summary>
        [HttpPost("verify-setup")]
        public async Task<IActionResult> VerifyMfaSetup([FromBody] VerifyOtpRequest request)
        {
            try
            {
                var userId = User.GetUserId();
                var user = await _userManager.FindByIdAsync(userId);
                
                if (user == null)
                {
                    return NotFoundResponse("User not found");
                }

                var isValidOtp = await _otpService.ValidateOtpAsync(userId, request.Otp);
                if (!isValidOtp)
                {
                    return ErrorResponse("Invalid or expired OTP");
                }

                // Enable MFA
                user.IsMFAEnabled = true;
                await _userManager.UpdateAsync(user);

                // Audit log
                await _auditService.LogAuditAsync(userId, "MfaEnabled", "Successfully enabled MFA", "User", null, Request.GetClientIpAddress());

                return SuccessResponse(null, "MFA enabled successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying MFA setup for user: {UserId}", User.GetUserId());
                return InternalServerErrorResponse( "Internal server error");
            }
        }

        /// <summary>
        /// Disable MFA for current user
        /// </summary>
        [HttpPost("disable")]
        public async Task<IActionResult> DisableMfa([FromBody] DisableMfaRequest request)
        {
            try
            {
                var userId = User.GetUserId();
                var user = await _userManager.FindByIdAsync(userId);
                
                if (user == null)
                {
                    return NotFoundResponse("User not found");
                }

                if (!user.IsMFAEnabled)
                {
                    return ErrorResponse("MFA is not enabled");
                }

                // Verify password before disabling MFA
                var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
                if (!passwordValid)
                {
                    return ErrorResponse("Invalid password");
                }

                // Send OTP for additional verification
                var otpSent = await _otpService.SendOtpAsync(userId, user.PhoneNumber!);
                if (!otpSent)
                {
                    return InternalServerErrorResponse( "Failed to send OTP");
                }

                return SuccessResponse(null, "OTP sent to your phone number. Please verify to disable MFA.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling MFA for user: {UserId}", User.GetUserId());
                return InternalServerErrorResponse( "Internal server error");
            }
        }

        /// <summary>
        /// Verify OTP and complete MFA disable
        /// </summary>
        [HttpPost("verify-disable")]
        public async Task<IActionResult> VerifyMfaDisable([FromBody] VerifyOtpRequest request)
        {
            try
            {
                var userId = User.GetUserId();
                var user = await _userManager.FindByIdAsync(userId);
                
                if (user == null)
                {
                    return NotFoundResponse("User not found");
                }

                var isValidOtp = await _otpService.ValidateOtpAsync(userId, request.Otp);
                if (!isValidOtp)
                {
                    return ErrorResponse("Invalid or expired OTP");
                }

                // Disable MFA
                user.IsMFAEnabled = false;
                await _userManager.UpdateAsync(user);

                // Audit log
                await _auditService.LogAuditAsync(userId, "MfaDisabled", "Successfully disabled MFA", "User", null, Request.GetClientIpAddress());

                return SuccessResponse(null, "MFA disabled successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying MFA disable for user: {UserId}", User.GetUserId());
                return InternalServerErrorResponse( "Internal server error");
            }
        }

        /// <summary>
        /// Send OTP for login verification
        /// </summary>
        [HttpPost("send-login-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> SendLoginOtp([FromBody] SendLoginOtpRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null || !user.IsMFAEnabled)
                {
                    // Don't reveal if user exists or MFA status
                    return SuccessResponse(null, "If MFA is enabled for this account, an OTP has been sent.");
                }

                var otpSent = await _otpService.SendOtpAsync(user.Id, user.PhoneNumber!);
                if (!otpSent)
                {
                    return InternalServerErrorResponse( "Failed to send OTP");
                }

                return SuccessResponse(null, "OTP sent to your registered phone number.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending login OTP for email: {Email}", request.Email);
                return InternalServerErrorResponse( "Internal server error");
            }
        }

        /// <summary>
        /// Verify login OTP and get authentication token
        /// </summary>
        [HttpPost("verify-login-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyLoginOtp([FromBody] VerifyLoginOtpRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null || !user.IsActive)
                {
                    return UnauthorizedResponse("Invalid credentials");
                }

                // Validate OTP
                var isValidOtp = await _otpService.ValidateOtpAsync(user.Id, request.Otp);
                if (!isValidOtp)
                {
                    _logger.LogWarning("Invalid OTP for user: {Email}", request.Email);
                    return UnauthorizedResponse("Invalid or expired OTP");
                }

                // OTP is valid - get auth response (which checks for password change requirement)
                var result = await _authService.VerifyMfaLoginAsync(request.Email, request.Otp);
                if (result == null)
                {
                    return UnauthorizedResponse("Authentication failed");
                }

                // Audit log
                await _auditService.LogAuditAsync(user.Id, "MfaLoginSuccess", "Successfully verified MFA for login", "User", null, Request.GetClientIpAddress());

                _logger.LogInformation("User {Email} MFA login verified successfully", request.Email);
                return SuccessResponse(result, "MFA verification successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying login OTP for email: {Email}", request.Email);
                return InternalServerErrorResponse("Internal server error");
            }
        }

        /// <summary>
        /// Get MFA status for current user
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetMfaStatus()
        {
            try
            {
                var userId = User.GetUserId();
                var user = await _userManager.FindByIdAsync(userId);
                
                if (user == null)
                {
                    return NotFoundResponse("User not found");
                }

                return SuccessResponse(new
                {
                    MfaEnabled = user.IsMFAEnabled,
                    PhoneNumber = string.IsNullOrEmpty(user.PhoneNumber) ? null : MaskPhoneNumber(user.PhoneNumber),
                    HasPhoneNumber = !string.IsNullOrEmpty(user.PhoneNumber)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting MFA status for user: {UserId}", User.GetUserId());
                return InternalServerErrorResponse( "Internal server error");
            }
        }

        private static string MaskPhoneNumber(string phoneNumber)
        {
            if (phoneNumber.Length < 4) return phoneNumber;
            return phoneNumber[..3] + new string('*', phoneNumber.Length - 6) + phoneNumber[^3..];
        }
    }

    // Request DTOs
    public class EnableMfaRequest
    {
        public string? PhoneNumber { get; set; }
    }

    public class VerifyOtpRequest
    {
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Otp { get; set; } = string.Empty;
    }

    public class DisableMfaRequest
    {
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class SendLoginOtpRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyLoginOtpRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Otp { get; set; } = string.Empty;
    }
}
