using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace medical_be.Models;

public class User : IdentityUser
{
    [Required]
    [MaxLength(13)]
    public string IDNP { get; set; } = string.Empty; // Personal Identification Number
    
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(15)]
    public new string? PhoneNumber { get; set; }

    public DateTime DateOfBirth { get; set; }

    [Required]
    public Gender Gender { get; set; }
    
    [MaxLength(10)]
    public string? BloodType { get; set; } // A+, B-, O+, etc.

    [MaxLength(500)]
    public string? Address { get; set; }
    
    [MaxLength(100)]
    public string? ClinicId { get; set; } // For doctors
    
    public bool IsMFAEnabled { get; set; } = false;
    public string? MFASecret { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<Appointment> PatientAppointments { get; set; } = new List<Appointment>();
    public virtual ICollection<Appointment> DoctorAppointments { get; set; } = new List<Appointment>();
    public virtual ICollection<MedicalRecord> MedicalRecords { get; set; } = new List<MedicalRecord>();
    public virtual ICollection<VisitRecord> PatientVisitRecords { get; set; } = new List<VisitRecord>();
    public virtual ICollection<VisitRecord> DoctorVisitRecords { get; set; } = new List<VisitRecord>();
    public virtual ICollection<Vaccination> PatientVaccinations { get; set; } = new List<Vaccination>();
    public virtual ICollection<Vaccination> AdministeredVaccinations { get; set; } = new List<Vaccination>();
    public virtual ICollection<Allergy> PatientAllergies { get; set; } = new List<Allergy>();
    public virtual ICollection<Allergy> RecordedAllergies { get; set; } = new List<Allergy>();
}

public enum Gender
{
    Male = 1,
    Female = 2,
    Other = 3
}
