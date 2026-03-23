using AutoMapper;
using SchoolManagement.Models.DTOs.Master;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.Models.Mappings;

public sealed class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        // Country
        CreateMap<Country, CountryResponse>();

        // State — CountryName is resolved via navigation property State.Country.Name
        CreateMap<State, StateResponse>()
            .ForMember(dest => dest.CountryName, opt => opt.MapFrom(src => src.Country.Name));

        // City — StateName and CountryName resolved via navigation properties
        CreateMap<City, CityResponse>()
            .ForMember(dest => dest.StateName,   opt => opt.MapFrom(src => src.State.Name))
            .ForMember(dest => dest.CountryId,   opt => opt.MapFrom(src => src.State.CountryId))
            .ForMember(dest => dest.CountryName, opt => opt.MapFrom(src => src.State.Country.Name));
    }
}
