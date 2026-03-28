using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagement.Common.Configuration;
using SchoolManagement.Common.Constants;
using SchoolManagement.Common.Helpers;
using SchoolManagement.Common.Services;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.DbInfrastructure.Repositories;
using SchoolManagement.DbInfrastructure.Repositories.Implementations;
using SchoolManagement.DbInfrastructure.Repositories.Interfaces;

namespace SchoolManagement.DbInfrastructure.Extensions;

public static class DbInfrastructureExtensions
{
    public static IServiceCollection AddDbInfrastructure(this IServiceCollection services)
    {
        var connectionString  =AppConfigFactory.Configuration?
            .GetSection("ConnectionStrings:DefaultConnection")?.Value
            ?? throw new InvalidOperationException(AppMessages.General.ConnectionStringMissing);
        //var connectionString = InitializeConfiguration.ConnectionString;
        var dbSettings       = InitializeConfiguration.DatabaseSettings;

        // Register EF Core DbContext with AuditInterceptor
        services.AddDbContext<SchoolManagementDbContext>((sp, options) =>
        {
            var requestContext = sp.GetRequiredService<IRequestContext>();
            var interceptor = new AuditInterceptor(requestContext);

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.CommandTimeout(dbSettings.CommandTimeout);
                sqlOptions.EnableRetryOnFailure(3);
            })
            .AddInterceptors(interceptor);

            if (dbSettings.EnableSensitiveDataLogging)
                options.EnableSensitiveDataLogging();
        });

        // Repositories
        services.AddScoped(typeof(IWriteRepository<>), typeof(WriteRepository<>));
        services.AddScoped<IReadRepository, ReadRepository>();

        // Dapper helper (self-manages connections via AppConfigFactory)
        services.AddScoped<IDapperHelper, DapperHelper>();

        // Dapper write helper with built-in audit support
        services.AddScoped<IDapperAuditExecutor, DapperAuditExecutor>();

        // ErrorLogService (needs DbContext)
        services.AddScoped<IErrorLogService, ErrorLogService>();

        return services;
    }
}
