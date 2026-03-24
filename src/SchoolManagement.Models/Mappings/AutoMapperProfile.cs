using AutoMapper;
using SchoolManagement.Models.DTOs.Master;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.Models.Mappings;

public sealed class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        // ── Outbound (Entity → Response DTO) ─────────────────────────────────

        // Country
        CreateMap<Country, CountryResponse>();

        // State — CountryName resolved via navigation property
        CreateMap<State, StateResponse>()
            .ForMember(dest => dest.CountryName, opt => opt.MapFrom(src => src.Country.Name));

        // City — resolved via navigation properties
        CreateMap<City, CityResponse>()
            .ForMember(dest => dest.StateName,   opt => opt.MapFrom(src => src.State.Name))
            .ForMember(dest => dest.CountryId,   opt => opt.MapFrom(src => src.State.CountryId))
            .ForMember(dest => dest.CountryName, opt => opt.MapFrom(src => src.State.Country.Name));

        // Organization
        CreateMap<Organization, OrganizationResponse>();

        // Menu / Page / Permission
        CreateMap<MenuMaster,            MenuResponse>();
        CreateMap<PageMaster,            PageResponse>();
        CreateMap<MenuAndPagePermission, MenuAndPagePermissionResponse>();

        // ── Inbound (Request DTO → Entity) ────────────────────────────────────
        // Used with _mapper.Map<TEntity>(request)   for creates
        //       and _mapper.Map(request, entity)    for updates

        // PageMaster — nav properties and Modules list (different element type) must be ignored
        CreateMap<CreatePageRequest, PageMaster>()
            .ForMember(dest => dest.Menu,        opt => opt.Ignore())
            .ForMember(dest => dest.Modules,     opt => opt.Ignore())
            .ForMember(dest => dest.Permissions, opt => opt.Ignore());

        CreateMap<UpdatePageRequest, PageMaster>()
            .ForMember(dest => dest.Menu,        opt => opt.Ignore())
            .ForMember(dest => dest.Modules,     opt => opt.Ignore())
            .ForMember(dest => dest.Permissions, opt => opt.Ignore());
    }
}
