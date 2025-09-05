using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace medical_be.Services
{
    public interface IOtpService
    {
        Task<string> GenerateOtpAsync(string userId);
        Task<bool> ValidateOtpAsync(string userId, string otp);
        Task<bool> SendOtpAsync(string userId, string phoneNumber);
        Task CleanupExpiredOtpsAsync();
    }

    public class OtpService : IOtpService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<OtpService> _logger;
        private readonly ISmsService _smsService;
        private readonly int _otpExpiryMinutes = 5;
        private readonly int _otpLength = 6;

        public OtpService(
            IMemoryCache cache,
            ILogger<OtpService> logger,
            ISmsService smsService)
        {
            _cache = cache;
            _logger = logger;
            _smsService = smsService;
        }

    public Task<string> GenerateOtpAsync(string userId)
        {
            try
            {
                // Generate random OTP
                var otp = GenerateRandomOtp();
                var cacheKey = $"otp_{userId}";
                
                // Store OTP in cache with expiry
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_otpExpiryMinutes)
                };

                _cache.Set(cacheKey, otp, cacheOptions);
                
        _logger.LogInformation("OTP generated for user: {UserId}", userId);
        return Task.FromResult(otp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating OTP for user: {UserId}", userId);
                throw;
            }
        }

    public Task<bool> ValidateOtpAsync(string userId, string otp)
        {
            try
            {
                var cacheKey = $"otp_{userId}";
                
                if (_cache.TryGetValue(cacheKey, out string? cachedOtp))
                {
                    if (cachedOtp == otp)
                    {
                        // Remove OTP from cache after successful validation
                        _cache.Remove(cacheKey);
                        _logger.LogInformation("OTP validated successfully for user: {UserId}", userId);
            return Task.FromResult(true);
                    }
                }

                _logger.LogWarning("Invalid OTP attempt for user: {UserId}", userId);
        return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating OTP for user: {UserId}", userId);
        return Task.FromResult(false);
            }
        }

        public async Task<bool> SendOtpAsync(string userId, string phoneNumber)
        {
            try
            {
                var otp = await GenerateOtpAsync(userId);
                var message = $"Your medical portal verification code is: {otp}. Valid for {_otpExpiryMinutes} minutes.";
                
                return await _smsService.SendSmsAsync(phoneNumber, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP to user: {UserId}", userId);
                return false;
            }
        }

        public Task CleanupExpiredOtpsAsync()
        {
            // Cache automatically handles expiry, no manual cleanup needed
            return Task.CompletedTask;
        }

        private string GenerateRandomOtp()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            
            var number = Math.Abs(BitConverter.ToInt32(bytes, 0));
            return (number % (int)Math.Pow(10, _otpLength)).ToString($"D{_otpLength}");
        }
    }

    // SMS Service Interface and Implementation
    public interface ISmsService
    {
        Task<bool> SendSmsAsync(string phoneNumber, string message);
    }

    public class SmsService : ISmsService
    {
        private readonly ILogger<SmsService> _logger;
        private readonly IConfiguration _configuration;

        public SmsService(ILogger<SmsService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                // In development, just log the SMS
                if (_configuration.GetValue<string>("Environment") == "Development")
                {
                    _logger.LogInformation("SMS to {PhoneNumber}: {Message}", phoneNumber, message);
                    return Task.FromResult(true);
                }

                // TODO: Implement actual SMS service (Twilio, AWS SNS, etc.)
                // For now, we'll simulate success
                _logger.LogInformation("SMS sent to {PhoneNumber}", phoneNumber);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS to {PhoneNumber}", phoneNumber);
                return Task.FromResult(false);
            }
        }
    }
}
