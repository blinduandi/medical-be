using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace medical_be.Models;

public class MedicalRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    public string DoctorId { get; set; } = string.Empty;

    public int? AppointmentId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Diagnosis { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Symptoms { get; set; }

    [MaxLength(1000)]
    public string? Treatment { get; set; }

    [MaxLength(1000)]
    public string? Prescription { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public DateTime RecordDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    [ForeignKey("PatientId")]
    public virtual User Patient { get; set; } = null!;

    [ForeignKey("DoctorId")]
    public virtual User Doctor { get; set; } = null!;

    [ForeignKey("AppointmentId")]
    public virtual Appointment? Appointment { get; set; }
}
