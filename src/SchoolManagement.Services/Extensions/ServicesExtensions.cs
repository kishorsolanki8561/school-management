using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Services.Implementations;
using SchoolManagement.Services.Interfaces;

namespace SchoolManagement.Services.Extensions;

public static class ServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<ICountryService, CountryService>();
        services.AddScoped<IStateService, StateService>();
        services.AddScoped<ICityService, CityService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        return services;
    }
}
