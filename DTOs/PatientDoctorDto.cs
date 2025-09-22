using System.ComponentModel.DataAnnotations;

namespace medical_be.DTOs;

/// <summary>
/// DTO for adding a doctor to a patient's list
/// </summary>
public class AddDoctorToPatientDto
{
    [Required]
    public string DoctorId { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for patient's doctor relationship information
/// </summary>
public class PatientDoctorDto
{
    public int Id { get; set; }
    public string DoctorId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string DoctorEmail { get; set; } = string.Empty;
    public string? DoctorPhoneNumber { get; set; }
    public string? ClinicId { get; set; }
    public string? Specialty { get; set; }
    public string? Experience { get; set; }
    public DateTime AssignedDate { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public string AssignedBy { get; set; } = string.Empty;
}

/// <summary>
/// DTO for doctor's patient relationship information
/// </summary>
public class DoctorPatientDto
{
    public int Id { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string PatientEmail { get; set; } = string.Empty;
    public string? PatientPhoneNumber { get; set; }
    public string PatientIDNP { get; set; } = string.Empty;
    public string? BloodType { get; set; }
    public DateTime DateOfBirth { get; set; }
    public DateTime AssignedDate { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public string AssignedBy { get; set; } = string.Empty;
    public DateTime? LastVisit { get; set; }
    public int TotalVisits { get; set; }
}

/// <summary>
/// DTO for removing a doctor from patient's list
/// </summary>
public class RemoveDoctorFromPatientDto
{
    [Required]
    public string DoctorId { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Reason { get; set; }
}

/// <summary>
/// DTO for updating patient-doctor relationship
/// </summary>
public class UpdatePatientDoctorDto
{
    [MaxLength(500)]
    public string? Notes { get; set; }
    
    public bool? IsActive { get; set; }
}

/// <summary>
/// DTO for available doctors list (simplified for selection)
/// </summary>
public class AvailableDoctorDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ClinicId { get; set; }
    public string? Specialty { get; set; }
    public string? Experience { get; set; }
    public string IDNP { get; set; } = string.Empty;
    public bool IsAlreadyAssigned { get; set; }
}

/// <summary>
/// DTO for admin assigning a patient to a doctor
/// </summary>
public class AdminAssignPatientDoctorDto
{
    [Required]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    public string DoctorId { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for admin removing a patient-doctor relationship
/// </summary>
public class AdminRemovePatientDoctorDto
{
    [Required]
    public int RelationshipId { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}