using System.ComponentModel.DataAnnotations;

namespace medical_be.Models;

/// <summary>
/// Tracks when doctors access patient details for audit and transparency purposes
/// </summary>
public class PatientAccessLog
{
    public Guid Id { get; set; }

    [Required]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    public string DoctorId { get; set; } = string.Empty;

    [Required]
    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(200)]
    public string AccessType { get; set; } = string.Empty; // "ViewDetails", "ViewMedicalRecords", "ViewVisitHistory", etc.

    [MaxLength(500)]
    public string? AccessReason { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    [MaxLength(100)]
    public string? SessionId { get; set; }

    // Navigation properties
    public virtual User Patient { get; set; } = null!;
    public virtual User Doctor { get; set; } = null!;
}