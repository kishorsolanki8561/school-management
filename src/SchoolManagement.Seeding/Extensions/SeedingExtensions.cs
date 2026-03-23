using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SchoolManagement.Seeding.Seeding;

namespace SchoolManagement.Seeding.Extensions;

public static class SeedingExtensions
{
    public static IServiceCollection AddSeeding(this IServiceCollection services)
    {
        services.AddScoped<ISeeder, RoleSeeder>();
        services.AddScoped<ISeeder, UserSeeder>();
        services.AddScoped<ISeeder, CountrySeeder>();
        services.AddScoped<DatabaseSeeder>();
        return services;
    }

    public static async Task SeedDatabaseAsync(this IHost app,
        CancellationToken cancellationToken = default)
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await seeder.SeedAllAsync(cancellationToken);
    }
}
