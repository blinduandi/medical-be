using DotNetEnv;

namespace medical_be.Extensions;

public static class EnvironmentExtensions
{
    public static IServiceCollection AddEnvironmentConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Override configuration with environment variables from .env file
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(connectionString))
        {
            configuration["ConnectionStrings:DefaultConnection"] = connectionString;
        }

        var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
        if (!string.IsNullOrEmpty(jwtSecretKey))
        {
            configuration["Jwt:SecretKey"] = jwtSecretKey;
        }

        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
        if (!string.IsNullOrEmpty(jwtIssuer))
        {
            configuration["Jwt:Issuer"] = jwtIssuer;
        }

        var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
        if (!string.IsNullOrEmpty(jwtAudience))
        {
            configuration["Jwt:Audience"] = jwtAudience;
        }

        var jwtExpirationMinutes = Environment.GetEnvironmentVariable("JWT_EXPIRATION_MINUTES");
        if (!string.IsNullOrEmpty(jwtExpirationMinutes))
        {
            configuration["Jwt:ExpirationMinutes"] = jwtExpirationMinutes;
        }

        var logLevel = Environment.GetEnvironmentVariable("LOG_LEVEL");
        if (!string.IsNullOrEmpty(logLevel))
        {
            configuration["Logging:LogLevel:Default"] = logLevel;
        }

        return services;
    }
}
