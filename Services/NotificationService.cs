using medical_be.Shared.Interfaces;
using medical_be.Models;
using medical_be.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;

namespace medical_be.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var smtpHost = _configuration["SMTP_HOST"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["SMTP_PORT"] ?? "587");
            var smtpUsername = _configuration["SMTP_USERNAME"];
            var smtpPassword = _configuration["SMTP_PASSWORD"];

            if (string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning("SMTP credentials not configured. Email not sent.");
                return;
            }

            using var client = new SmtpClient(smtpHost, smtpPort);
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpUsername, "Medical System"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(to);
            await client.SendMailAsync(mailMessage);

            var notificationLog = new NotificationLog
            {
                Id = Guid.NewGuid(),
                RecipientEmail = to,
                Type = NotificationType.Email,
                Subject = subject,
                Content = body,
                Status = NotificationStatus.Sent,
                SentAt = DateTime.UtcNow
            };

            _context.NotificationLogs.Add(notificationLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Email sent successfully to: {To}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to: {To}", to);

            var notificationLog = new NotificationLog
            {
                Id = Guid.NewGuid(),
                RecipientEmail = to,
                Type = NotificationType.Email,
                Subject = subject,
                Content = body,
                Status = NotificationStatus.Failed,
                ErrorMessage = ex.Message
            };

            _context.NotificationLogs.Add(notificationLog);
            await _context.SaveChangesAsync();
            throw;
        }
    }

    public async Task SendSmsAsync(string phoneNumber, string message)
    {
        try
        {
            var twilioSid = _configuration["TWILIO_SID"];
            var twilioToken = _configuration["TWILIO_TOKEN"];

            if (string.IsNullOrEmpty(twilioSid) || string.IsNullOrEmpty(twilioToken))
            {
                _logger.LogWarning("Twilio credentials not configured. SMS not sent.");
                return;
            }

            _logger.LogInformation("SMS would be sent to {PhoneNumber}: {Message}", phoneNumber, message);

            var notificationLog = new NotificationLog
            {
                Id = Guid.NewGuid(),
                RecipientPhone = phoneNumber,
                Type = NotificationType.SMS,
                Subject = "SMS Notification",
                Content = message,
                Status = NotificationStatus.Sent,
                SentAt = DateTime.UtcNow
            };

            _context.NotificationLogs.Add(notificationLog);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS to: {PhoneNumber}", phoneNumber);
            throw;
        }
    }

    public async Task SendAppointmentReminderAsync(int appointmentId)
    {
        try
        {
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == appointmentId);

            if (appointment == null)
            {
                _logger.LogWarning("Appointment {AppointmentId} not found for reminder", appointmentId);
                return;
            }

            var patientEmail = appointment.Patient.Email;
            var doctorEmail = appointment.Doctor.Email;

            var patientSubject = "Appointment Confirmation";
            var patientBody = $@"
                <h2>Appointment Confirmed</h2>
                <p>Dear {appointment.Patient.FirstName} {appointment.Patient.LastName},</p>
                <p>Your appointment has been scheduled with Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName}.</p>
                <p><strong>Date & Time:</strong> {appointment.AppointmentDate:dddd, MMMM dd, yyyy 'at' h:mm tt}</p>
                <p><strong>Reason:</strong> {appointment.Reason}</p>
                <p>Please arrive 15 minutes before your scheduled time.</p>
                <p>Best regards,<br>Medical System</p>
            ";

            await SendEmailAsync(patientEmail!, patientSubject, patientBody);

            var doctorSubject = "New Appointment Scheduled";
            var doctorBody = $@"
                <h2>New Appointment</h2>
                <p>Dear Dr. {appointment.Doctor.FirstName} {appointment.Doctor.LastName},</p>
                <p>A new appointment has been scheduled with {appointment.Patient.FirstName} {appointment.Patient.LastName}.</p>
                <p><strong>Date & Time:</strong> {appointment.AppointmentDate:dddd, MMMM dd, yyyy 'at' h:mm tt}</p>
                <p><strong>Reason:</strong> {appointment.Reason}</p>
                <p>Patient Contact: {appointment.Patient.PhoneNumber}</p>
                <p>Best regards,<br>Medical System</p>
            ";

            await SendEmailAsync(doctorEmail!, doctorSubject, doctorBody);
            _logger.LogInformation("Appointment reminders sent for appointment: {AppointmentId}", appointmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending appointment reminder for: {AppointmentId}", appointmentId);
        }
    }

    public async Task SendRegistrationWelcomeAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId.ToString());
            if (user?.Email == null)
            {
                _logger.LogWarning("User {UserId} not found or has no email for welcome message", userId);
                return;
            }

            var subject = "Welcome to Medical System";
            var body = $@"
                <h2>Welcome to Medical System!</h2>
                <p>Dear {user.FirstName} {user.LastName},</p>
                <p>Thank you for registering with our medical system. Your account has been created successfully.</p>
                <p><strong>Your Details:</strong></p>
                <ul>
                    <li>Email: {user.Email}</li>
                    <li>Registration Date: {user.CreatedAt:MMMM dd, yyyy}</li>
                </ul>
                <p>You can now:</p>
                <ul>
                    <li>Schedule appointments with doctors</li>
                    <li>View your medical records</li>
                    <li>Manage your profile</li>
                </ul>
                <p>If you have any questions, please contact our support team.</p>
                <p>Best regards,<br>Medical System Team</p>
            ";

            await SendEmailAsync(user.Email, subject, body);
            _logger.LogInformation("Welcome email sent to new user: {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending welcome email to user: {UserId}", userId);
        }
    }

    public async Task SendPasswordResetAsync(string email, string resetToken)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                _logger.LogWarning("User with email {Email} not found for password reset", email);
                return;
            }

            var subject = "Password Reset Request";
            var resetUrl = $"{_configuration["FRONTEND_URL"]}/reset-password?token={resetToken}&email={email}";
            
            var body = $@"
                <h2>Password Reset Request</h2>
                <p>Dear {user.FirstName} {user.LastName},</p>
                <p>We received a request to reset your password for your Medical System account.</p>
                <p>Click the link below to reset your password:</p>
                <p><a href='{resetUrl}' style='background-color: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>Reset Password</a></p>
                <p>This link will expire in 24 hours for security reasons.</p>
                <p>If you didn't request this password reset, please ignore this email or contact support if you have concerns.</p>
                <p>Best regards,<br>Medical System Team</p>
            ";

            await SendEmailAsync(email, subject, body);
            _logger.LogInformation("Password reset email sent to: {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset email to: {Email}", email);
        }
    }
}
