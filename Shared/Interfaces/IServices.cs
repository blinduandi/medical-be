using medical_be.Shared.DTOs;
using medical_be.DTOs;

namespace medical_be.Shared.Interfaces;

public interface IAuthService
{
    Task<medical_be.Shared.DTOs.AuthResponseDto> AuthenticateAsync(medical_be.Shared.DTOs.LoginDto loginDto);
    Task<medical_be.Shared.DTOs.UserDto> CreateUserAsync(medical_be.Shared.DTOs.CreateUserDto createUserDto);
    Task<medical_be.Shared.DTOs.UserDto> GetUserByIdAsync(Guid userId);
    Task<medical_be.Shared.DTOs.UserDto> UpdateUserAsync(Guid userId, medical_be.Shared.DTOs.UpdateUserDto updateUserDto);
    Task<bool> ValidateTokenAsync(string token);
    Task<bool> DeleteUserAsync(Guid userId);
    Task<List<medical_be.Shared.DTOs.UserDto>> GetUsersAsync();
    Task<List<medical_be.Shared.DTOs.UserDto>> GetUsersByRoleAsync(string role);

    // Extended methods used by controllers
    Task<medical_be.DTOs.RegistrationResultDto> RegisterAsync(medical_be.DTOs.RegisterDto registerDto);
    Task<medical_be.DTOs.AuthResponseDto?> LoginAsync(medical_be.DTOs.LoginDto loginDto);
    Task<medical_be.DTOs.AuthResponseDto?> VerifyMfaLoginAsync(string email, string otp);
    Task<bool> ChangePasswordAsync(string userId, medical_be.DTOs.ChangePasswordDto dto);
    Task<medical_be.DTOs.UserDto?> GetUserByIdAsync(string userId);
    Task<medical_be.DTOs.UserDto?> UpdateUserAsync(string userId, medical_be.DTOs.UpdateUserDto dto);
    Task<bool> AssignRoleAsync(string userId, string roleName);
    Task<bool> RemoveRoleAsync(string userId, string roleName);
    
    // Email verification methods
    Task<bool> GenerateVerificationCodeAsync(string email);
    Task<medical_be.DTOs.VerificationResponseDto> VerifyCodeAsync(string email, string code);
    
    // Password reset methods
    Task<bool> ForgotPasswordAsync(string email);
    Task<medical_be.DTOs.ResetPasswordResponseDto> ResetPasswordAsync(medical_be.DTOs.ResetPasswordDto dto);
    Task<medical_be.DTOs.ResetPasswordResponseDto> ChangePasswordWithTokenAsync(medical_be.DTOs.ChangePasswordWithTokenDto dto);
    
    // Doctor account creation with temporary password
    Task<medical_be.DTOs.DoctorCreationResultDto> CreateDoctorWithTemporaryPasswordAsync(medical_be.DTOs.RegisterDto registerDto);
}

public interface IMedicalService
{
    Task<medical_be.DTOs.AppointmentDto> CreateAppointmentAsync(medical_be.DTOs.CreateAppointmentDto appointmentDto);
    Task<medical_be.DTOs.AppointmentDto> UpdateAppointmentAsync(int id, medical_be.DTOs.UpdateAppointmentDto appointmentDto);
    Task<List<medical_be.DTOs.AppointmentDto>> GetUserAppointmentsAsync(string userId);
    Task<List<medical_be.DTOs.AppointmentDto>> GetDoctorAppointmentsAsync(string doctorId);
    Task<medical_be.DTOs.AppointmentDto?> GetAppointmentByIdAsync(int id);
    Task<bool> CancelAppointmentAsync(int id);
}

public interface IAuditService
{
    Task LogActivityAsync(string action, Guid userId, string details, string? entityType = null, Guid? entityId = null);
    Task<List<AuditLogDto>> GetAuditLogsAsync(DateTime from, DateTime to);
    Task<List<AuditLogDto>> GetUserAuditLogsAsync(Guid userId, DateTime from, DateTime to);
    Task<List<AuditLogDto>> GetEntityAuditLogsAsync(string entityType, Guid entityId);

    // Overload matching controller usage (string userId and optional IP)
    Task LogAuditAsync(string userId, string action, string details, string? entityType = null, Guid? entityId = null, string? ipAddress = null);
}

public interface INotificationService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendSmsAsync(string phoneNumber, string message);
    Task SendAppointmentReminderAsync(int appointmentId);
    Task SendRegistrationWelcomeAsync(Guid userId);
    Task SendPasswordResetAsync(string email, string resetToken);
    Task TestBrevoEmailAsync();

}
