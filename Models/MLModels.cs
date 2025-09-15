using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace medical_be.Models;

public class MedicalPattern
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string TriggerCondition { get; set; } = string.Empty; // JSON condition

    [Required]
    public string OutcomeCondition { get; set; } = string.Empty; // JSON condition

    public int MinimumCases { get; set; } = 10;

    public double ConfidenceThreshold { get; set; } = 0.7;

    public bool IsActive { get; set; } = true;

    // Audit fields
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CreatedBy))]
    public virtual User? Creator { get; set; }

    [ForeignKey(nameof(UpdatedBy))]
    public virtual User? Updater { get; set; }

    public virtual ICollection<PatternMatch> Matches { get; set; } = new List<PatternMatch>();
}

public class PatternMatch
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PatternId { get; set; }

    [Required]
    public string PatientId { get; set; } = string.Empty;

    public double ConfidenceScore { get; set; }

    public string MatchingData { get; set; } = string.Empty; // JSON

    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    public bool IsNotified { get; set; } = false;

    public DateTime? NotifiedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(PatternId))]
    public virtual MedicalPattern Pattern { get; set; } = null!;

    [ForeignKey(nameof(PatientId))]
    public virtual User Patient { get; set; } = null!;

    public virtual ICollection<MedicalAlert> Alerts { get; set; } = new List<MedicalAlert>();
}

public class MedicalAlert
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string PatientId { get; set; } = string.Empty;

    public int? PatternMatchId { get; set; }

    [Required]
    [StringLength(100)]
    public string AlertType { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Severity { get; set; } = string.Empty; // LOW, MEDIUM, HIGH, CRITICAL

    [Required]
    public string Message { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? RecommendedActions { get; set; }

    public int PatientCount { get; set; } = 0;

    public double ConfidenceScore { get; set; } = 0;

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReadAt { get; set; }

    public string? ReadBy { get; set; }

    // Navigation properties
    [ForeignKey(nameof(PatientId))]
    public virtual User Patient { get; set; } = null!;

    [ForeignKey(nameof(PatternMatchId))]
    public virtual PatternMatch? PatternMatch { get; set; }

    [ForeignKey(nameof(ReadBy))]
    public virtual User? Reader { get; set; }
}

public class LabResult
{
    [Key]
    public int Id { get; set; }

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
    [StringLength(20)]
    public string Status { get; set; } = string.Empty; // NORMAL, HIGH, LOW, CRITICAL

    [Required]
    public DateTime TestDate { get; set; }

    [StringLength(200)]
    public string? LabName { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(PatientId))]
    public virtual User Patient { get; set; } = null!;
}

public class Diagnosis
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string DiagnosisCode { get; set; } = string.Empty; // ICD-10

    [Required]
    [StringLength(200)]
    public string DiagnosisName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? Severity { get; set; }

    [StringLength(100)]
    public string? Category { get; set; } // Cancer, Cardiovascular, Diabetes, etc.

    [Required]
    public DateTime DiagnosedDate { get; set; }

    [Required]
    public string DoctorId { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime? ResolvedDate { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(PatientId))]
    public virtual User Patient { get; set; } = null!;

    [ForeignKey(nameof(DoctorId))]
    public virtual User Doctor { get; set; } = null!;
}
