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
    }
}
