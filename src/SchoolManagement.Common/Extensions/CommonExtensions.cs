using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SchoolManagement.Common.Configuration;
using SchoolManagement.Common.Helpers;
using SchoolManagement.Common.Middleware;
using SchoolManagement.Common.Services;
using SchoolManagement.Common.Utilities;

namespace SchoolManagement.Common.Extensions;

public static class CommonExtensions
{
    public static IServiceCollection AddCommonServices(this IServiceCollection services)
    {
        services.AddScoped<IRequestContext, RequestContext>();
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddSingleton<IIpLocationResolver, IpLocationResolver>();
        services.AddSingleton<IFilesValidator, FilesValidator>();
        services.AddSingleton<IFilePathHelper, FilePathHelper>();
        return services;
    }

    public static IApplicationBuilder UseRequestContextMiddleware(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestContextMiddleware>();

    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();

    public static IApplicationBuilder UseEncryption(this IApplicationBuilder app) =>
        app.UseMiddleware<EncryptionMiddleware>();
}
