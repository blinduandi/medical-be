using System.ComponentModel.DataAnnotations;

namespace medical_be.DTOs;

public class MedicalAlertDto
{
    public int Id { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? RecommendedActions { get; set; }
    public int PatientCount { get; set; }
    public double ConfidenceScore { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? ReadByName { get; set; }
}

public class PatientRiskDto
{
    public string PatientId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string? BloodType { get; set; }
    public double RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public List<string> RiskFactors { get; set; } = new();
    public int RecentVisits { get; set; }
    public int TotalAllergies { get; set; }
    public int VaccinationCount { get; set; }
    public DateTime? LastVisit { get; set; }
}

public class CorrelationAnalysisDto
{
    public string Factor1 { get; set; } = string.Empty;
    public string Factor2 { get; set; } = string.Empty;
    public double CorrelationStrength { get; set; }
    public string Significance { get; set; } = string.Empty;
    public int SampleSize { get; set; }
    public string Insight { get; set; } = string.Empty;
    
    // Legacy properties for backward compatibility
    public double AgeVisitCorrelation { get; set; }
    public double AgeAllergyCorrelation { get; set; }
    public double AllergyVisitCorrelation { get; set; }
    public double VaccinationVisitCorrelation { get; set; }
    public double BloodTypeRiskCorrelation { get; set; }
    public string AnalysisDate { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
}

public class SeasonalTrendDto
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int VisitCount { get; set; }
    public double AverageVisitsPerDay { get; set; }
    public double PercentageOfAverage { get; set; }
    public string TrendType { get; set; } = string.Empty; // HIGH, NORMAL, LOW
}

/// <summary>
/// Individual pattern detection result
/// </summary>
public class PatternDetectionResultDto
{
    public string PatternName { get; set; } = string.Empty;
    public string PatternType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public List<string>? AffectedPatients { get; set; }
    public string Recommendation { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Summary of all pattern detection results
/// </summary>
public class PatternDetectionSummaryResultDto
{
    public List<MedicalAlertDto> PatternAlerts { get; set; } = new();
    public List<PatientRiskDto> HighRiskPatients { get; set; } = new();
    public CorrelationAnalysisDto CorrelationAnalysis { get; set; } = new();
    public List<SeasonalTrendDto> SeasonalTrends { get; set; } = new();
    public PatternSummaryDto Summary { get; set; } = new();
}

public class PatternSummaryDto
{
    public int TotalAlerts { get; set; }
    public int HighSeverityAlerts { get; set; }
    public int TotalHighRiskPatients { get; set; }
    public int TotalPatientsAnalyzed { get; set; }
    public double OverallRiskPercentage { get; set; }
    public DateTime AnalysisCompletedAt { get; set; } = DateTime.UtcNow;
    public string NextAnalysisScheduled { get; set; } = DateTime.UtcNow.AddHours(6).ToString("yyyy-MM-dd HH:mm:ss");
}

public class CreateLabResultDto
{
    [Required]
    public string PatientId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string TestName { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string? TestCode { get; set; }
    
    [Required]
    public decimal Value { get; set; }
    
    [Required]
    [StringLength(20)]
    public string Unit { get; set; } = string.Empty;
    
    public decimal? ReferenceMin { get; set; }
    public decimal? ReferenceMax { get; set; }
    
    [Required]
    public DateTime TestDate { get; set; }
    
    [StringLength(200)]
    public string? LabName { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
}

public class CreateDiagnosisDto
{
    [Required]
    public string PatientId { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    public string DiagnosisCode { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string DiagnosisName { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [StringLength(50)]
    public string? Severity { get; set; }
    
    [StringLength(100)]
    public string? Category { get; set; }
    
    [Required]
    public DateTime DiagnosedDate { get; set; }
    
    [Required]
    public string DoctorId { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Notes { get; set; }
}

public class BloodTypeAnalysisDto
{
    public string BloodType { get; set; } = string.Empty;
    public int Count { get; set; }
    public double AvgAge { get; set; }
    public double AvgVisits { get; set; }
    public int HighRiskCount { get; set; }
    public double RiskPercentage { get; set; }
    public double AvgAllergies { get; set; }
    public double AvgLabAbnormalities { get; set; }
}

public class AgeGroupAnalysisDto
{
    public string AgeGroup { get; set; } = string.Empty;
    public int Count { get; set; }
    public double AvgVisits { get; set; }
    public int HighRiskCount { get; set; }
    public double RiskPercentage { get; set; }
    public int ChronicConditions { get; set; }
    public int LowVaccinationRate { get; set; }
    public double AvgLabValues { get; set; }
}

public class MedicalInsightDto
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public object Data { get; set; } = new();
    public string Recommendation { get; set; } = string.Empty;
    public double ConfidenceLevel { get; set; }
}

public class PredictiveAnalyticsDto
{
    public List<PatientRiskDto> HighVisitPrediction { get; set; } = new();
    public List<SeasonalTrendDto> SeasonalPredictions { get; set; } = new();
    public List<MedicalInsightDto> Insights { get; set; } = new();
    public ModelAccuracyDto ModelAccuracy { get; set; } = new();
}

public class ModelAccuracyDto
{
    public string VisitPrediction { get; set; } = "85%";
    public string SeasonalTrends { get; set; } = "92%";
    public string RiskAssessment { get; set; } = "78%";
    public string PatternDetection { get; set; } = "83%";
}

public class PatientAnalyticsDto
{
    public PatientInfoDto PatientInfo { get; set; } = new();
    public RiskAssessmentDto RiskAssessment { get; set; } = new();
    public MedicalHistoryDto MedicalHistory { get; set; } = new();
    public List<MedicalAlertDto> PatternAlerts { get; set; } = new();
    public PredictionsDto Predictions { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class PatientInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string? BloodType { get; set; }
    public string? Gender { get; set; }
}

public class RiskAssessmentDto
{
    public double RiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public List<string> RiskFactors { get; set; } = new();
}

public class MedicalHistoryDto
{
    public int TotalVisits { get; set; }
    public int RecentVisits { get; set; }
    public DateTime? LastVisit { get; set; }
    public int AllergiesCount { get; set; }
    public int VaccinationsCount { get; set; }
    public int DiagnosesCount { get; set; }
    public int LabResultsCount { get; set; }
}

public class PredictionsDto
{
    public int PredictedNextYearVisits { get; set; }
    public List<string> RecommendedActions { get; set; } = new();
    public double HealthTrendScore { get; set; }
}

/// <summary>
/// Response DTO for data seeding operations
/// </summary>
public class DataSeedingResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int UsersCreated { get; set; }
    public int DoctorsCreated { get; set; }
    public int PatientsCreated { get; set; }
    public int AppointmentsCreated { get; set; }
    public int RatingsCreated { get; set; }
    public int VisitsCreated { get; set; }
    public int AllergiesCreated { get; set; }
    public int VaccinationsCreated { get; set; }
    public int LabResultsCreated { get; set; }
    public int DiagnosesCreated { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public DateTime CompletedAt { get; set; }
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Status DTO for data seeding progress
/// </summary>
public class DataSeedingStatusDto
{
    public int TotalUsers { get; set; }
    public int TotalVisits { get; set; }
    public int TotalAllergies { get; set; }
    public int TotalVaccinations { get; set; }
    public int TotalLabResults { get; set; }
    public int TotalDiagnoses { get; set; }
    public bool IsLargeDatasetSeeded { get; set; }
    public DateTime LastSeedingDate { get; set; }
    public double AverageDataPerUser { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// DTO for pattern detection summary
/// </summary>
public class PatternDetectionSummaryDto
{
    public AnalysisSummaryDto Summary { get; set; } = new();
    public List<PatternDetectionResultDto> Patterns { get; set; } = new();
    public List<string> HighRiskPatients { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// DTO for analysis summary
/// </summary>
public class AnalysisSummaryDto
{
    public int TotalPatterns { get; set; }
    public int TotalAlerts { get; set; }
    public int TotalHighRiskPatients { get; set; }
    public DateTime AnalysisCompletedAt { get; set; }
}

/// <summary>
/// DTO for predictive insights
/// </summary>
public class PredictiveInsightDto
{
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PredictionType { get; set; } = string.Empty;
    public double RiskScore { get; set; }
    public double Probability { get; set; }
    public string TimeFrame { get; set; } = string.Empty;
    public List<string> RecommendedActions { get; set; } = new();
    public double Confidence { get; set; }
}

/// <summary>
/// Response DTO for seasonal health trends analysis
/// </summary>
public class SeasonalTrendsDto
{
    public string Season { get; set; } = string.Empty;
    public int Month { get; set; }
    public int TotalVisits { get; set; }
    public int RespiratoryIssues { get; set; }
    public int AllergicReactions { get; set; }
    public int FlucVaccinations { get; set; }
    public double AverageAgeOfPatients { get; set; }
    public List<string> CommonSymptoms { get; set; } = new();
    public double TrendStrength { get; set; }
}
