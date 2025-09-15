using Microsoft.EntityFrameworkCore;
using medical_be.Data;
using medical_be.Models;
using medical_be.DTOs;

namespace medical_be.Services
{
    /// <summary>
    /// Core pattern detection service for medical analytics and ML insights
    /// </summary>
    public class PatternDetectionService : IPatternDetectionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PatternDetectionService> _logger;

        public PatternDetectionService(ApplicationDbContext context, ILogger<PatternDetectionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<PatternDetectionResultDto>> DetectPatternsAsync()
        {
            var results = new List<PatternDetectionResultDto>();

            try
            {
                // Run all pattern detection algorithms
                var highVisitPatients = await DetectHighVisitFrequencyPatternsAsync();
                var bloodTypeCorrelations = await DetectBloodTypeCorrelationsAsync();
                var vaccinationGaps = await DetectVaccinationGapsAsync();
                var seasonalPatterns = await DetectSeasonalPatternsAsync();
                var allergyCluster = await DetectAllergyClustersAsync();
                var labAnomalies = await DetectLabAnomaliesAsync();
                var diagnosisTrends = await DetectDiagnosisTrendsAsync();

                results.AddRange(highVisitPatients);
                results.AddRange(bloodTypeCorrelations);
                results.AddRange(vaccinationGaps);
                results.AddRange(seasonalPatterns);
                results.AddRange(allergyCluster);
                results.AddRange(labAnomalies);
                results.AddRange(diagnosisTrends);

                _logger.LogInformation("Pattern detection completed. Found {ResultCount} patterns", results.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during pattern detection");
                throw;
            }

            return results;
        }

        public async Task<PatternDetectionSummaryDto> RunCompleteAnalysisAsync()
        {
            var allPatterns = await DetectPatternsAsync();
            
            var highRiskPatients = new List<string>();
            var alerts = allPatterns.Where(p => p.Severity == "HIGH").ToList();
            
            // Calculate risk scores for patients mentioned in patterns
            foreach (var pattern in alerts)
            {
                if (pattern.AffectedPatients?.Any() == true)
                {
                    foreach (var patientId in pattern.AffectedPatients)
                    {
                        var riskScore = await CalculateRiskScoreAsync(patientId);
                        if (riskScore > 0.6 && !highRiskPatients.Contains(patientId))
                        {
                            highRiskPatients.Add(patientId);
                        }
                    }
                }
            }

            return new PatternDetectionSummaryDto
            {
                Summary = new AnalysisSummaryDto
                {
                    TotalPatterns = allPatterns.Count,
                    TotalAlerts = alerts.Count,
                    TotalHighRiskPatients = highRiskPatients.Count,
                    AnalysisCompletedAt = DateTime.UtcNow
                },
                Patterns = allPatterns,
                HighRiskPatients = highRiskPatients,
                Recommendations = GenerateRecommendations(allPatterns)
            };
        }

        public async Task<double> CalculateRiskScoreAsync(string patientId)
        {
            try
            {
                var patient = await _context.Users
                    .Include(u => u.PatientVisitRecords)
                    .Include(u => u.PatientAllergies)
                    .Include(u => u.PatientVaccinations)
                    .Include(u => u.LabResults)
                    .FirstOrDefaultAsync(u => u.Id == patientId);

                if (patient == null) return 0;

                double riskScore = 0;
                var age = patient.DateOfBirth != default(DateTime) ? DateTime.Now.Year - patient.DateOfBirth.Year : 0;

                // Age factor (0-0.3)
                if (age > 80) riskScore += 0.3;
                else if (age > 70) riskScore += 0.2;
                else if (age > 60) riskScore += 0.1;

                // Visit frequency (0-0.25)
                var recentVisits = patient.PatientVisitRecords.Count(v => v.VisitDate >= DateTime.UtcNow.AddMonths(-6));
                if (recentVisits > 6) riskScore += 0.25;
                else if (recentVisits > 4) riskScore += 0.15;
                else if (recentVisits > 2) riskScore += 0.1;

                // Allergies (0-0.15)
                var severeAllergies = patient.PatientAllergies.Count(a => a.Severity == AllergySeverity.Severe);
                if (severeAllergies > 3) riskScore += 0.15;
                else if (severeAllergies > 1) riskScore += 0.1;
                else if (patient.PatientAllergies.Any()) riskScore += 0.05;

                // Vaccination status (0-0.1)
                var vaccinationCount = patient.PatientVaccinations.Count;
                if (vaccinationCount == 0) riskScore += 0.1;
                else if (vaccinationCount < 2) riskScore += 0.05;

                // Lab results anomalies (0-0.2)
                var recentAbnormalLabs = patient.LabResults.Count(lr => 
                    lr.TestDate >= DateTime.UtcNow.AddMonths(-3) && 
                    (lr.Status == "HIGH" || lr.Status == "LOW" || lr.Status == "CRITICAL"));
                if (recentAbnormalLabs > 5) riskScore += 0.2;
                else if (recentAbnormalLabs > 3) riskScore += 0.15;
                else if (recentAbnormalLabs > 1) riskScore += 0.1;

                return Math.Min(riskScore, 1.0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating risk score for patient {PatientId}", patientId);
                return 0;
            }
        }

        public async Task<PatientAnalyticsDto> GetPatientAnalyticsAsync(string patientId)
        {
            var patient = await _context.Users
                .Include(u => u.PatientVisitRecords)
                .Include(u => u.PatientAllergies)
                .Include(u => u.PatientVaccinations)
                .Include(u => u.LabResults)
                .Include(u => u.PatientDiagnoses)
                .FirstOrDefaultAsync(u => u.Id == patientId);

            if (patient == null)
                throw new ArgumentException("Patient not found");

            var riskScore = await CalculateRiskScoreAsync(patientId);
            var age = patient.DateOfBirth != default(DateTime) ? DateTime.Now.Year - patient.DateOfBirth.Year : 0;

            return new PatientAnalyticsDto
            {
                PatientInfo = new PatientInfoDto
                {
                    Id = patient.Id,
                    FirstName = patient.FirstName,
                    LastName = patient.LastName,
                    Age = age,
                    BloodType = patient.BloodType,
                    Gender = patient.Gender.ToString()
                },
                RiskAssessment = new RiskAssessmentDto
                {
                    RiskScore = Math.Round(riskScore, 3),
                    RiskLevel = GetRiskLevel(riskScore),
                    RiskFactors = GetRiskFactors(patient, age)
                },
                MedicalHistory = new MedicalHistoryDto
                {
                    TotalVisits = patient.PatientVisitRecords.Count,
                    RecentVisits = patient.PatientVisitRecords.Count(v => v.VisitDate >= DateTime.UtcNow.AddMonths(-6)),
                    LastVisit = patient.PatientVisitRecords.OrderByDescending(v => v.VisitDate).FirstOrDefault()?.VisitDate,
                    AllergiesCount = patient.PatientAllergies.Count,
                    VaccinationsCount = patient.PatientVaccinations.Count,
                    DiagnosesCount = patient.PatientDiagnoses.Count,
                    LabResultsCount = patient.LabResults.Count
                },
                Predictions = new PredictionsDto
                {
                    PredictedNextYearVisits = PredictNextYearVisits(patient.PatientVisitRecords.ToList()),
                    RecommendedActions = GetRecommendedActions(patient, riskScore),
                    HealthTrendScore = CalculateHealthTrend(patient)
                }
            };
        }

        public async Task<List<CorrelationAnalysisDto>> AnalyzeCorrelationsAsync()
        {
            var correlations = new List<CorrelationAnalysisDto>();

            try
            {
                // Blood type vs. visit frequency correlation
                var bloodTypeData = await _context.Users
                    .Where(u => !string.IsNullOrEmpty(u.BloodType))
                    .Include(u => u.PatientVisitRecords)
                    .GroupBy(u => u.BloodType)
                    .Select(g => new
                    {
                        BloodType = g.Key,
                        Count = g.Count(),
                        AvgVisits = g.Average(u => u.PatientVisitRecords.Count)
                    })
                    .ToListAsync();

                if (bloodTypeData.Count > 1)
                {
                    var correlation = CalculateCorrelation(
                        bloodTypeData.Select(x => (double)x.Count).ToList(),
                        bloodTypeData.Select(x => x.AvgVisits).ToList()
                    );

                    correlations.Add(new CorrelationAnalysisDto
                    {
                        Factor1 = "Blood Type Distribution",
                        Factor2 = "Average Visits",
                        CorrelationStrength = Math.Round(correlation, 3),
                        Significance = GetSignificanceLevel(Math.Abs(correlation)),
                        SampleSize = bloodTypeData.Sum(x => x.Count),
                        Insight = GenerateCorrelationInsight("Blood Type", "Visit Frequency", correlation)
                    });
                }

                // Age vs. allergy correlation
                var ageAllergyData = await _context.Users
                    .Where(u => u.DateOfBirth != default(DateTime))
                    .Include(u => u.PatientAllergies)
                    .Select(u => new
                    {
                        Age = DateTime.Now.Year - u.DateOfBirth.Year,
                        AllergyCount = u.PatientAllergies.Count
                    })
                    .ToListAsync();

                if (ageAllergyData.Count > 1)
                {
                    var ageAllergyCorr = CalculateCorrelation(
                        ageAllergyData.Select(x => (double)x.Age).ToList(),
                        ageAllergyData.Select(x => (double)x.AllergyCount).ToList()
                    );

                    correlations.Add(new CorrelationAnalysisDto
                    {
                        Factor1 = "Patient Age",
                        Factor2 = "Allergy Count",
                        CorrelationStrength = Math.Round(ageAllergyCorr, 3),
                        Significance = GetSignificanceLevel(Math.Abs(ageAllergyCorr)),
                        SampleSize = ageAllergyData.Count,
                        Insight = GenerateCorrelationInsight("Age", "Allergies", ageAllergyCorr)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in correlation analysis");
            }

            return correlations;
        }

        public async Task<List<SeasonalTrendsDto>> AnalyzeSeasonalTrendsAsync()
        {
            var trends = new List<SeasonalTrendsDto>();

            try
            {
                var monthlyData = await _context.VisitRecords
                    .Where(v => v.VisitDate >= DateTime.UtcNow.AddYears(-2))
                    .Include(v => v.Patient)
                    .GroupBy(v => v.VisitDate.Month)
                    .Select(g => new
                    {
                        Month = g.Key,
                        TotalVisits = g.Count(),
                        AverageAge = g.Average(v => DateTime.Now.Year - v.Patient.DateOfBirth.Year),
                        RespiratoryIssues = g.Count(v => v.Symptoms.ToLower().Contains("cough") || 
                                                        v.Symptoms.ToLower().Contains("fever") ||
                                                        v.Symptoms.ToLower().Contains("cold")),
                        AllergicReactions = g.Count(v => v.Symptoms.ToLower().Contains("allerg") ||
                                                        v.Symptoms.ToLower().Contains("rash") ||
                                                        v.Symptoms.ToLower().Contains("itch"))
                    })
                    .ToListAsync();

                foreach (var monthData in monthlyData)
                {
                    trends.Add(new SeasonalTrendsDto
                    {
                        Month = monthData.Month,
                        Season = GetSeason(monthData.Month),
                        TotalVisits = monthData.TotalVisits,
                        RespiratoryIssues = monthData.RespiratoryIssues,
                        AllergicReactions = monthData.AllergicReactions,
                        AverageAgeOfPatients = Math.Round(monthData.AverageAge, 1),
                        CommonSymptoms = GetCommonSymptomsForMonth(monthData.Month),
                        TrendStrength = CalculateTrendStrength(monthData.TotalVisits, monthlyData.Average(x => x.TotalVisits))
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in seasonal trends analysis");
            }

            return trends.OrderBy(t => t.Month).ToList();
        }

        public async Task<List<PredictiveInsightDto>> GetPredictiveInsightsAsync()
        {
            var insights = new List<PredictiveInsightDto>();

            try
            {
                // Predict patients at risk of developing chronic conditions
                var highRiskPatients = await _context.Users
                    .Where(u => u.DateOfBirth != default(DateTime))
                    .Include(u => u.PatientVisitRecords)
                    .Include(u => u.PatientAllergies)
                    .Where(u => u.PatientVisitRecords.Count >= 5) // Frequent visitors
                    .Select(u => new
                    {
                        u.Id,
                        u.FirstName,
                        u.LastName,
                        Age = DateTime.Now.Year - u.DateOfBirth.Year,
                        VisitCount = u.PatientVisitRecords.Count,
                        RecentVisits = u.PatientVisitRecords.Count(v => v.VisitDate >= DateTime.UtcNow.AddMonths(-6)),
                        HasAllergies = u.PatientAllergies.Any()
                    })
                    .ToListAsync();

                foreach (var patient in highRiskPatients.Take(20)) // Limit for performance
                {
                    var riskScore = await CalculateRiskScoreAsync(patient.Id);
                    if (riskScore > 0.5)
                    {
                        insights.Add(new PredictiveInsightDto
                        {
                            PatientId = patient.Id,
                            PatientName = $"{patient.FirstName} {patient.LastName}",
                            PredictionType = "Chronic Condition Risk",
                            RiskScore = Math.Round(riskScore, 3),
                            Probability = Math.Round(riskScore * 100, 1),
                            TimeFrame = "6-12 months",
                            RecommendedActions = new List<string>
                            {
                                "Schedule comprehensive health assessment",
                                "Monitor vital signs more frequently",
                                "Review medication adherence",
                                "Lifestyle counseling recommended"
                            },
                            Confidence = Math.Round(0.7 + (riskScore * 0.2), 2)
                        });
                    }
                }

                // Predict vaccination needs
                var vaccinationNeeds = await _context.Users
                    .Include(u => u.PatientVaccinations)
                    .Where(u => u.PatientVaccinations.Count < 3)
                    .Take(10)
                    .ToListAsync();

                foreach (var patient in vaccinationNeeds)
                {
                    insights.Add(new PredictiveInsightDto
                    {
                        PatientId = patient.Id,
                        PatientName = $"{patient.FirstName} {patient.LastName}",
                        PredictionType = "Vaccination Gap",
                        RiskScore = 0.3,
                        Probability = 85.0,
                        TimeFrame = "Next 3 months",
                        RecommendedActions = new List<string>
                        {
                            "Review vaccination history",
                            "Schedule missing vaccinations",
                            "Educate about vaccine importance"
                        },
                        Confidence = 0.9
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating predictive insights");
            }

            return insights;
        }

        #region Private Helper Methods

        private async Task<List<PatternDetectionResultDto>> DetectHighVisitFrequencyPatternsAsync()
        {
            var results = new List<PatternDetectionResultDto>();

            try
            {
                var frequentPatients = await _context.Users
                    .Include(u => u.PatientVisitRecords)
                    .Where(u => u.PatientVisitRecords.Count(v => v.VisitDate >= DateTime.UtcNow.AddMonths(-6)) >= 5)
                    .Select(u => new
                    {
                        u.Id,
                        u.FirstName,
                        u.LastName,
                        Age = DateTime.Now.Year - u.DateOfBirth.Year,
                        RecentVisits = u.PatientVisitRecords.Count(v => v.VisitDate >= DateTime.UtcNow.AddMonths(-6))
                    })
                    .ToListAsync();

                if (frequentPatients.Count >= 3)
                {
                    results.Add(new PatternDetectionResultDto
                    {
                        PatternName = "High Visit Frequency Pattern",
                        PatternType = "VISIT_FREQUENCY",
                        Description = $"Found {frequentPatients.Count} patients with unusually high visit frequency (5+ visits in 6 months)",
                        Severity = frequentPatients.Count > 10 ? "HIGH" : "MEDIUM",
                        ConfidenceScore = Math.Min(frequentPatients.Count / 20.0, 1.0),
                        AffectedPatients = frequentPatients.Select(p => p.Id).ToList(),
                        Recommendation = "Investigate underlying health conditions, consider comprehensive health assessments",
                        DetectedAt = DateTime.UtcNow,
                        Metadata = new Dictionary<string, object>
                        {
                            { "AverageAge", Math.Round(frequentPatients.Average(p => p.Age), 1) },
                            { "AverageVisits", Math.Round(frequentPatients.Average(p => p.RecentVisits), 1) },
                            { "PatientCount", frequentPatients.Count }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting high visit frequency patterns");
            }

            return results;
        }

        private async Task<List<PatternDetectionResultDto>> DetectBloodTypeCorrelationsAsync()
        {
            var results = new List<PatternDetectionResultDto>();

            try
            {
                var bloodTypeStats = await _context.Users
                    .Where(u => !string.IsNullOrEmpty(u.BloodType))
                    .Include(u => u.PatientVisitRecords)
                    .Include(u => u.PatientAllergies)
                    .GroupBy(u => u.BloodType)
                    .Where(g => g.Count() >= 10) // At least 10 patients per blood type
                    .Select(g => new
                    {
                        BloodType = g.Key,
                        Count = g.Count(),
                        AvgVisits = g.Average(u => u.PatientVisitRecords.Count),
                        AvgAllergies = g.Average(u => u.PatientAllergies.Count),
                        AvgAge = g.Average(u => DateTime.Now.Year - u.DateOfBirth.Year)
                    })
                    .ToListAsync();

                if (bloodTypeStats.Count >= 2)
                {
                    // Find blood types with significantly higher visit rates
                    var avgVisits = bloodTypeStats.Average(x => x.AvgVisits);
                    var highVisitTypes = bloodTypeStats.Where(x => x.AvgVisits > avgVisits * 1.5).ToList();

                    if (highVisitTypes.Any())
                    {
                        results.Add(new PatternDetectionResultDto
                        {
                            PatternName = "Blood Type Visit Correlation",
                            PatternType = "BLOOD_TYPE_CORRELATION",
                            Description = $"Blood type(s) {string.Join(", ", highVisitTypes.Select(x => x.BloodType))} show higher visit frequency",
                            Severity = "MEDIUM",
                            ConfidenceScore = 0.7,
                            Recommendation = "Monitor patients with these blood types more closely",
                            DetectedAt = DateTime.UtcNow,
                            Metadata = new Dictionary<string, object>
                            {
                                { "HighVisitBloodTypes", highVisitTypes.Select(x => new { x.BloodType, x.AvgVisits }).ToList() },
                                { "OverallAverage", Math.Round(avgVisits, 2) }
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting blood type correlations");
            }

            return results;
        }

        private async Task<List<PatternDetectionResultDto>> DetectVaccinationGapsAsync()
        {
            var results = new List<PatternDetectionResultDto>();

            try
            {
                var unvaccinatedPatients = await _context.Users
                    .Include(u => u.PatientVaccinations)
                    .Where(u => u.PatientVaccinations.Count < 2 && DateTime.Now.Year - u.DateOfBirth.Year >= 18)
                    .CountAsync();

                if (unvaccinatedPatients >= 10)
                {
                    results.Add(new PatternDetectionResultDto
                    {
                        PatternName = "Vaccination Gap Pattern",
                        PatternType = "VACCINATION_GAP",
                        Description = $"Found {unvaccinatedPatients} adult patients with incomplete vaccination records",
                        Severity = unvaccinatedPatients > 50 ? "HIGH" : "MEDIUM",
                        ConfidenceScore = Math.Min(unvaccinatedPatients / 100.0, 1.0),
                        Recommendation = "Implement vaccination outreach programs, send reminders to patients",
                        DetectedAt = DateTime.UtcNow,
                        Metadata = new Dictionary<string, object>
                        {
                            { "UnvaccinatedCount", unvaccinatedPatients },
                            { "RecommendedAction", "Vaccination Campaign" }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting vaccination gaps");
            }

            return results;
        }

        private async Task<List<PatternDetectionResultDto>> DetectSeasonalPatternsAsync()
        {
            var results = new List<PatternDetectionResultDto>();

            try
            {
                var currentMonth = DateTime.Now.Month;
                var monthlyVisits = await _context.VisitRecords
                    .Where(v => v.VisitDate >= DateTime.UtcNow.AddYears(-1))
                    .GroupBy(v => v.VisitDate.Month)
                    .Select(g => new { Month = g.Key, Count = g.Count() })
                    .ToListAsync();

                if (monthlyVisits.Count >= 6) // At least 6 months of data
                {
                    var avgVisits = monthlyVisits.Average(x => x.Count);
                    var currentMonthVisits = monthlyVisits.FirstOrDefault(x => x.Month == currentMonth)?.Count ?? 0;

                    if (currentMonthVisits > avgVisits * 1.8) // 80% above average
                    {
                        results.Add(new PatternDetectionResultDto
                        {
                            PatternName = "Seasonal Visit Spike",
                            PatternType = "SEASONAL_PATTERN",
                            Description = $"Current month shows {Math.Round((currentMonthVisits / avgVisits - 1) * 100, 1)}% increase in visits compared to average",
                            Severity = "MEDIUM",
                            ConfidenceScore = 0.8,
                            Recommendation = "Prepare additional resources, investigate seasonal factors",
                            DetectedAt = DateTime.UtcNow,
                            Metadata = new Dictionary<string, object>
                            {
                                { "CurrentMonthVisits", currentMonthVisits },
                                { "AverageMonthlyVisits", Math.Round(avgVisits, 1) },
                                { "PercentageIncrease", Math.Round((currentMonthVisits / avgVisits - 1) * 100, 1) }
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting seasonal patterns");
            }

            return results;
        }

        private async Task<List<PatternDetectionResultDto>> DetectAllergyClustersAsync()
        {
            var results = new List<PatternDetectionResultDto>();

            try
            {
                var allergyStats = await _context.Allergies
                    .Include(a => a.Patient)
                    .GroupBy(a => a.AllergenName)
                    .Where(g => g.Count() >= 5)
                    .Select(g => new
                    {
                        Allergen = g.Key,
                        Count = g.Count(),
                        AverageAge = g.Average(a => DateTime.Now.Year - a.Patient.DateOfBirth.Year),
                        SevereCount = g.Count(a => a.Severity == AllergySeverity.Severe)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();

                if (allergyStats.Any())
                {
                    var topAllergen = allergyStats.First();
                    if (topAllergen.Count >= 20) // Significant cluster
                    {
                        results.Add(new PatternDetectionResultDto
                        {
                            PatternName = "Allergy Cluster Pattern",
                            PatternType = "ALLERGY_CLUSTER",
                            Description = $"High concentration of {topAllergen.Allergen} allergies ({topAllergen.Count} patients)",
                            Severity = topAllergen.SevereCount > 5 ? "HIGH" : "MEDIUM",
                            ConfidenceScore = Math.Min(topAllergen.Count / 50.0, 1.0),
                            Recommendation = "Investigate environmental factors, consider allergy prevention measures",
                            DetectedAt = DateTime.UtcNow,
                            Metadata = new Dictionary<string, object>
                            {
                                { "TopAllergen", topAllergen.Allergen },
                                { "AffectedPatients", topAllergen.Count },
                                { "SevereCases", topAllergen.SevereCount },
                                { "AverageAge", Math.Round(topAllergen.AverageAge, 1) }
                            }
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting allergy clusters");
            }

            return results;
        }

        private async Task<List<PatternDetectionResultDto>> DetectLabAnomaliesAsync()
        {
            var results = new List<PatternDetectionResultDto>();

            try
            {
                var recentAbnormalLabs = await _context.LabResults
                    .Where(lr => lr.TestDate >= DateTime.UtcNow.AddMonths(-3) && 
                                (lr.Status == "HIGH" || lr.Status == "LOW" || lr.Status == "CRITICAL"))
                    .Include(lr => lr.Patient)
                    .GroupBy(lr => lr.TestName)
                    .Where(g => g.Count() >= 10)
                    .Select(g => new
                    {
                        TestName = g.Key,
                        AbnormalCount = g.Count(),
                        CriticalCount = g.Count(lr => lr.Status == "CRITICAL"),
                        AverageAge = g.Average(lr => DateTime.Now.Year - lr.Patient.DateOfBirth.Year)
                    })
                    .OrderByDescending(x => x.AbnormalCount)
                    .ToListAsync();

                foreach (var labAnomaly in recentAbnormalLabs.Take(3)) // Top 3 anomalies
                {
                    results.Add(new PatternDetectionResultDto
                    {
                        PatternName = "Lab Result Anomaly Pattern",
                        PatternType = "LAB_ANOMALY",
                        Description = $"High rate of abnormal {labAnomaly.TestName} results ({labAnomaly.AbnormalCount} cases in 3 months)",
                        Severity = labAnomaly.CriticalCount > 5 ? "HIGH" : "MEDIUM",
                        ConfidenceScore = Math.Min(labAnomaly.AbnormalCount / 30.0, 1.0),
                        Recommendation = "Review lab procedures, investigate potential causes, consider additional testing",
                        DetectedAt = DateTime.UtcNow,
                        Metadata = new Dictionary<string, object>
                        {
                            { "TestName", labAnomaly.TestName },
                            { "AbnormalCount", labAnomaly.AbnormalCount },
                            { "CriticalCount", labAnomaly.CriticalCount },
                            { "AveragePatientAge", Math.Round(labAnomaly.AverageAge, 1) }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting lab anomalies");
            }

            return results;
        }

        private async Task<List<PatternDetectionResultDto>> DetectDiagnosisTrendsAsync()
        {
            var results = new List<PatternDetectionResultDto>();

            try
            {
                var recentDiagnoses = await _context.Diagnoses
                    .Where(d => d.DiagnosedDate >= DateTime.UtcNow.AddMonths(-6))
                    .Include(d => d.Patient)
                    .GroupBy(d => d.DiagnosisName)
                    .Where(g => g.Count() >= 5)
                    .Select(g => new
                    {
                        DiagnosisName = g.Key,
                        Count = g.Count(),
                        AverageAge = g.Average(d => DateTime.Now.Year - d.Patient.DateOfBirth.Year),
                        RecentTrend = g.Count(d => d.DiagnosedDate >= DateTime.UtcNow.AddMonths(-1))
                    })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();

                foreach (var diagnosis in recentDiagnoses.Take(3))
                {
                    results.Add(new PatternDetectionResultDto
                    {
                        PatternName = "Diagnosis Trend Pattern",
                        PatternType = "DIAGNOSIS_TREND",
                        Description = $"Increasing trend in {diagnosis.DiagnosisName} diagnoses ({diagnosis.Count} cases in 6 months)",
                        Severity = diagnosis.Count > 20 ? "HIGH" : "MEDIUM",
                        ConfidenceScore = Math.Min(diagnosis.Count / 25.0, 1.0),
                        Recommendation = "Monitor disease prevalence, consider preventive measures",
                        DetectedAt = DateTime.UtcNow,
                        Metadata = new Dictionary<string, object>
                        {
                            { "DiagnosisName", diagnosis.DiagnosisName },
                            { "TotalCases", diagnosis.Count },
                            { "RecentCases", diagnosis.RecentTrend },
                            { "AveragePatientAge", Math.Round(diagnosis.AverageAge, 1) }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting diagnosis trends");
            }

            return results;
        }

        private string GetRiskLevel(double riskScore)
        {
            if (riskScore >= 0.8) return "CRITICAL";
            if (riskScore >= 0.6) return "HIGH";
            if (riskScore >= 0.4) return "MEDIUM";
            if (riskScore >= 0.2) return "LOW";
            return "MINIMAL";
        }

        private List<string> GetRiskFactors(User patient, int age)
        {
            var factors = new List<string>();

            if (age > 70) factors.Add("Advanced age");
            var recentVisits = patient.PatientVisitRecords.Count(v => v.VisitDate >= DateTime.UtcNow.AddMonths(-6));
            if (recentVisits > 4) factors.Add("Frequent medical visits");
            if (patient.PatientAllergies.Any(a => a.Severity == AllergySeverity.Severe)) factors.Add("Severe allergies");
            if (patient.PatientVaccinations.Count < 2) factors.Add("Incomplete vaccinations");

            return factors;
        }

        private int PredictNextYearVisits(List<VisitRecord> visitHistory)
        {
            if (!visitHistory.Any()) return 0;

            var recentVisits = visitHistory.Where(v => v.VisitDate >= DateTime.UtcNow.AddYears(-1)).Count();
            var trend = visitHistory.Where(v => v.VisitDate >= DateTime.UtcNow.AddMonths(-6)).Count() * 2;

            return Math.Max(recentVisits, trend);
        }

        private List<string> GetRecommendedActions(User patient, double riskScore)
        {
            var actions = new List<string>();

            if (riskScore > 0.6) actions.Add("Schedule comprehensive health assessment");
            if (patient.PatientVaccinations.Count < 3) actions.Add("Update vaccination status");
            if (patient.PatientAllergies.Any(a => a.Severity == AllergySeverity.Severe)) actions.Add("Allergy management review");

            return actions;
        }

        private double CalculateHealthTrend(User patient)
        {
            var recentVisits = patient.PatientVisitRecords.Where(v => v.VisitDate >= DateTime.UtcNow.AddMonths(-6)).Count();
            var olderVisits = patient.PatientVisitRecords.Where(v => v.VisitDate >= DateTime.UtcNow.AddYears(-1) && v.VisitDate < DateTime.UtcNow.AddMonths(-6)).Count();

            if (olderVisits == 0) return 0.5; // Neutral trend

            return Math.Max(0, 1 - (double)recentVisits / olderVisits);
        }

        private List<string> GenerateRecommendations(List<PatternDetectionResultDto> patterns)
        {
            var recommendations = new List<string>();

            if (patterns.Any(p => p.PatternType == "VISIT_FREQUENCY"))
                recommendations.Add("Consider implementing telemedicine options for frequent visitors");

            if (patterns.Any(p => p.PatternType == "VACCINATION_GAP"))
                recommendations.Add("Launch vaccination awareness campaign");

            if (patterns.Any(p => p.PatternType == "SEASONAL_PATTERN"))
                recommendations.Add("Adjust staffing levels based on seasonal trends");

            return recommendations;
        }

        private double CalculateCorrelation(List<double> x, List<double> y)
        {
            if (x.Count != y.Count || x.Count < 2) return 0;

            var meanX = x.Average();
            var meanY = y.Average();

            var numerator = x.Zip(y, (xi, yi) => (xi - meanX) * (yi - meanY)).Sum();
            var denominator = Math.Sqrt(x.Sum(xi => Math.Pow(xi - meanX, 2)) * y.Sum(yi => Math.Pow(yi - meanY, 2)));

            return denominator == 0 ? 0 : numerator / denominator;
        }

        private string GetSignificanceLevel(double correlation)
        {
            if (Math.Abs(correlation) > 0.7) return "Strong";
            if (Math.Abs(correlation) > 0.4) return "Moderate";
            if (Math.Abs(correlation) > 0.2) return "Weak";
            return "Very Weak";
        }

        private string GenerateCorrelationInsight(string factor1, string factor2, double correlation)
        {
            var strength = GetSignificanceLevel(Math.Abs(correlation));
            var direction = correlation > 0 ? "positive" : "negative";
            return $"{strength} {direction} correlation between {factor1} and {factor2}";
        }

        private string GetSeason(int month)
        {
            return month switch
            {
                12 or 1 or 2 => "Winter",
                3 or 4 or 5 => "Spring",
                6 or 7 or 8 => "Summer",
                9 or 10 or 11 => "Fall",
                _ => "Unknown"
            };
        }

        private List<string> GetCommonSymptomsForMonth(int month)
        {
            return month switch
            {
                12 or 1 or 2 => new List<string> { "Cough", "Fever", "Cold symptoms" },
                3 or 4 or 5 => new List<string> { "Allergies", "Respiratory issues", "Skin problems" },
                6 or 7 or 8 => new List<string> { "Heat exhaustion", "Dehydration", "Skin conditions" },
                9 or 10 or 11 => new List<string> { "Flu symptoms", "Respiratory infections", "Joint pain" },
                _ => new List<string>()
            };
        }

        private double CalculateTrendStrength(int currentValue, double average)
        {
            if (average == 0) return 0;
            return Math.Abs(currentValue - average) / average;
        }

        #endregion
    }
}
