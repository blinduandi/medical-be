using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using medical_be.Models;
using medical_be.Shared.Interfaces;
using medical_be.Data;
using medical_be.Services;

namespace medical_be.Services;

public class AuthService : IAuthService
{
	private readonly UserManager<User> _userManager;
	private readonly SignInManager<User> _signInManager;
	private readonly IJwtService _jwtService;
	private readonly ILogger<AuthService> _logger;
	private readonly ApplicationDbContext _context;
	private readonly INotificationService _notificationService;
	private readonly EmailTemplateService _emailTemplateService;

	public AuthService(
		UserManager<User> userManager,
		SignInManager<User> signInManager,
		IJwtService jwtService,
		ILogger<AuthService> logger,
		ApplicationDbContext context,
		INotificationService notificationService,
		EmailTemplateService emailTemplateService)
	{
		_userManager = userManager;
		_signInManager = signInManager;
		_jwtService = jwtService;
		_logger = logger;
		_context = context;
		_notificationService = notificationService;
		_emailTemplateService = emailTemplateService;
	}

	// Legacy/shared interface methods
	public async Task<medical_be.Shared.DTOs.AuthResponseDto> AuthenticateAsync(medical_be.Shared.DTOs.LoginDto loginDto)
	{
		// Bridge to new LoginAsync
		var result = await LoginAsync(new medical_be.DTOs.LoginDto { Email = loginDto.Email, Password = loginDto.Password });
		if (result == null || result.RequiresPasswordChange) 
			throw new UnauthorizedAccessException("Invalid credentials or password change required");
		return new medical_be.Shared.DTOs.AuthResponseDto
		{
			Token = result.Token ?? string.Empty,
			ExpiresAt = result.Expiration ?? DateTime.UtcNow,
			RefreshToken = string.Empty,
			User = new medical_be.Shared.DTOs.UserDto
			{
				Id = Guid.TryParse(result.User.Id, out var gid) ? gid : Guid.Empty,
				Email = result.User.Email,
				FirstName = result.User.FirstName,
				LastName = result.User.LastName,
				PhoneNumber = result.User.PhoneNumber ?? string.Empty,
				DateOfBirth = result.User.DateOfBirth,
				Gender = result.User.Gender.ToString(),
				Address = result.User.Address ?? string.Empty,
				IsActive = result.User.IsActive,
				CreatedAt = DateTime.UtcNow,
				Roles = result.User.Roles
			}
		};
	}

	public async Task<medical_be.Shared.DTOs.UserDto> CreateUserAsync(medical_be.Shared.DTOs.CreateUserDto createUserDto)
	{
		var user = new User
		{
			UserName = createUserDto.Email,
			Email = createUserDto.Email,
			FirstName = createUserDto.FirstName,
			LastName = createUserDto.LastName,
			PhoneNumber = createUserDto.PhoneNumber,
			DateOfBirth = createUserDto.DateOfBirth,
			Gender = Enum.TryParse<Gender>(createUserDto.Gender, out var g) ? g : Gender.Other,
			Address = createUserDto.Address,
			EmailConfirmed = false,
			IsActive = true
		};

		var result = await _userManager.CreateAsync(user, createUserDto.Password);
		if (!result.Succeeded)
		{
			throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
		}

		if (createUserDto.Roles?.Any() == true)
		{
			await _userManager.AddToRolesAsync(user, createUserDto.Roles);
		}

		return await MapToUserDtoAsync(user);
	}

	public async Task<medical_be.Shared.DTOs.UserDto> GetUserByIdAsync(Guid userId)
	{
		var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId.ToString());
		if (user == null) throw new KeyNotFoundException("User not found");
		return await MapToUserDtoAsync(user);
	}

	public async Task<medical_be.Shared.DTOs.UserDto> UpdateUserAsync(Guid userId, medical_be.Shared.DTOs.UpdateUserDto updateUserDto)
	{
		var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId.ToString());
		if (user == null) throw new KeyNotFoundException("User not found");

		user.FirstName = updateUserDto.FirstName;
		user.LastName = updateUserDto.LastName;
		user.PhoneNumber = updateUserDto.PhoneNumber;
		user.DateOfBirth = updateUserDto.DateOfBirth;
		user.Gender = Enum.TryParse<Gender>(updateUserDto.Gender, out var g) ? g : user.Gender;
		user.Address = updateUserDto.Address;
		user.UpdatedAt = DateTime.UtcNow;

		var result = await _userManager.UpdateAsync(user);
		if (!result.Succeeded)
		{
			throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
		}

		return await MapToUserDtoAsync(user);
	}

	public Task<bool> ValidateTokenAsync(string token)
	{
		var principal = _jwtService.ValidateToken(token);
		return Task.FromResult(principal != null);
	}

	public async Task<bool> DeleteUserAsync(Guid userId)
	{
		var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId.ToString());
		if (user == null) return false;
		var res = await _userManager.DeleteAsync(user);
		return res.Succeeded;
	}

	public async Task<List<medical_be.Shared.DTOs.UserDto>> GetUsersAsync()
	{
		var users = await _userManager.Users.ToListAsync();
		var list = new List<medical_be.Shared.DTOs.UserDto>();
		foreach (var u in users)
		{
			list.Add(await MapToUserDtoAsync(u));
		}
		return list;
	}

	public async Task<List<medical_be.Shared.DTOs.UserDto>> GetUsersByRoleAsync(string role)
	{
		var users = await _userManager.GetUsersInRoleAsync(role);
		var list = new List<medical_be.Shared.DTOs.UserDto>();
		foreach (var u in users)
		{
			list.Add(await MapToUserDtoAsync(u));
		}
		return list;
	}

	// Newer methods used by current controllers
	public async Task<medical_be.DTOs.RegistrationResultDto> RegisterAsync(medical_be.DTOs.RegisterDto registerDto)
	{
		var user = new User
		{
			UserName = registerDto.Email,
			Email = registerDto.Email,
			IDNP = registerDto.IDNP,
			FirstName = registerDto.FirstName,
			LastName = registerDto.LastName,
			PhoneNumber = registerDto.PhoneNumber,
			DateOfBirth = registerDto.DateOfBirth,
			Gender = registerDto.Gender,
			Address = registerDto.Address,
			EmailConfirmed = true,
			IsActive = true
		};

		var result = await _userManager.CreateAsync(user, registerDto.Password);
		_logger.LogInformation($"Register result: {System.Text.Json.JsonSerializer.Serialize(result)} ");
		if (!result.Succeeded)
		{
			_logger.LogWarning("User registration failed for {Email}. Errors: {Errors}",
				registerDto.Email, string.Join("; ", result.Errors.Select(e => e.Description)));
			
			return new medical_be.DTOs.RegistrationResultDto
			{
				Success = false,
				Message = "Registration failed",
				Errors = result.Errors.Select(e => e.Description).ToList()
			};
		}

	// 	// ---- CREATE DYNAMIC SINGLE NOTIFICATION ----
    // var notification = new SingleNotification
    // {
    //     Title = "Welcome to MedTrack!",
    //     Body = $@"
    //                 <h2>Welcome to Medical System!</h2>
    //                 <p>Dear {user.FirstName} {user.LastName},</p>
    //                 <p>Thank you for registering with our medical system. Your account has been created successfully.</p>
    //                 <p><strong>Your Details:</strong></p>
    //                 <ul>
    //                     <li>Email: {user.Email}</li>
    //                     <li>Registration Date: {user.CreatedAt:MMMM dd, yyyy}</li>
    //                 </ul>
    //                 <p>You can now:</p>
    //                 <ul>
    //                     <li>Schedule appointments with doctors</li>
    //                     <li>View your medical records</li>
    //                     <li>Manage your profile</li>
    //                 </ul>
    //                 <p>If you have any questions, please contact our support team.</p>
    //                 <p>Best regards,<br>Medical System Team</p>
    //             ",
    //     ToEmail = user.Email,
    //     Status = "waiting_for_sending",
    //     CreatedAt = DateTime.UtcNow,
    //     UpdatedAt = DateTime.UtcNow
    // };

    // _context.Notifications.Add(notification);
    // await _context.SaveChangesAsync();
    // // -------------------------------------------

		// Assign role based on UserRole parameter
		var roleName = registerDto.UserRole switch
		{
			medical_be.DTOs.UserRegistrationType.Doctor => "Doctor",
			medical_be.DTOs.UserRegistrationType.Admin => "Admin",
			medical_be.DTOs.UserRegistrationType.Patient => "Patient",
			_ => "Patient" // Default fallback
		};
		
		var roleResult = await _userManager.AddToRoleAsync(user, roleName);
		if (!roleResult.Succeeded)
		{
			_logger.LogWarning("Failed to assign role {Role} to user {Email}. Errors: {Errors}", 
				roleName, registerDto.Email, string.Join("; ", roleResult.Errors.Select(e => e.Description)));
		}

		var roles = await _userManager.GetRolesAsync(user);
		var token = _jwtService.GenerateToken(user, roles);

		return new medical_be.DTOs.RegistrationResultDto
		{
			Success = true,
			Message = "Registration successful",
			AuthResponse = new medical_be.DTOs.AuthResponseDto
			{
				Token = token,
				Expiration = DateTime.UtcNow.AddMinutes(60),
				User = new medical_be.DTOs.UserDto
				{
					Id = user.Id,
					Email = user.Email!,
					FirstName = user.FirstName,
					LastName = user.LastName,
					PhoneNumber = user.PhoneNumber,
					DateOfBirth = user.DateOfBirth,
					Gender = user.Gender,
					Address = user.Address,
					IsActive = user.IsActive,
					Roles = roles.ToList()
				}
			}
		};
	}

	public async Task<medical_be.DTOs.AuthResponseDto?> LoginAsync(medical_be.DTOs.LoginDto loginDto)
	{
		var user = await _userManager.FindByEmailAsync(loginDto.Email);
		if (user == null || !user.IsActive) return null;

		// Check if temporary password has expired
		if (user.MustChangePassword && user.TemporaryPasswordExpires.HasValue && user.TemporaryPasswordExpires < DateTime.UtcNow)
		{
			_logger.LogWarning("Temporary password has expired for user: {Email}", loginDto.Email);
			return null;
		}

		var check = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);
		if (!check.Succeeded) return null;

		var roles = await _userManager.GetRolesAsync(user);

		// Always require MFA - no token issued until MFA is verified
		_logger.LogInformation("User {Email} logged in, MFA verification required", loginDto.Email);

		return new medical_be.DTOs.AuthResponseDto
		{
			Token = null,
			Expiration = null,
			User = new medical_be.DTOs.UserDto
			{
				Id = user.Id,
				Email = user.Email!,
				FirstName = user.FirstName,
				LastName = user.LastName,
				PhoneNumber = user.PhoneNumber,
				DateOfBirth = user.DateOfBirth,
				Gender = user.Gender,
				Address = user.Address,
				IsActive = user.IsActive,
				Roles = roles.ToList()
			},
			RequiresMfa = true,
			RequiresPasswordChange = false,
			PasswordChangeToken = null
		};
	}

	public async Task<medical_be.DTOs.AuthResponseDto?> VerifyMfaLoginAsync(string email, string otp)
	{
		var user = await _userManager.FindByEmailAsync(email);
		if (user == null || !user.IsActive) return null;

		// Validate OTP using the OTP service (injected via constructor or resolve from context)
		// For now, we trust the controller has already validated the OTP
		
		var roles = await _userManager.GetRolesAsync(user);

		// After MFA verification, check if password change is required
		if (user.MustChangePassword)
		{
			var passwordChangeToken = GeneratePasswordChangeToken();
			user.VerificationCode = passwordChangeToken;
			user.VerificationCodeExpires = DateTime.UtcNow.AddMinutes(15);
			await _userManager.UpdateAsync(user);

			_logger.LogInformation("User {Email} MFA verified, but requires password change", email);

			return new medical_be.DTOs.AuthResponseDto
			{
				Token = null,
				Expiration = null,
				User = new medical_be.DTOs.UserDto
				{
					Id = user.Id,
					Email = user.Email!,
					FirstName = user.FirstName,
					LastName = user.LastName,
					PhoneNumber = user.PhoneNumber,
					DateOfBirth = user.DateOfBirth,
					Gender = user.Gender,
					Address = user.Address,
					IsActive = user.IsActive,
					Roles = roles.ToList()
				},
				RequiresMfa = false,
				RequiresPasswordChange = true,
				PasswordChangeToken = passwordChangeToken
			};
		}

		// MFA verified and no password change required - issue JWT token
		var token = _jwtService.GenerateToken(user, roles);
		_logger.LogInformation("User {Email} MFA verified, JWT token issued", email);

		return new medical_be.DTOs.AuthResponseDto
		{
			Token = token,
			Expiration = DateTime.UtcNow.AddMinutes(60),
			User = new medical_be.DTOs.UserDto
			{
				Id = user.Id,
				Email = user.Email!,
				FirstName = user.FirstName,
				LastName = user.LastName,
				PhoneNumber = user.PhoneNumber,
				DateOfBirth = user.DateOfBirth,
				Gender = user.Gender,
				Address = user.Address,
				IsActive = user.IsActive,
				Roles = roles.ToList()
			},
			RequiresMfa = false,
			RequiresPasswordChange = false
		};
	}

	public async Task<bool> ChangePasswordAsync(string userId, medical_be.DTOs.ChangePasswordDto dto)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user == null) return false;
		var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
		
		if (result.Succeeded && user.MustChangePassword)
		{
			// Clear the mandatory password change flag after successful change
			user.MustChangePassword = false;
			user.TemporaryPasswordExpires = null;
			await _userManager.UpdateAsync(user);
			_logger.LogInformation("User {UserId} completed mandatory password change", userId);
		}
		
		return result.Succeeded;
	}

	public async Task<medical_be.DTOs.UserDto?> GetUserByIdAsync(string userId)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user == null) return null;
		var roles = await _userManager.GetRolesAsync(user);
		return new medical_be.DTOs.UserDto
		{
			Id = user.Id,
			Email = user.Email!,
			FirstName = user.FirstName,
			LastName = user.LastName,
			PhoneNumber = user.PhoneNumber,
			DateOfBirth = user.DateOfBirth,
			Gender = user.Gender,
			Address = user.Address,
			IsActive = user.IsActive,
			Roles = roles.ToList()
		};
	}

	public async Task<medical_be.DTOs.UserDto?> UpdateUserAsync(string userId, medical_be.DTOs.UpdateUserDto dto)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user == null) return null;
		user.FirstName = dto.FirstName;
		user.LastName = dto.LastName;
		user.PhoneNumber = dto.PhoneNumber;
		user.DateOfBirth = dto.DateOfBirth;
		user.Gender = dto.Gender;
		user.Address = dto.Address;
		user.UpdatedAt = DateTime.UtcNow;
		var res = await _userManager.UpdateAsync(user);
		if (!res.Succeeded) return null;
		var roles = await _userManager.GetRolesAsync(user);
		return new medical_be.DTOs.UserDto
		{
			Id = user.Id,
			Email = user.Email!,
			FirstName = user.FirstName,
			LastName = user.LastName,
			PhoneNumber = user.PhoneNumber,
			DateOfBirth = user.DateOfBirth,
			Gender = user.Gender,
			Address = user.Address,
			IsActive = user.IsActive,
			Roles = roles.ToList()
		};
	}

	public async Task<bool> AssignRoleAsync(string userId, string roleName)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user == null) return false;
		var res = await _userManager.AddToRoleAsync(user, roleName);
		return res.Succeeded;
	}

	public async Task<bool> RemoveRoleAsync(string userId, string roleName)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user == null) return false;
		var res = await _userManager.RemoveFromRoleAsync(user, roleName);
		return res.Succeeded;
	}

	private string GeneratePasswordChangeToken()
	{
		// Generate a secure 32-character alphanumeric token
		const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
		var random = new Random();
		return new string(Enumerable.Repeat(chars, 32).Select(s => s[random.Next(s.Length)]).ToArray());
	}

	public async Task<medical_be.DTOs.ResetPasswordResponseDto> ChangePasswordWithTokenAsync(medical_be.DTOs.ChangePasswordWithTokenDto dto)
	{
		var user = await _userManager.FindByEmailAsync(dto.Email);
		if (user == null)
		{
			_logger.LogWarning("Password change with token attempted for non-existent email: {Email}", dto.Email);
			return new medical_be.DTOs.ResetPasswordResponseDto
			{
				Success = false,
				Message = "Invalid request"
			};
		}

		// Check if the token exists and matches
		if (string.IsNullOrEmpty(user.VerificationCode) || user.VerificationCode != dto.PasswordChangeToken)
		{
			_logger.LogWarning("Invalid password change token for user: {Email}", dto.Email);
			return new medical_be.DTOs.ResetPasswordResponseDto
			{
				Success = false,
				Message = "Invalid or expired token"
			};
		}

		// Check if token has expired
		if (user.VerificationCodeExpires == null || user.VerificationCodeExpires < DateTime.UtcNow)
		{
			_logger.LogWarning("Password change token expired for user: {Email}", dto.Email);
			return new medical_be.DTOs.ResetPasswordResponseDto
			{
				Success = false,
				Message = "Token has expired. Please login again to get a new token."
			};
		}

		// Reset the password
		var token = await _userManager.GeneratePasswordResetTokenAsync(user);
		var resetResult = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

		if (!resetResult.Succeeded)
		{
			_logger.LogError("Failed to change password for user: {Email}. Errors: {Errors}", 
				dto.Email, string.Join("; ", resetResult.Errors.Select(e => e.Description)));
			return new medical_be.DTOs.ResetPasswordResponseDto
			{
				Success = false,
				Message = string.Join("; ", resetResult.Errors.Select(e => e.Description))
			};
		}

		// Clear the token and MustChangePassword flag
		user.VerificationCode = null;
		user.VerificationCodeExpires = null;
		user.MustChangePassword = false;
		user.TemporaryPasswordExpires = null;
		await _userManager.UpdateAsync(user);

		_logger.LogInformation("Password changed successfully with token for user: {Email}", dto.Email);

		return new medical_be.DTOs.ResetPasswordResponseDto
		{
			Success = true,
			Message = "Password changed successfully. Please login with your new password."
		};
	}

	private async Task<medical_be.Shared.DTOs.UserDto> MapToUserDtoAsync(User user)
	{
		var roles = await _userManager.GetRolesAsync(user);
		return new medical_be.Shared.DTOs.UserDto
		{
			Id = Guid.TryParse(user.Id, out var gid) ? gid : Guid.Empty,
			Email = user.Email ?? string.Empty,
			FirstName = user.FirstName,
			LastName = user.LastName,
			PhoneNumber = user.PhoneNumber ?? string.Empty,
			DateOfBirth = user.DateOfBirth,
			Gender = user.Gender.ToString(),
			Address = user.Address ?? string.Empty,
			IsActive = user.IsActive,
			CreatedAt = user.CreatedAt,
			Roles = roles.ToList()
		};
	}

	// Email verification methods
	public async Task<bool> GenerateVerificationCodeAsync(string email)
	{
		var user = await _userManager.FindByEmailAsync(email);
		if (user == null)
		{
			_logger.LogWarning("Attempted to generate verification code for non-existent email: {Email}", email);
			return false;
		}

		// Clear any existing verification code to invalidate old codes
		if (!string.IsNullOrEmpty(user.VerificationCode))
		{
			_logger.LogInformation("Invalidating previous verification code for user: {Email}", email);
		}

		// Generate a new 6-digit verification code
		var code = new Random().Next(100000, 999999).ToString();
		
		// Set new code and expiration (overwrites old code, invalidating it)
		user.VerificationCode = code;
		user.VerificationCodeExpires = DateTime.UtcNow.AddMinutes(15);

		var result = await _userManager.UpdateAsync(user);
		if (!result.Succeeded)
			{
				_logger.LogError("Failed to update user with verification code for email: {Email}. Errors: {Errors}", 
					email, string.Join("; ", result.Errors.Select(e => e.Description)));
				return false;
			}

			_logger.LogInformation("Verification code generated for user: {Email}", email);

			// Prepare placeholders for template
			var placeholders = new Dictionary<string, string>
			{
				{ "FirstName", user.FirstName },
				{ "LastName", user.LastName },
				{ "VerificationCode", code }
			};

			// Load the template
			var body = await _emailTemplateService.GetTemplateAsync("VerificationEmail.html", placeholders);

			var subject = "Your Verification Code";

			// Send email
			await _notificationService.SendEmailAsync(user.Email!, subject, body);

			return true;
	}

	public async Task<medical_be.DTOs.VerificationResponseDto> VerifyCodeAsync(string email, string code)
	{
		var user = await _userManager.FindByEmailAsync(email);
		if (user == null)
		{
			_logger.LogWarning("Attempted to verify code for non-existent email: {Email}", email);
			return new medical_be.DTOs.VerificationResponseDto
			{
				IsValid = false,
				Message = "User not found"
			};
		}

		// Check if verification code exists
		if (string.IsNullOrEmpty(user.VerificationCode))
		{
			_logger.LogWarning("No verification code found for user: {Email}", email);
			return new medical_be.DTOs.VerificationResponseDto
			{
				IsValid = false,
				Message = "No verification code found. Please request a new code."
			};
		}

		// Check if code has expired
		if (user.VerificationCodeExpires == null || user.VerificationCodeExpires < DateTime.UtcNow)
		{
			_logger.LogWarning("Verification code expired for user: {Email}", email);
			return new medical_be.DTOs.VerificationResponseDto
			{
				IsValid = false,
				Message = "Verification code has expired. Please request a new code."
			};
		}

		// Check if code matches
		if (user.VerificationCode != code)
		{
			_logger.LogWarning("Invalid verification code provided for user: {Email}. Code may have been replaced by a newer request.", email);
			return new medical_be.DTOs.VerificationResponseDto
			{
				IsValid = false,
				Message = "Invalid verification code"
			};
		}

		// Code is valid - mark email as verified and clear verification code
		user.IsEmailVerified = true;
		user.EmailConfirmed = true;
		user.VerificationCode = null;
		user.VerificationCodeExpires = null;

		var result = await _userManager.UpdateAsync(user);
		var placeholders = new Dictionary<string, string>
		{
			{ "FirstName", user.FirstName },
			{ "LastName", user.LastName }
		};

		var body = await _emailTemplateService.GetTemplateAsync("WelcomeEmail.html", placeholders);
		await _notificationService.SendEmailAsync(user.Email!, "Welcome to Medical System!", body);

		return new medical_be.DTOs.VerificationResponseDto
		{
			IsValid = true,
			Message = "Email verified successfully"
		};

	}

	// Password reset methods
	public async Task<bool> ForgotPasswordAsync(string email)
	{
		var user = await _userManager.FindByEmailAsync(email);
		if (user == null)
		{
			// Return true to prevent email enumeration attacks
			_logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
			return true;
		}

		// Generate a 6-digit reset code
		var resetCode = new Random().Next(100000, 999999).ToString();
		
		// Store the code and set 15 minute expiration
		user.VerificationCode = resetCode;
		user.VerificationCodeExpires = DateTime.UtcNow.AddMinutes(15);

		var result = await _userManager.UpdateAsync(user);
		if (!result.Succeeded)
		{
			_logger.LogError("Failed to save password reset code for email: {Email}. Errors: {Errors}", 
				email, string.Join("; ", result.Errors.Select(e => e.Description)));
			return false;
		}

		_logger.LogInformation("Password reset code generated for user: {Email}", email);

		// Send password reset email
		await _notificationService.SendPasswordResetAsync(email, resetCode);

		return true;
	}

	public async Task<medical_be.DTOs.ResetPasswordResponseDto> ResetPasswordAsync(medical_be.DTOs.ResetPasswordDto dto)
	{
		var user = await _userManager.FindByEmailAsync(dto.Email);
		if (user == null)
		{
			_logger.LogWarning("Password reset attempted for non-existent email: {Email}", dto.Email);
			return new medical_be.DTOs.ResetPasswordResponseDto
			{
				Success = false,
				Message = "Invalid request"
			};
		}

		// Check if reset code exists
		if (string.IsNullOrEmpty(user.VerificationCode))
		{
			_logger.LogWarning("No reset code found for user: {Email}", dto.Email);
			return new medical_be.DTOs.ResetPasswordResponseDto
			{
				Success = false,
				Message = "No password reset request found. Please request a new code."
			};
		}

		// Check if code has expired
		if (user.VerificationCodeExpires == null || user.VerificationCodeExpires < DateTime.UtcNow)
		{
			_logger.LogWarning("Password reset code expired for user: {Email}", dto.Email);
			return new medical_be.DTOs.ResetPasswordResponseDto
			{
				Success = false,
				Message = "Reset code has expired. Please request a new code."
			};
		}

		// Check if code matches
		if (user.VerificationCode != dto.ResetCode)
		{
			_logger.LogWarning("Invalid reset code provided for user: {Email}", dto.Email);
			return new medical_be.DTOs.ResetPasswordResponseDto
			{
				Success = false,
				Message = "Invalid reset code"
			};
		}

		// Reset the password using Identity's token-based approach
		var token = await _userManager.GeneratePasswordResetTokenAsync(user);
		var resetResult = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

		if (!resetResult.Succeeded)
		{
			_logger.LogError("Failed to reset password for user: {Email}. Errors: {Errors}", 
				dto.Email, string.Join("; ", resetResult.Errors.Select(e => e.Description)));
			return new medical_be.DTOs.ResetPasswordResponseDto
			{
				Success = false,
				Message = string.Join("; ", resetResult.Errors.Select(e => e.Description))
			};
		}

		// Clear the verification code after successful reset
		user.VerificationCode = null;
		user.VerificationCodeExpires = null;
		await _userManager.UpdateAsync(user);

		_logger.LogInformation("Password reset successful for user: {Email}", dto.Email);

		return new medical_be.DTOs.ResetPasswordResponseDto
		{
			Success = true,
			Message = "Password has been reset successfully"
		};
	}

	public async Task<medical_be.DTOs.DoctorCreationResultDto> CreateDoctorWithTemporaryPasswordAsync(medical_be.DTOs.RegisterDto registerDto)
	{
		// Generate a secure temporary password
		var temporaryPassword = GenerateSecureTemporaryPassword();
		var passwordExpiresAt = DateTime.UtcNow.AddMinutes(30);

		var user = new User
		{
			UserName = registerDto.Email,
			Email = registerDto.Email,
			IDNP = registerDto.IDNP,
			FirstName = registerDto.FirstName,
			LastName = registerDto.LastName,
			PhoneNumber = registerDto.PhoneNumber,
			DateOfBirth = registerDto.DateOfBirth,
			Gender = registerDto.Gender,
			Address = registerDto.Address,
			EmailConfirmed = true,
			IsActive = true,
			MustChangePassword = true,
			TemporaryPasswordExpires = passwordExpiresAt
		};

		var result = await _userManager.CreateAsync(user, temporaryPassword);
		if (!result.Succeeded)
		{
			_logger.LogWarning("Doctor registration failed for {Email}. Errors: {Errors}",
				registerDto.Email, string.Join("; ", result.Errors.Select(e => e.Description)));

			return new medical_be.DTOs.DoctorCreationResultDto
			{
				Success = false,
				Message = "Doctor registration failed",
				Errors = result.Errors.Select(e => e.Description).ToList()
			};
		}

		// Assign Doctor role
		var roleResult = await _userManager.AddToRoleAsync(user, "Doctor");
		if (!roleResult.Succeeded)
		{
			_logger.LogWarning("Failed to assign Doctor role to user {Email}. Errors: {Errors}",
				registerDto.Email, string.Join("; ", roleResult.Errors.Select(e => e.Description)));
		}

		var roles = await _userManager.GetRolesAsync(user);
		var token = _jwtService.GenerateToken(user, roles);

		// Send temporary password via email
		await SendTemporaryPasswordEmailAsync(user, temporaryPassword, passwordExpiresAt);

		_logger.LogInformation("Doctor created with temporary password: {Email}, expires at {ExpiresAt}", 
			registerDto.Email, passwordExpiresAt);

		return new medical_be.DTOs.DoctorCreationResultDto
		{
			Success = true,
			Message = "Doctor created successfully. Temporary password sent to email.",
			TemporaryPassword = temporaryPassword,
			PasswordExpiresAt = passwordExpiresAt,
			AuthResponse = new medical_be.DTOs.AuthResponseDto
			{
				Token = token,
				Expiration = DateTime.UtcNow.AddMinutes(60),
				User = new medical_be.DTOs.UserDto
				{
					Id = user.Id,
					Email = user.Email!,
					FirstName = user.FirstName,
					LastName = user.LastName,
					PhoneNumber = user.PhoneNumber,
					DateOfBirth = user.DateOfBirth,
					Gender = user.Gender,
					Address = user.Address,
					IsActive = user.IsActive,
					Roles = roles.ToList()
				},
				RequiresPasswordChange = true
			}
		};
	}

	private string GenerateSecureTemporaryPassword()
	{
		// Generate a secure random password: 12 chars with uppercase, lowercase, digit, and special char
		const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		const string lowercase = "abcdefghijklmnopqrstuvwxyz";
		const string digits = "0123456789";
		const string special = "!@#$%^&*";
		const string allChars = uppercase + lowercase + digits + special;

		var random = new Random();
		var password = new char[12];

		// Ensure at least one of each required character type
		password[0] = uppercase[random.Next(uppercase.Length)];
		password[1] = lowercase[random.Next(lowercase.Length)];
		password[2] = digits[random.Next(digits.Length)];
		password[3] = special[random.Next(special.Length)];

		// Fill the rest randomly
		for (int i = 4; i < password.Length; i++)
		{
			password[i] = allChars[random.Next(allChars.Length)];
		}

		// Shuffle the password
		return new string(password.OrderBy(_ => random.Next()).ToArray());
	}

	private async Task SendTemporaryPasswordEmailAsync(User user, string temporaryPassword, DateTime expiresAt)
	{
		try
		{
			var placeholders = new Dictionary<string, string>
			{
				{ "FirstName", user.FirstName },
				{ "LastName", user.LastName },
				{ "Email", user.Email! },
				{ "TemporaryPassword", temporaryPassword },
				{ "ExpiresAt", expiresAt.ToString("MMMM dd, yyyy HH:mm UTC") }
			};

			// Try to use template, fallback to inline HTML
			string body;
			try
			{
				body = await _emailTemplateService.GetTemplateAsync("DoctorTemporaryPassword.html", placeholders);
			}
			catch
			{
				// Fallback template if file doesn't exist
				body = $@"
					<h2>Welcome to Medical System!</h2>
					<p>Dear Dr. {user.FirstName} {user.LastName},</p>
					<p>Your doctor account has been created. Here are your temporary login credentials:</p>
					<p><strong>Email:</strong> {user.Email}</p>
					<p><strong>Temporary Password:</strong> <code>{temporaryPassword}</code></p>
					<p style='color: #dc3545;'><strong>Important:</strong> This password will expire on {expiresAt:MMMM dd, yyyy HH:mm} UTC (30 minutes from account creation).</p>
					<p>For security reasons, you will be required to change your password after your first login.</p>
					<p>Best regards,<br>Medical System Team</p>
				";
			}

			await _notificationService.SendEmailAsync(user.Email!, "Your Doctor Account - Temporary Password", body);
			_logger.LogInformation("Temporary password email sent to doctor: {Email}", user.Email);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to send temporary password email to doctor: {Email}", user.Email);
		}
	}
}
