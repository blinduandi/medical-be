# Medical API Environment Setup Guide

## Quick Setup

1. **Copy environment file:**
   ```bash
   cp .env.example .env
   ```

2. **Configure your .env file with your specific values**

3. **Install packages and run:**
   ```bash
   dotnet restore
   dotnet ef database update
   dotnet run
   ```

## Environment Variables Explained

### Database Configuration
- **DB_CONNECTION_STRING**: Your database connection string
  - Development: `Server=(localdb)\mssqllocaldb;Database=MedicalDb_Dev;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true`
  - Production: Use your production database connection string

### JWT Configuration
- **JWT_SECRET_KEY**: Secret key for signing JWT tokens (minimum 32 characters)
  - Generate a secure key: Use a password generator or run `openssl rand -base64 32`
- **JWT_ISSUER**: The issuer of the JWT token (usually your API name)
- **JWT_AUDIENCE**: The audience for the JWT token (usually your client application)
- **JWT_EXPIRATION_MINUTES**: How long tokens should be valid (in minutes)

### Future API Keys (Commented out in .env)
- **EMAIL_API_KEY**: For email service integration (SendGrid, Mailgun, etc.)
- **SMS_API_KEY**: For SMS service integration (Twilio, etc.)
- **PAYMENT_API_KEY**: For payment processing (Stripe, PayPal, etc.)
- **CLOUD_STORAGE_API_KEY**: For cloud storage (AWS S3, Azure Blob, etc.)

### Security Settings
- **ALLOWED_ORIGINS**: Comma-separated list of allowed CORS origins
  - Example: `http://localhost:3000,https://yourdomain.com`

### Logging
- **LOG_LEVEL**: Controls how verbose logging should be
  - Options: `Debug`, `Information`, `Warning`, `Error`, `Critical`

## Environment-Specific Configurations

### Development
```env
ASPNETCORE_ENVIRONMENT=Development
JWT_EXPIRATION_MINUTES=1440  # 24 hours for easier development
LOG_LEVEL=Information
DB_CONNECTION_STRING=Server=(localdb)\mssqllocaldb;Database=MedicalDb_Dev;...
```

### Production
```env
ASPNETCORE_ENVIRONMENT=Production
JWT_EXPIRATION_MINUTES=60    # 1 hour for better security
LOG_LEVEL=Warning
DB_CONNECTION_STRING=Server=your-prod-server;Database=MedicalDb;...
```

## Security Best Practices

1. **Never commit .env files** - They're already in .gitignore
2. **Use strong, unique secret keys** - Generate new ones for each environment
3. **Rotate keys regularly** - Especially in production
4. **Use environment-specific configurations** - Different settings for dev/staging/prod
5. **Limit CORS origins** - Only allow trusted domains

## Troubleshooting

### Common Issues

1. **"Connection string not found"**
   - Make sure `DB_CONNECTION_STRING` is set in your .env file
   - Check that the .env file is in the project root directory

2. **"JWT secret key not found"**
   - Ensure `JWT_SECRET_KEY` is set and at least 32 characters long

3. **Database connection issues**
   - Verify your SQL Server is running
   - Check the connection string format
   - Ensure the database exists or run `dotnet ef database update`

4. **CORS errors**
   - Add your frontend URL to `ALLOWED_ORIGINS`
   - Format: `http://localhost:3000,https://localhost:3000`

### Debugging

Enable verbose logging by setting:
```env
LOG_LEVEL=Debug
```

This will show detailed information about configuration loading and database operations.
