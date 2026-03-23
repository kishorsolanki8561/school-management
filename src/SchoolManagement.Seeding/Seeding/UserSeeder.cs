using Microsoft.EntityFrameworkCore;
using SchoolManagement.Common.Utilities;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.Models.Entities;
using SchoolManagement.Models.Enums;

namespace SchoolManagement.Seeding.Seeding;

public sealed class UserSeeder : ISeeder
{
    private readonly SchoolManagementDbContext _context;

    public UserSeeder(SchoolManagementDbContext context)
        => _context = context;

    public async Task<bool> IsSeededAsync(CancellationToken cancellationToken = default)
        => await _context.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin, cancellationToken);

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var admin = new User
        {
            Username = "superadmin",
            Email    = "kishorsolanki2012@gmail.com",
            PasswordHash = HashingUtility.HashPassword("phalodi@123"),
            Role     = UserRole.SuperAdmin
        };

        await _context.Users.AddAsync(admin, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
