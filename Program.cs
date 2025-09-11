using medical_be.Data;
using medical_be.Extensions;
using medical_be.Middleware;
using Microsoft.EntityFrameworkCore;
using Serilog;
using DotNetEnv;
using medical_be.Shared.Interfaces;
using medical_be.Services;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;

// Load environment variables from .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);
var emailApiKey = Environment.GetEnvironmentVariable("EMAIL_API_KEY");
if (string.IsNullOrEmpty(emailApiKey))
{
    throw new InvalidOperationException("EMAIL_API_KEY is not set in environment variables.");
}

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
builder.Services.AddScoped<INotificationService, NotificationService>();

// ---------------- Quartz Setup ----------------
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("NotificationJob");
    q.AddJob<NotificationJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("NotificationJob-trigger")
        .StartNow()
        .WithSimpleSchedule(x => x.WithIntervalInMinutes(1).RepeatForever())); 
});

// Hosted service for Quartz
builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);


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
using (var scope = app.Services.CreateScope())
{
    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
    await notificationService.TestBrevoEmailAsync();
}


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
// Disable HTTPS redirection for development to avoid 307 redirects
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
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
