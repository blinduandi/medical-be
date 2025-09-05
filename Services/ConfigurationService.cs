namespace medical_be.Services;

public interface IConfigurationService
{
    string GetConnectionString();
    string GetJwtSecretKey();
    string GetJwtIssuer();
    string GetJwtAudience();
    int GetJwtExpirationMinutes();
    string GetEmailApiKey();
    string GetSmsApiKey();
    string GetPaymentApiKey();
    string GetCloudStorageApiKey();
    string[] GetAllowedOrigins();
}

public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;

    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetConnectionString()
    {
        return Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
               ?? _configuration.GetConnectionString("DefaultConnection")
               ?? throw new InvalidOperationException("Database connection string not found");
    }

    public string GetJwtSecretKey()
    {
        return Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
               ?? _configuration["Jwt:SecretKey"]
               ?? throw new InvalidOperationException("JWT secret key not found");
    }

    public string GetJwtIssuer()
    {
        return Environment.GetEnvironmentVariable("JWT_ISSUER")
               ?? _configuration["Jwt:Issuer"]
               ?? throw new InvalidOperationException("JWT issuer not found");
    }

    public string GetJwtAudience()
    {
        return Environment.GetEnvironmentVariable("JWT_AUDIENCE")
               ?? _configuration["Jwt:Audience"]
               ?? throw new InvalidOperationException("JWT audience not found");
    }

    public int GetJwtExpirationMinutes()
    {
        var envValue = Environment.GetEnvironmentVariable("JWT_EXPIRATION_MINUTES");
        if (!string.IsNullOrEmpty(envValue) && int.TryParse(envValue, out var envMinutes))
        {
            return envMinutes;
        }

        var configValue = _configuration["Jwt:ExpirationMinutes"];
        if (!string.IsNullOrEmpty(configValue) && int.TryParse(configValue, out var configMinutes))
        {
            return configMinutes;
        }

        return 60; // Default to 60 minutes
    }

    public string GetEmailApiKey()
    {
        return Environment.GetEnvironmentVariable("EMAIL_API_KEY")
               ?? _configuration["ExternalServices:EmailApiKey"]
               ?? string.Empty;
    }

    public string GetSmsApiKey()
    {
        return Environment.GetEnvironmentVariable("SMS_API_KEY")
               ?? _configuration["ExternalServices:SmsApiKey"]
               ?? string.Empty;
    }

    public string GetPaymentApiKey()
    {
        return Environment.GetEnvironmentVariable("PAYMENT_API_KEY")
               ?? _configuration["ExternalServices:PaymentApiKey"]
               ?? string.Empty;
    }

    public string GetCloudStorageApiKey()
    {
        return Environment.GetEnvironmentVariable("CLOUD_STORAGE_API_KEY")
               ?? _configuration["ExternalServices:CloudStorageApiKey"]
               ?? string.Empty;
    }

    public string[] GetAllowedOrigins()
    {
        var envValue = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS");
        if (!string.IsNullOrEmpty(envValue))
        {
            return envValue.Split(',', StringSplitOptions.RemoveEmptyEntries);
        }

        var configValue = _configuration["Cors:AllowedOrigins"];
        if (!string.IsNullOrEmpty(configValue))
        {
            return configValue.Split(',', StringSplitOptions.RemoveEmptyEntries);
        }

        return new[] { "http://localhost:3000", "https://localhost:3000" };
    }
}
