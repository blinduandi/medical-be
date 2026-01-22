using medical_be.Services;

namespace medical_be.Services;

public class MedicalMonitoringBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MedicalMonitoringBackgroundService> _logger;

    public MedicalMonitoringBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<MedicalMonitoringBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Medical Monitoring Background Service started at {Time}", DateTime.UtcNow);

        try
        {
            // Wait a bit before starting the first analysis
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var patternService = scope.ServiceProvider.GetRequiredService<IPatternDetectionService>();

                    _logger.LogInformation("Starting automated medical pattern detection analysis at {Time}", DateTime.UtcNow);

                    // Run complete pattern analysis
                    var result = await patternService.RunCompleteAnalysisAsync();
                    
                    _logger.LogInformation("Pattern detection completed. Summary: {TotalAlerts} alerts, {HighRiskPatients} high-risk patients", 
                        result.Summary.TotalAlerts, result.Summary.TotalHighRiskPatients);

                    // Log high severity patterns
                    var highSeverityPatterns = result.Patterns.Where(p => p.Severity == "HIGH").ToList();
                    if (highSeverityPatterns.Any())
                    {
                        _logger.LogWarning("CRITICAL: {HighSeverityCount} high severity patterns detected:", highSeverityPatterns.Count);
                        
                        foreach (var pattern in highSeverityPatterns.Take(5)) // Log first 5
                        {
                            _logger.LogWarning("  - {PatternType}: {Description}", pattern.PatternType, pattern.Description);
                        }
                    }

                // Log top risk patients
                if (result.HighRiskPatients.Any())
                {
                    _logger.LogInformation("Top {Count} high-risk patients identified:", Math.Min(result.HighRiskPatients.Count, 5));
                    foreach (var patientId in result.HighRiskPatients.Take(5))
                    {
                        var riskScore = await patternService.CalculateRiskScoreAsync(patientId);
                        _logger.LogInformation("  - Patient ID: {PatientId}, Risk Score: {RiskScore:F3}", 
                            patientId, riskScore);
                    }
                }

                // Log recommendations
                if (result.Recommendations.Any())
                {
                    _logger.LogInformation("System recommendations:");
                    foreach (var recommendation in result.Recommendations.Take(3))
                    {
                        _logger.LogInformation("  - {Recommendation}", recommendation);
                    }
                }

                // Log pattern analysis completion
                _logger.LogInformation("Pattern analysis completed successfully at {Timestamp}", DateTime.UtcNow);

                // Here you could:
                // - Store alerts in database for persistence
                // - Send notifications to medical staff
                // - Trigger automated responses
                // - Generate reports

                _logger.LogInformation("Next analysis scheduled for {NextTime}", DateTime.UtcNow.AddHours(6));

                // Wait 6 hours before next analysis
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Medical monitoring background service cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in medical monitoring background service");
                
                // Wait 1 hour before retry on error
                try
                {
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Medical monitoring background service cancelled during retry wait");
                    break;
                }
            }
        }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Medical monitoring background service cancelled during initialization");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in medical monitoring background service");
        }

        _logger.LogInformation("Medical Monitoring Background Service stopped at {Time}", DateTime.UtcNow);
    }
}
