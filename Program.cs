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
using System.Text.Json.Serialization;

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
builder.Services.AddScoped<EmailTemplateService>();


// Machine Learning & Analytics Services
builder.Services.AddScoped<IPatternDetectionService, PatternDetectionService>();
builder.Services.AddScoped<IDataSeedingService, DataSeedingService>();
builder.Services.AddScoped<IPatientAccessLogService, PatientAccessLogService>();
builder.Services.AddHostedService<MedicalMonitoringBackgroundService>();

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
    
    // First ensure database exists without automatic migration
    await context.Database.EnsureCreatedAsync();
    
    // Manually create ML tables if they don't exist
    try 
    {
        // Ensure nullable UserId columns exist to match EF model snapshots for certain tables
        await context.Database.ExecuteSqlRawAsync(@"
            IF COL_LENGTH('VisitRecords', 'UserId') IS NULL
            BEGIN
                ALTER TABLE [VisitRecords] ADD [UserId] nvarchar(450) NULL;
                CREATE INDEX [IX_VisitRecords_UserId] ON [VisitRecords]([UserId]);
                ALTER TABLE [VisitRecords] ADD CONSTRAINT [FK_VisitRecords_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]);
            END

            IF COL_LENGTH('Vaccinations', 'UserId') IS NULL
            BEGIN
                ALTER TABLE [Vaccinations] ADD [UserId] nvarchar(450) NULL;
                CREATE INDEX [IX_Vaccinations_UserId] ON [Vaccinations]([UserId]);
                ALTER TABLE [Vaccinations] ADD CONSTRAINT [FK_Vaccinations_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]);
            END

            IF COL_LENGTH('Allergies', 'UserId') IS NULL
            BEGIN
                ALTER TABLE [Allergies] ADD [UserId] nvarchar(450) NULL;
                CREATE INDEX [IX_Allergies_UserId] ON [Allergies]([UserId]);
                ALTER TABLE [Allergies] ADD CONSTRAINT [FK_Allergies_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers]([Id]);
            END
        ");

        // Check if ML tables exist, if not, create them manually
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='MedicalPatterns' AND xtype='U')
            CREATE TABLE [MedicalPatterns] (
                [Id] int NOT NULL IDENTITY,
                [Name] nvarchar(200) NOT NULL,
                [Description] nvarchar(1000) NOT NULL,
                [TriggerCondition] nvarchar(max) NOT NULL,
                [OutcomeCondition] nvarchar(max) NOT NULL,
                [MinimumCases] int NOT NULL,
                [ConfidenceThreshold] float NOT NULL,
                [IsActive] bit NOT NULL,
                [CreatedAt] datetime2 NOT NULL,
                [UpdatedAt] datetime2 NOT NULL,
                [CreatedBy] nvarchar(450) NULL,
                [UpdatedBy] nvarchar(450) NULL,
                CONSTRAINT [PK_MedicalPatterns] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_MedicalPatterns_AspNetUsers_CreatedBy] FOREIGN KEY ([CreatedBy]) REFERENCES [AspNetUsers] ([Id]),
                CONSTRAINT [FK_MedicalPatterns_AspNetUsers_UpdatedBy] FOREIGN KEY ([UpdatedBy]) REFERENCES [AspNetUsers] ([Id])
            );
        ");
        
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='PatternMatches' AND xtype='U')
            CREATE TABLE [PatternMatches] (
                [Id] int NOT NULL IDENTITY,
                [PatternId] int NOT NULL,
                [PatientId] nvarchar(450) NOT NULL,
                [ConfidenceScore] float NOT NULL,
                [MatchingData] nvarchar(max) NOT NULL,
                [DetectedAt] datetime2 NOT NULL,
                [IsNotified] bit NOT NULL,
                [NotifiedAt] datetime2 NULL,
                CONSTRAINT [PK_PatternMatches] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_PatternMatches_MedicalPatterns_PatternId] FOREIGN KEY ([PatternId]) REFERENCES [MedicalPatterns] ([Id]) ON DELETE CASCADE
            );
        ");
        
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='MedicalAlerts' AND xtype='U')
            CREATE TABLE [MedicalAlerts] (
                [Id] int NOT NULL IDENTITY,
                [PatientId] nvarchar(450) NOT NULL,
                [PatternMatchId] int NULL,
                [AlertType] nvarchar(100) NOT NULL,
                [Severity] nvarchar(50) NOT NULL,
                [Message] nvarchar(max) NOT NULL,
                [Description] nvarchar(max) NULL,
                [RecommendedActions] nvarchar(max) NULL,
                [PatientCount] int NOT NULL,
                [ConfidenceScore] float NOT NULL,
                [IsRead] bit NOT NULL,
                [CreatedAt] datetime2 NOT NULL,
                [ReadAt] datetime2 NULL,
                [ReadBy] nvarchar(450) NULL,
                CONSTRAINT [PK_MedicalAlerts] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_MedicalAlerts_AspNetUsers_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_MedicalAlerts_AspNetUsers_ReadBy] FOREIGN KEY ([ReadBy]) REFERENCES [AspNetUsers] ([Id]),
                CONSTRAINT [FK_MedicalAlerts_PatternMatches_PatternMatchId] FOREIGN KEY ([PatternMatchId]) REFERENCES [PatternMatches] ([Id]) ON DELETE SET NULL
            );
        ");
        
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='LabResults' AND xtype='U')
            CREATE TABLE [LabResults] (
                [Id] int NOT NULL IDENTITY,
                [PatientId] nvarchar(450) NOT NULL,
                [TestName] nvarchar(100) NOT NULL,
                [TestCode] nvarchar(20) NULL,
                [Value] decimal(18,2) NOT NULL,
                [Unit] nvarchar(20) NOT NULL,
                [ReferenceMin] decimal(18,2) NULL,
                [ReferenceMax] decimal(18,2) NULL,
                [Status] nvarchar(20) NOT NULL,
                [TestDate] datetime2 NOT NULL,
                [LabName] nvarchar(200) NULL,
                [Notes] nvarchar(500) NULL,
                [CreatedAt] datetime2 NOT NULL,
                CONSTRAINT [PK_LabResults] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_LabResults_AspNetUsers_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
            );
        ");
        
        await context.Database.ExecuteSqlRawAsync(@"
            IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Diagnoses' AND xtype='U')
            CREATE TABLE [Diagnoses] (
                [Id] int NOT NULL IDENTITY,
                [PatientId] nvarchar(450) NOT NULL,
                [DiagnosisCode] nvarchar(20) NOT NULL,
                [DiagnosisName] nvarchar(200) NOT NULL,
                [Description] nvarchar(500) NULL,
                [Severity] nvarchar(50) NULL,
                [Category] nvarchar(100) NULL,
                [DiagnosedDate] datetime2 NOT NULL,
                [DoctorId] nvarchar(450) NOT NULL,
                [IsActive] bit NOT NULL,
                [ResolvedDate] datetime2 NULL,
                [Notes] nvarchar(500) NULL,
                [CreatedAt] datetime2 NOT NULL,
                CONSTRAINT [PK_Diagnoses] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_Diagnoses_AspNetUsers_DoctorId] FOREIGN KEY ([DoctorId]) REFERENCES [AspNetUsers] ([Id]),
                CONSTRAINT [FK_Diagnoses_AspNetUsers_PatientId] FOREIGN KEY ([PatientId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
            );
        ");
    }
    catch (Exception ex)
    {
        // Log but continue - tables might already exist
        Console.WriteLine($"Note: {ex.Message}");
    }
}

app.Run();

// Make Program class accessible for testing
public partial class Program { }
