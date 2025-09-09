namespace medical_be.DTOs;

// Visit Record DTOs
public class VisitRecordDto
{
    public Guid Id { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public string DoctorId { get; set; } = string.Empty;
    public DateTime VisitDate { get; set; }
    public string Symptoms { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string Treatment { get; set; } = string.Empty;
    public string Prescription { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string VisitType { get; set; } = string.Empty;
    public List<string> UploadedFiles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Populated fields
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
}

public class CreateVisitRecordDto
{
    public DateTime VisitDate { get; set; }
    public string Diagnosis { get; set; } = string.Empty;
    public string Treatment { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string VisitType { get; set; } = string.Empty;
}

public class UpdateVisitRecordDto
{
    public string Symptoms { get; set; } = string.Empty;
    public string Diagnosis { get; set; } = string.Empty;
    public string Treatment { get; set; } = string.Empty;
    public string Prescription { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

// Vaccination DTOs
public class VaccinationDto
{
    public Guid Id { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public string VaccineName { get; set; } = string.Empty;
    public DateTime DateAdministered { get; set; }
    public string? BatchNumber { get; set; }
    public string? Manufacturer { get; set; }
    public string? Notes { get; set; }
    public string? AdministeredById { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Populated fields
    public string? DoctorName { get; set; }
}

public class CreateVaccinationDto
{
    public Guid PatientId { get; set; }
    public string VaccineName { get; set; } = string.Empty;
    public DateTime DateAdministered { get; set; }
    public string? BatchNumber { get; set; }
    public string? Manufacturer { get; set; }
    public string? Notes { get; set; }
}

public class UpdateVaccinationDto
{
    public string VaccineName { get; set; } = string.Empty;
    public DateTime DateAdministered { get; set; }
    public string? BatchNumber { get; set; }
    public string? Manufacturer { get; set; }
    public string? Notes { get; set; }
}

// Allergy DTOs
public class AllergyDto
{
    public Guid Id { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public string AllergenName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? Reaction { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
    public DateTime DiagnosedDate { get; set; }
    public string? RecordedById { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Populated fields
    public string? DoctorName { get; set; }
}

public class CreateAllergyDto
{
    public string AllergenName { get; set; } = string.Empty;
    public string AllergyType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? Reaction { get; set; }
    public string? Notes { get; set; }
}

public class UpdateAllergyDto
{
    public string AllergenName { get; set; } = string.Empty;
    public int Severity { get; set; }
    public string? Reaction { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}

// Patient Search DTOs
public class PatientSearchDto
{
    public string? IDNP { get; set; }
    public string? Name { get; set; }
    public DateTime? DateOfBirth { get; set; }
}

public class DoctorSearchDto
{
    public string? IDNP { get; set; }
    public string? Name { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? ClinicId { get; set; }
    public int TotalPatients { get; set; }
    public DateTime? LastActivity { get; set; }
}

public class PatientSummaryDto
{
    public Guid Id { get; set; }
    public string IDNP { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? BloodType { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime LastVisit { get; set; }
    public int TotalVisits { get; set; }
    public List<string> ActiveAllergies { get; set; } = new();
}

// Medical Document DTOs
public class MedicalDocumentDto
{
    public Guid Id { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public Guid? VisitRecordId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string? Description { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string UploadedById { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Populated fields
    public string UploadedByName { get; set; } = string.Empty;
}

// Extended User DTOs
public class PatientProfileDto : UserDto
{
    public string IDNP { get; set; } = string.Empty;
    public string? BloodType { get; set; }
    public List<AllergyDto> ActiveAllergies { get; set; } = new();
    public List<VaccinationDto> RecentVaccinations { get; set; } = new();
    public DateTime? LastVisit { get; set; }
    public int TotalVisits { get; set; }
}

public class DoctorProfileDto : UserDto
{
    public string IDNP { get; set; } = string.Empty;
    public string? ClinicId { get; set; }
    public int TotalPatients { get; set; }
    public DateTime? LastActivity { get; set; }
}

// Update Document DTOs
public class UpdateDocumentDTO
{
    public string Description { get; set; } = string.Empty;
}

public class UploadDocumentDTO
{
    public string PatientId { get; set; } = string.Empty;
    public IFormFile File { get; set; } = null!;
    public string DocumentType { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// Admin User Management DTOs
public class CreateUserDTO
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string IDNP { get; set; } = string.Empty;
    public string? BloodType { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? Role { get; set; }
}

public class UpdateUserDTO
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? IDNP { get; set; }
    public string? BloodType { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public bool? IsActive { get; set; }
}

public class AssignRoleDTO
{
    public string RoleName { get; set; } = string.Empty;
}
