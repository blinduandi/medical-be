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
    }

    public class DoctorUpdateDto
    {
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string ClinicId { get; set; }
        public bool? IsActive { get; set; }
    }
}
