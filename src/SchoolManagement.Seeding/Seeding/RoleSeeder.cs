using Microsoft.EntityFrameworkCore;
using SchoolManagement.DbInfrastructure.Context;
using SchoolManagement.Models.Entities;
using SchoolManagement.Models.Enums;

namespace SchoolManagement.Seeding.Seeding;

public sealed class RoleSeeder : ISeeder
{
    private readonly SchoolManagementDbContext _context;

    public RoleSeeder(SchoolManagementDbContext context)
        => _context = context;

    public async Task<bool> IsSeededAsync(CancellationToken cancellationToken = default)
        => await _context.Roles.AnyAsync(cancellationToken);

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var roles = new List<Role>
        {
            new() { Id = (int)UserRole.SuperAdmin,  Name = nameof(UserRole.SuperAdmin),  Description = "Full system access" },
            new() { Id = (int)UserRole.SchoolAdmin, Name = nameof(UserRole.SchoolAdmin), Description = "School-level administration" },
            new() { Id = (int)UserRole.Supervisor,  Name = nameof(UserRole.Supervisor),  Description = "Supervisory oversight" },
            new() { Id = (int)UserRole.Teacher,     Name = nameof(UserRole.Teacher),     Description = "Teaching staff" },
            new() { Id = (int)UserRole.Student,     Name = nameof(UserRole.Student),     Description = "Enrolled student" },
        };

        await _context.Roles.AddRangeAsync(roles, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
