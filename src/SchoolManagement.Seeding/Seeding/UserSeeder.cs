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
        => await _context.Users.AnyAsync(u => u.IsAdmin, cancellationToken);

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var admin = new User
        {
            Username     = "OwnerAdmin",
            Email        = "kishorsolanki2012@gmail.com",
            PasswordHash = HashingUtility.HashPassword("phalodi@123"),
            IsAdmin      = true
        };

        await _context.Users.AddAsync(admin, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Assign "Super Admin" role (Id=2) via UserRoleMapping
        var mapping = new UserRoleMapping
        {
            UserId = admin.Id,
            RoleId = (int)UserRole.OwnerAdmin
        };
        await _context.UserRoleMappings.AddAsync(mapping, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
