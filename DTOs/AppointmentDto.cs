using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using medical_be.Models;

namespace medical_be.DTOs;

public class CreateAppointmentDto
{
    [Required]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    public string DoctorId { get; set; } = string.Empty;

    [Required]
    public DateTime AppointmentDate { get; set; }

    [JsonIgnore]
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(30);

    // Helper property for JSON serialization
    public int DurationMinutes 
    { 
        get => (int)Duration.TotalMinutes;
        set => Duration = TimeSpan.FromMinutes(value);
    }

    [MaxLength(500)]
    public string? Reason { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class UpdateAppointmentDto
{
    public DateTime? AppointmentDate { get; set; }
    public TimeSpan? Duration { get; set; }
    public AppointmentStatus? Status { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class AppointmentDto
{
    public int Id { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public TimeSpan Duration { get; set; }
    public AppointmentStatus Status { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
