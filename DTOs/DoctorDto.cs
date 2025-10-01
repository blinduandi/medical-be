using medical_be.Models;

namespace medical_be.DTOs
{
    public class DoctorCreateDto
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public Gender Gender { get; set; }
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string IDNP { get; set; } = null!;
        public DateTime DateOfBirth { get; set; }
        public string Address { get; set; } = null!;
        public string ClinicId { get; set; } = null!;
        public DoctorSpecialty Specialty { get; set; } = DoctorSpecialty.GeneralPractice;
        public string Experience { get; set; } = null!;
    }

    public class DoctorUpdateDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? ClinicId { get; set; }
        public DoctorSpecialty? Specialty { get; set; }
        public string? Experience { get; set; }
        public bool? IsActive { get; set; }
    }
}
