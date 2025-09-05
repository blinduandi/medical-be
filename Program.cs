using medical_be.Data;
using medical_be.Extensions;
using medical_be.Middleware;
using Microsoft.EntityFrameworkCore;
using Serilog;
using DotNetEnv;

// Load environment variables from .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.Host.UseSerilog();

// Load environment configuration
builder.Services.AddEnvironmentConfiguration(builder.Configuration);

// Add services to the container
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddValidators();
builder.Services.AddSerilogLogging();
builder.Services.AddCorsPolicy(builder.Configuration);

// Add controllers
builder.Services.AddControllers();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(medical_be.Mapping.MappingProfile));

// Add API Explorer and Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();

// Security configurations
builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 5001;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Medical API V1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
    });
}

// Security middleware
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();

// HTTPS and CORS
app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigins");

// Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

// Rate limiting (basic implementation)
app.Use(async (context, next) =>
{
    context.Response.Headers["X-RateLimit-Limit"] = "100";
    context.Response.Headers["X-RateLimit-Remaining"] = "99";
    await next.Invoke();
});

// Map controllers
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => new
{
    Status = "Healthy",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow
}).AllowAnonymous();

// Seed database
await app.SeedDataAsync();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();
}

app.Run();

// Make Program class accessible for testing
public partial class Program { }
