using medical_be.DTOs;

namespace medical_be.Services;

public interface IPatternDetectionService
{
    Task<List<PatternDetectionResultDto>> DetectPatternsAsync();
    Task<PatternDetectionSummaryDto> RunCompleteAnalysisAsync();
    Task<double> CalculateRiskScoreAsync(string patientId);
    Task<PatientAnalyticsDto> GetPatientAnalyticsAsync(string patientId);
    Task<List<CorrelationAnalysisDto>> AnalyzeCorrelationsAsync();
    Task<List<SeasonalTrendsDto>> AnalyzeSeasonalTrendsAsync();
    Task<List<PredictiveInsightDto>> GetPredictiveInsightsAsync();
}
