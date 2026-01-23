using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using medical_be.Helpers;
using medical_be.Services;

namespace medical_be.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataSeederController : ControllerBase
{
    private readonly IDataSeedingService _seedingService;
    private readonly ILogger<DataSeederController> _logger;

    public DataSeederController(
        IDataSeedingService seedingService,
        ILogger<DataSeederController> logger)
    {
        _seedingService = seedingService;
        _logger = logger;
    }

    /// <summary>
    /// Seed database with comprehensive test data for presentation
    /// </summary>
    /// <param name="userCount">Number of users to create (default: 50)</param>
    [HttpPost("seed")]
    public async Task<IActionResult> SeedData([FromQuery] int userCount = 50)
    {
        try
        {
            _logger.LogInformation("Starting data seeding with {Count} users...", userCount);
            
            var result = await _seedingService.SeedLargeDatasetAsync(userCount);
            
            if (result.Success)
            {
                return ApiResponse.Success(new
                {
                    result.UsersCreated,
                    result.VisitsCreated,
                    result.AllergiesCreated,
                    result.VaccinationsCreated,
                    result.LabResultsCreated,
                    result.DiagnosesCreated,
                    result.AppointmentsCreated,
                    result.RatingsCreated,
                    result.DoctorsCreated,
                    result.PatientsCreated,
                    ProcessingTime = result.ProcessingTime.ToString(@"hh\:mm\:ss"),
                    result.Message
                }, result.Message);
            }
            
            return ApiResponse.Error(result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding data");
            return ApiResponse.Error("Failed to seed data");
        }
    }

    /// <summary>
    /// Get current seeding status
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        try
        {
            var status = await _seedingService.GetSeedingStatusAsync();
            return ApiResponse.Success(status, "Seeding status retrieved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seeding status");
            return ApiResponse.Error("Failed to get status");
        }
    }

    /// <summary>
    /// Clear all seeded data (WARNING: This will delete all test data)
    /// </summary>
    [HttpDelete("clear")]
    public async Task<IActionResult> ClearData()
    {
        try
        {
            await _seedingService.ClearSeedDataAsync();
            return ApiResponse.Success(null, "All seeded data cleared successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing data");
            return ApiResponse.Error("Failed to clear data");
        }
    }

    /// <summary>
    /// Seed patients and ratings for a specific doctor
    /// </summary>
    /// <param name="doctorId">Doctor ID</param>
    /// <param name="patientCount">Number of patients to create (default: 20)</param>
    [HttpPost("seed-doctor-patients/{doctorId}")]
    public async Task<IActionResult> SeedDoctorPatients(string doctorId, [FromQuery] int patientCount = 20)
    {
        try
        {
            _logger.LogInformation("Seeding {Count} patients for doctor {DoctorId}...", patientCount, doctorId);
            
            var result = await _seedingService.SeedPatientsForDoctorAsync(doctorId, patientCount);
            
            if (result.Success)
            {
                return ApiResponse.Success(new
                {
                    result.PatientsCreated,
                    result.AppointmentsCreated,
                    result.RatingsCreated,
                    result.Message
                }, result.Message);
            }
            
            return ApiResponse.Error(result.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding patients for doctor");
            return ApiResponse.Error("Failed to seed patients for doctor");
        }
    }
}
