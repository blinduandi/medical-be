using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace medical_be.Models;

/// <summary>
/// Represents the many-to-many relationship between patients and doctors
/// Allows patients to select their doctors and doctors to view their assigned patients
/// </summary>
public class PatientDoctor
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    public string DoctorId { get; set; } = string.Empty;

    /// <summary>
    /// Date when the patient was assigned to the doctor
    /// </summary>
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates if the relationship is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Optional notes about the patient-doctor relationship
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Date when the relationship was deactivated (if applicable)
    /// </summary>
    public DateTime? DeactivatedDate { get; set; }

    /// <summary>
    /// Who assigned this patient to the doctor (Admin, Patient self-assignment, etc.)
    /// </summary>
    [MaxLength(50)]
    public string AssignedBy { get; set; } = "Patient"; // "Patient", "Admin", "Doctor"

    // Navigation properties
    [ForeignKey("PatientId")]
    public virtual User Patient { get; set; } = null!;

    [ForeignKey("DoctorId")]
    public virtual User Doctor { get; set; } = null!;
}