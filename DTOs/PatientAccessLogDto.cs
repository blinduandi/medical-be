using System.ComponentModel.DataAnnotations;

namespace medical_be.DTOs;

/// <summary>
/// DTO for patient access log information
/// </summary>
public class PatientAccessLogDto
{
    public Guid Id { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public DateTime AccessedAt { get; set; }
    public string AccessType { get; set; } = string.Empty;
    public string? AccessReason { get; set; }
    public string? IpAddress { get; set; }
}

/// <summary>
/// DTO for creating a patient access log entry
/// </summary>
public class CreatePatientAccessLogDto
{
    [Required]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string AccessType { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? AccessReason { get; set; }
}

/// <summary>
/// DTO for querying patient access logs
/// </summary>
public class PatientAccessLogQueryDto
{
    public string? PatientId { get; set; }
    public string? DoctorId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? AccessType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// DTO for patient access log summary
/// </summary>
public class PatientAccessLogSummaryDto
{
    public int TotalAccesses { get; set; }
    public int UniqueDoctors { get; set; }
    public DateTime? LastAccess { get; set; }
    public string? LastAccessedBy { get; set; }
    public Dictionary<string, int> AccessTypeCount { get; set; } = new();
    public List<PatientAccessLogDto> RecentAccesses { get; set; } = new();
}