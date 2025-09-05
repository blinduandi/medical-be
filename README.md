# Medical API

A comprehensive medical management API built with .NET 9.0, featuring user authentication, appointment management, and medical records.

## Features

- **Authentication & Authorization**: JWT-based authentication with role-based access control
- **User Management**: Registration, login, profile management
- **Role-Based Permissions**: Admin, Doctor, Nurse, Patient, Receptionist roles
- **Medical Records**: Comprehensive patient medical record management
- **Appointments**: Appointment scheduling and management
- **Security**: Security headers, error handling, input validation
- **Logging**: Structured logging with Serilog

## Tech Stack

- **.NET 9.0**
- **Entity Framework Core** (SQL Server)
- **ASP.NET Core Identity**
- **JWT Authentication**
- **FluentValidation**
- **Serilog**
- **Swagger/OpenAPI**

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- SQL Server (LocalDB is sufficient for development)

### Installation

1. Clone the repository
2. Navigate to the project directory
3. Copy `.env.example` to `.env` and configure your environment variables:
   ```bash
   cp .env.example .env
   ```
4. Update the `.env` file with your specific configuration values
5. Restore packages:
   ```bash
   dotnet restore
   ```

6. Apply migrations:
   ```bash
   dotnet ef database update
   ```

7. Run the application:
   ```bash
   dotnet run
   ```

The API will be available at `https://localhost:5001` or `http://localhost:5000`.

## API Documentation

Once the application is running, visit `https://localhost:5001` to access the Swagger UI documentation.

## Default Users

The application seeds the following default users:

- **Admin**: admin@medical.com / Admin123!
- **Doctor**: doctor@medical.com / Doctor123!

## Project Structure

```
├── Controllers/         # API Controllers
├── Data/               # Database Context
├── DTOs/               # Data Transfer Objects
├── Extensions/         # Service Extensions
├── Middleware/         # Custom Middleware
├── Models/             # Entity Models
├── Repositories/       # Repository Pattern
├── Services/           # Business Logic Services
├── Validators/         # FluentValidation Validators
└── Properties/         # Launch Settings
```

## Security Features

- JWT token authentication
- Password hashing with BCrypt
- Role-based authorization
- Security headers middleware
- Input validation
- CORS protection
- Rate limiting (basic implementation)

## API Endpoints

### Authentication
- `POST /api/auth/register` - User registration
- `POST /api/auth/login` - User login
- `POST /api/auth/change-password` - Change password
- `GET /api/auth/me` - Get current user
- `PUT /api/auth/me` - Update current user

### Admin Only
- `POST /api/auth/assign-role/{userId}` - Assign role to user
- `DELETE /api/auth/remove-role/{userId}` - Remove role from user

## Configuration

### Environment Variables

The application uses a `.env` file for configuration. Copy `.env.example` to `.env` and update the values:

**Required Environment Variables:**
- `DB_CONNECTION_STRING` - Database connection string
- `JWT_SECRET_KEY` - Secret key for JWT token signing (minimum 32 characters)
- `JWT_ISSUER` - JWT token issuer
- `JWT_AUDIENCE` - JWT token audience
- `JWT_EXPIRATION_MINUTES` - JWT token expiration time in minutes

**Optional Environment Variables:**
- `EMAIL_API_KEY` - API key for email service
- `SMS_API_KEY` - API key for SMS service  
- `PAYMENT_API_KEY` - API key for payment service
- `CLOUD_STORAGE_API_KEY` - API key for cloud storage
- `ALLOWED_ORIGINS` - Comma-separated list of allowed CORS origins
- `LOG_LEVEL` - Logging level (Debug, Information, Warning, Error)

### Legacy Configuration

For backward compatibility, you can still configure settings in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MedicalDb;..."
  },
  "Jwt": {
    "SecretKey": "YourSecretKey",
    "Issuer": "MedicalAPI",
    "Audience": "MedicalAPIUsers",
    "ExpirationMinutes": 60
  }
}
```

**Note:** Environment variables take precedence over appsettings.json values.

## Development

### Adding Migrations

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Running Tests

```bash
dotnet test
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License.
