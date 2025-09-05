using System.ComponentModel.DataAnnotations;

namespace medical_be.Models;

/// <summary>
/// Visit Record - Doctor's notes for each patient visit
/// </summary>
public class VisitRecord
{
    public Guid Id { get; set; }

    [Required]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    public string DoctorId { get; set; } = string.Empty;
    
    [Required]
    public DateTime VisitDate { get; set; } = DateTime.UtcNow;
    
    [MaxLength(1000)]
    public string Symptoms { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(1000)]
    public string Diagnosis { get; set; } = string.Empty;
    
    [MaxLength(2000)]
    public string Treatment { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string Prescription { get; set; } = string.Empty;
    
    [MaxLength(2000)]
    public string Notes { get; set; } = string.Empty;
    
    public List<string> UploadedFiles { get; set; } = new(); // JSON array of file URLs
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual User Patient { get; set; } = null!;
    public virtual User Doctor { get; set; } = null!;
    
    [Required]
    public VisitType VisitType { get; set; } = VisitType.Consultation;
}

/// <summary>
/// Patient Vaccinations
/// </summary>
public class Vaccination
{
    public Guid Id { get; set; }
    
    [Required]
    public string PatientId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string VaccineName { get; set; } = string.Empty;
    
    public DateTime DateAdministered { get; set; }
    
    [MaxLength(100)]
    public string? BatchNumber { get; set; }
    
    [MaxLength(200)]
    public string? Manufacturer { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    public string? AdministeredById { get; set; } // Doctor ID
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual User Patient { get; set; } = null!;
    public virtual User? AdministeredBy { get; set; }
}

/// <summary>
/// Patient Allergies
/// </summary>
public class Allergy
{
    public Guid Id { get; set; }
    
    [Required]
    public string PatientId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(200)]
    public string AllergenName { get; set; } = string.Empty;
    
    [Required]
    public AllergySeverity Severity { get; set; }
    
    [MaxLength(500)]
    public string? Reaction { get; set; }
    
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    public bool IsActive { get; set; } = true; // Can be marked as resolved
    
    public DateTime DiagnosedDate { get; set; }
    public string? RecordedById { get; set; } // Doctor ID
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual User Patient { get; set; } = null!;
    public virtual User? RecordedBy { get; set; }
}

public enum AllergySeverity
{
    Mild = 1,
    Moderate = 2,
    Severe = 3,
    LifeThreatening = 4
}

/// <summary>
/// Medical Documents/Files
/// </summary>
public class MedicalDocument
{
    public Guid Id { get; set; }
    
    [Required]
    public string PatientId { get; set; } = string.Empty;
    
    public Guid? VisitRecordId { get; set; } // Optional - can be linked to a visit
    
    [Required]
    [MaxLength(255)]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string FileType { get; set; } = string.Empty; // PDF, JPG, PNG, etc.
    
    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty; // Storage path/URL
    
    public long FileSizeBytes { get; set; } // In bytes
    
    [MaxLength(200)]
    public string? Description { get; set; }
    
    [Required]
    public DocumentType DocumentType { get; set; }

    public string UploadedById { get; set; } = string.Empty; // Doctor ID

    [MaxLength(255)]
    public string StoredFileName { get; set; } = string.Empty;

    [MaxLength(150)]
    public string MimeType { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual User Patient { get; set; } = null!;
    public virtual User UploadedBy { get; set; } = null!;
    public virtual VisitRecord? VisitRecord { get; set; }
}

public enum DocumentType
{
    MedicalCertificate = 1,
    LabResults = 2,
    XRay = 3,
    MRI = 4,
    Prescription = 5,
    Referral = 6,
    Other = 7
}

public enum VisitType
{
    Consultation = 1,
    FollowUp = 2,
    Emergency = 3,
    RoutineCheck = 4
}
