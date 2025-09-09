using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using medical_be.Models;
using medical_be.Shared.Interfaces;

namespace medical_be.Services;

public class AuthService : IAuthService
{
	private readonly UserManager<User> _userManager;
	private readonly SignInManager<User> _signInManager;
	private readonly IJwtService _jwtService;
	private readonly ILogger<AuthService> _logger;

	public AuthService(
		UserManager<User> userManager,
		SignInManager<User> signInManager,
		IJwtService jwtService,
		ILogger<AuthService> logger)
	{
		_userManager = userManager;
		_signInManager = signInManager;
		_jwtService = jwtService;
		_logger = logger;
	}

	// Legacy/shared interface methods
	public async Task<medical_be.Shared.DTOs.AuthResponseDto> AuthenticateAsync(medical_be.Shared.DTOs.LoginDto loginDto)
	{
		// Bridge to new LoginAsync
		var result = await LoginAsync(new medical_be.DTOs.LoginDto { Email = loginDto.Email, Password = loginDto.Password });
		if (result == null) throw new UnauthorizedAccessException("Invalid credentials");
		return new medical_be.Shared.DTOs.AuthResponseDto
		{
			Token = result.Token,
			ExpiresAt = result.Expiration,
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
			EmailConfirmed = true,
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
	public async Task<medical_be.DTOs.AuthResponseDto?> RegisterAsync(medical_be.DTOs.RegisterDto registerDto)
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
		if (!result.Succeeded) 
		{
			_logger.LogWarning("User registration failed for {Email}. Errors: {Errors}", 
				registerDto.Email, string.Join("; ", result.Errors.Select(e => e.Description)));
			return null;
		}

		// Assign role based on UserRole parameter
		var roleName = registerDto.UserRole == medical_be.DTOs.UserRegistrationType.Doctor ? "Doctor" : "Patient";
		var roleResult = await _userManager.AddToRoleAsync(user, roleName);
		if (!roleResult.Succeeded)
		{
			_logger.LogWarning("Failed to assign role {Role} to user {Email}. Errors: {Errors}", 
				roleName, registerDto.Email, string.Join("; ", roleResult.Errors.Select(e => e.Description)));
		}

		var roles = await _userManager.GetRolesAsync(user);
		var token = _jwtService.GenerateToken(user, roles);

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
			}
		};
	}

	public async Task<medical_be.DTOs.AuthResponseDto?> LoginAsync(medical_be.DTOs.LoginDto loginDto)
	{
		var user = await _userManager.FindByEmailAsync(loginDto.Email);
		if (user == null || !user.IsActive) return null;

		var check = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, lockoutOnFailure: true);
		if (!check.Succeeded) return null;

		var roles = await _userManager.GetRolesAsync(user);
		var token = _jwtService.GenerateToken(user, roles);

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
			RequiresMfa = false
		};
	}

	public async Task<medical_be.DTOs.AuthResponseDto?> VerifyMfaLoginAsync(string email, string otp)
	{
		// Minimal stub to satisfy controllers; real OTP flow handled by MfaController
		var user = await _userManager.FindByEmailAsync(email);
		if (user == null) return null;
		var roles = await _userManager.GetRolesAsync(user);
		var token = _jwtService.GenerateToken(user, roles);
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
			RequiresMfa = false
		};
	}

	public async Task<bool> ChangePasswordAsync(string userId, medical_be.DTOs.ChangePasswordDto dto)
	{
		var user = await _userManager.FindByIdAsync(userId);
		if (user == null) return false;
		var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
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

		// Generate a 6-digit verification code
		var code = "123456";//new Random().Next(100000, 999999).ToString();
		
		// Set expiration to 15 minutes from now
		user.VerificationCode = code;
		user.VerificationCodeExpires = DateTime.UtcNow.AddMinutes(15);

		var result = await _userManager.UpdateAsync(user);
		if (result.Succeeded)
		{
			_logger.LogInformation("Verification code generated for user: {Email}", email);
			// TODO: Send email with verification code
			// This would integrate with your email service
			return true;
		}

		_logger.LogError("Failed to update user with verification code for email: {Email}. Errors: {Errors}", 
			email, string.Join("; ", result.Errors.Select(e => e.Description)));
		return false;
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
			_logger.LogWarning("Invalid verification code provided for user: {Email}", email);
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
		if (result.Succeeded)
		{
			_logger.LogInformation("Email verified successfully for user: {Email}", email);
			return new medical_be.DTOs.VerificationResponseDto
			{
				IsValid = true,
				Message = "Email verified successfully"
			};
		}

		_logger.LogError("Failed to update user after email verification for email: {Email}. Errors: {Errors}", 
			email, string.Join("; ", result.Errors.Select(e => e.Description)));
		return new medical_be.DTOs.VerificationResponseDto
		{
			IsValid = false,
			Message = "Failed to complete verification. Please try again."
		};
	}
}
