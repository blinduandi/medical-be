using medical_be.Shared.Interfaces;
using medical_be.Models;
using medical_be.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using brevo_csharp.Api;
using brevo_csharp.Client;
using brevo_csharp.Model;


namespace medical_be.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationService> _logger;
        private readonly TransactionalEmailsApi _brevoApi;
        private readonly string _fromEmail = "janetagrigoras@gmail.com";
        private readonly string _fromName = "Medical System";

        public NotificationService(ApplicationDbContext context,
                                   IConfiguration configuration,
                                   ILogger<NotificationService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;

            var emailApiKey = Environment.GetEnvironmentVariable("EMAIL_API_KEY");
            if (string.IsNullOrEmpty(emailApiKey))
            {
                _logger.LogWarning("Brevo API key not configured. Emails will not be sent.");
            }
            var config = new brevo_csharp.Client.Configuration();
            config.ApiKey.Add("api-key", emailApiKey ?? string.Empty);
            _brevoApi = new TransactionalEmailsApi(config);
        }

                public async System.Threading.Tasks.Task TestBrevoEmailAsync()
                {
                        await System.Threading.Tasks.Task.Yield();
                        // Intentionally no-op in development unless explicitly invoked.
                        // Example: await SendEmailAsync("example@example.com", "Test Email", "<p>This is a test.</p>");
                }


        public async System.Threading.Tasks.Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var sendEmail = new SendSmtpEmail
                    {
                        To = new List<SendSmtpEmailTo> { new SendSmtpEmailTo(email: to) },
                        Sender = new SendSmtpEmailSender(name: _fromName, email: _fromEmail),
                        Subject = subject,
                        HtmlContent = body
                    };


                var response = await _brevoApi.SendTransacEmailAsync(sendEmail);
                _logger.LogInformation("Email sent via Brevo to: {To}", to);

                var log = new NotificationLog
                {
                    Id = Guid.NewGuid(),
                    RecipientEmail = to,
                    Type = NotificationType.Email,
                    Subject = subject,
                    Content = body,
                    Status = NotificationStatus.Sent,
                    SentAt = DateTime.UtcNow
                };

                _context.NotificationLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to: {To}", to);

                var log = new NotificationLog
                {
                    Id = Guid.NewGuid(),
                    RecipientEmail = to,
                    Type = NotificationType.Email,
                    Subject = subject,
                    Content = body,
                    Status = NotificationStatus.Failed,
                    ErrorMessage = ex.Message
                };

                _context.NotificationLogs.Add(log);
                await _context.SaveChangesAsync();
            }
        }

        public async System.Threading.Tasks.Task SendSmsAsync(string phoneNumber, string message)
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

                var log = new NotificationLog
                {
                    Id = Guid.NewGuid(),
                    RecipientPhone = phoneNumber,
                    Type = NotificationType.SMS,
                    Subject = "SMS Notification",
                    Content = message,
                    Status = NotificationStatus.Sent,
                    SentAt = DateTime.UtcNow
                };

                _context.NotificationLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS to: {PhoneNumber}", phoneNumber);
            }
        }

        public async System.Threading.Tasks.Task SendAppointmentReminderAsync(int appointmentId)
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

                // Patient email
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
                await SendEmailAsync(appointment.Patient.Email!, patientSubject, patientBody);

                // Doctor email
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
                await SendEmailAsync(appointment.Doctor.Email!, doctorSubject, doctorBody);

                _logger.LogInformation("Appointment reminders sent for appointment: {AppointmentId}", appointmentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending appointment reminder for: {AppointmentId}", appointmentId);
            }
        }

        public async System.Threading.Tasks.Task SendRegistrationWelcomeAsync(Guid userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending welcome email to user: {UserId}", userId);
            }
        }

        public async System.Threading.Tasks.Task SendPasswordResetAsync(string email, string resetToken)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    _logger.LogWarning("User with email {Email} not found for password reset", email);
                    return;
                }

                var subject = "Password Reset Code";
                var body = $@"
                    <h2>Password Reset Request</h2>
                    <p>Dear {user.FirstName} {user.LastName},</p>
                    <p>We received a request to reset your password for your Medical System account.</p>
                    <p>Your password reset code is: <strong style='font-size: 24px; color: #007bff;'>{resetToken}</strong></p>
                    <p>This code will expire in 15 minutes for security reasons.</p>
                    <p>If you didn't request this password reset, please ignore this email or contact support.</p>
                    <p>Best regards,<br>Medical System Team</p>
                ";

                await SendEmailAsync(email, subject, body);
                _logger.LogInformation("Password reset code sent to: {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to: {Email}", email);
            }
        }
    }
}
