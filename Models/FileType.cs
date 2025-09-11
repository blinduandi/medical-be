using System.ComponentModel.DataAnnotations;

namespace medical_be.Models;

public class FileType
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(100)]
    public string Category { get; set; } = string.Empty; // "medical_scan", "profile_photo", "lab_result", "document"

    [Required]
    public string AllowedExtensions { get; set; } = string.Empty; // JSON array: ["jpg","jpeg","png","pdf"]

    public long MaxSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB default

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<MedicalFile> Files { get; set; } = new List<MedicalFile>();
}

public enum FileCategory
{
    ProfilePhoto,
    MedicalScan,
    LabResult,
    Document,
    Prescription,
    XRay,
    MRI,
    CTScan,
    Ultrasound,
    Other
}
