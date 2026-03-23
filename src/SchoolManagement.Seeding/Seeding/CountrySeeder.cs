using Microsoft.EntityFrameworkCore;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.Seeding.Seeding;

public sealed class CountrySeeder : ISeeder
{
    private readonly SchoolManagementDbContext _context;

    public CountrySeeder(SchoolManagementDbContext context)
        => _context = context;

    public async Task<bool> IsSeededAsync(CancellationToken cancellationToken = default)
        => await _context.Countries.AnyAsync(cancellationToken);

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var countries = new List<Country>
        {
            new() { Name = "India",          Code = "IND" },
            new() { Name = "United States",  Code = "USA" },
            new() { Name = "United Kingdom", Code = "GBR" },
            new() { Name = "Pakistan",       Code = "PAK" },
            new() { Name = "Canada",         Code = "CAN" },
            new() { Name = "Australia",      Code = "AUS" },
            new() { Name = "UAE",            Code = "ARE" },
            new() { Name = "Germany",        Code = "DEU" },
            new() { Name = "France",         Code = "FRA" },
            new() { Name = "Singapore",      Code = "SGP" },
        };

        await _context.Countries.AddRangeAsync(countries, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
