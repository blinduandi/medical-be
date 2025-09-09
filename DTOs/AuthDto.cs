using System.ComponentModel.DataAnnotations;
using medical_be.Models;

namespace medical_be.DTOs;

public enum UserRegistrationType
{
    Patient = 0,
    Doctor = 1
}

public class RegisterDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    public Gender Gender { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [Required]
    [MaxLength(13)]
    public string IDNP { get; set; } = string.Empty;

    [Required]
    public UserRegistrationType UserRole { get; set; } = UserRegistrationType.Patient;
}

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class VerifyMfaLoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Otp { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public UserDto User { get; set; } = null!;
    public bool RequiresMfa { get; set; } = false;
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
}

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare("NewPassword")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class UpdateUserDto
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Phone]
    public string? PhoneNumber { get; set; }

    [Required]
    public DateTime DateOfBirth { get; set; }

    [Required]
    public Gender Gender { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }
}

public class GetVerificationCodeDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class VerifyCodeDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}

public class VerificationResponseDto
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class RegistrationResultDto
{
    public bool Success { get; set; }
    public AuthResponseDto? AuthResponse { get; set; }
    public List<string> Errors { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
