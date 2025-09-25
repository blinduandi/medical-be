using AutoMapper;
using medical_be.DTOs;
using medical_be.Models;

namespace medical_be.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User -> PatientProfileDto
        CreateMap<User, PatientProfileDto>()
            .ForMember(d => d.ActiveAllergies, opt => opt.Ignore())
            .ForMember(d => d.RecentVaccinations, opt => opt.Ignore())
            .ForMember(d => d.LastVisit, opt => opt.Ignore())
            .ForMember(d => d.TotalVisits, opt => opt.Ignore());

        // VisitRecord -> VisitRecordDto
        CreateMap<VisitRecord, VisitRecordDto>()
            .ForMember(d => d.VisitType, opt => opt.MapFrom(s => s.VisitType.ToString()))
            .ForMember(d => d.PatientName, opt => opt.Ignore())
            .ForMember(d => d.DoctorName, opt => opt.Ignore());

        // Vaccination -> VaccinationDto
        CreateMap<Vaccination, VaccinationDto>()
            .ForMember(d => d.DoctorName, opt => opt.MapFrom(s => s.AdministeredBy != null ? (s.AdministeredBy.FirstName + " " + s.AdministeredBy.LastName) : null));

        // Allergy -> AllergyDto
        CreateMap<Allergy, AllergyDto>()
            .ForMember(d => d.Severity, opt => opt.MapFrom(s => s.Severity.ToString()));

        // Appointment -> AppointmentDto
        CreateMap<Appointment, AppointmentDto>()
            .ForMember(d => d.PatientName, opt => opt.Ignore())
            .ForMember(d => d.DoctorName, opt => opt.Ignore());

        // PatientDoctor -> PatientDoctorDto (show specialty as string)
        CreateMap<PatientDoctor, PatientDoctorDto>()
            .ForMember(d => d.DoctorName, opt => opt.MapFrom(s => s.Doctor.FirstName + " " + s.Doctor.LastName))
            .ForMember(d => d.DoctorEmail, opt => opt.MapFrom(s => s.Doctor.Email ?? ""))
            .ForMember(d => d.DoctorPhoneNumber, opt => opt.MapFrom(s => s.Doctor.PhoneNumber))
            .ForMember(d => d.ClinicId, opt => opt.MapFrom(s => s.Doctor.ClinicId))
            .ForMember(d => d.Specialty, opt => opt.MapFrom(s => s.Doctor.Specialty.ToString()))
            .ForMember(d => d.Experience, opt => opt.MapFrom(s => s.Doctor.Experience));

        // PatientDoctor -> DoctorPatientDto
        CreateMap<PatientDoctor, DoctorPatientDto>()
            .ForMember(d => d.PatientName, opt => opt.MapFrom(s => s.Patient.FirstName + " " + s.Patient.LastName))
            .ForMember(d => d.PatientEmail, opt => opt.MapFrom(s => s.Patient.Email ?? ""))
            .ForMember(d => d.PatientPhoneNumber, opt => opt.MapFrom(s => s.Patient.PhoneNumber))
            .ForMember(d => d.PatientIDNP, opt => opt.MapFrom(s => s.Patient.IDNP))
            .ForMember(d => d.BloodType, opt => opt.MapFrom(s => s.Patient.BloodType))
            .ForMember(d => d.DateOfBirth, opt => opt.MapFrom(s => s.Patient.DateOfBirth))
            .ForMember(d => d.LastVisit, opt => opt.Ignore())
            .ForMember(d => d.TotalVisits, opt => opt.Ignore());

        // User -> AvailableDoctorDto (for doctor selection, show specialty as string)
        CreateMap<User, AvailableDoctorDto>()
            .ForMember(d => d.Name, opt => opt.MapFrom(s => s.FirstName + " " + s.LastName))
            .ForMember(d => d.Email, opt => opt.MapFrom(s => s.Email ?? ""))
            .ForMember(d => d.Specialty, opt => opt.MapFrom(s => s.Specialty.ToString()))
            .ForMember(d => d.IsAlreadyAssigned, opt => opt.Ignore());

        // PatientAccessLog -> PatientAccessLogDto
        CreateMap<PatientAccessLog, PatientAccessLogDto>()
            .ForMember(d => d.PatientName, opt => opt.MapFrom(s => s.Patient.FirstName + " " + s.Patient.LastName))
            .ForMember(d => d.DoctorName, opt => opt.MapFrom(s => s.Doctor.FirstName + " " + s.Doctor.LastName));
    }
}
