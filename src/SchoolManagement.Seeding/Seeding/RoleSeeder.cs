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

    // Always return false so the upsert runs on every startup,
    // keeping the Roles table in sync with the code definition.
    public Task<bool> IsSeededAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var defined = GetDefinedRoles();

        // Load tracked entities keyed by Id — change tracker will detect mutations
        var existing = await _context.Roles
            .IgnoreQueryFilters()
            .ToDictionaryAsync(r => r.Id, cancellationToken);

        // UPDATE first — frees old Name values before new inserts reuse them.
        // Save one entity at a time so EF never sees multiple Modified rows
        // sharing the unique index on Name (avoids circular dependency error).
        foreach (var role in defined.Where(r => existing.TryGetValue(r.Id, out var db) && HasChanged(r, db)))
        {
            var db = existing[role.Id];
            db.Name        = role.Name;
            db.Description = role.Description;
            db.IsOrgRole   = role.IsOrgRole;
            await _context.SaveChangesAsync(cancellationToken);
        }

        // INSERT new roles after updates so no name conflict can occur
        var toInsert = defined.Where(r => !existing.ContainsKey(r.Id)).ToList();
        if (toInsert.Count > 0)
        {
            await _context.Roles.AddRangeAsync(toInsert, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // Auto-seed MenuAndPagePermissions (IsAllowed=false) for every
            // existing page-module-action mapping so the new role is ready to configure.
            await SeedPermissionsForNewRolesAsync(
                toInsert.Select(r => r.Id).ToList(), cancellationToken);
        }
    }

    private async Task SeedPermissionsForNewRolesAsync(
        IList<int> newRoleIds, CancellationToken cancellationToken)
    {
        // Load all active actio    n mappings together with their page's MenuId
        var mappings = await (
            from m in _context.PageMasterModuleActionMappings
            join p in _context.PageMasters on m.PageId equals p.Id
            select new { m.PageId, m.PageModuleId, m.ActionId, p.MenuId }
        ).ToListAsync(cancellationToken);

        if (mappings.Count == 0) return;   // no pages configured yet — nothing to seed

        // Idempotency: load any permissions that already exist for these roles
        var existingKeys = (await _context.MenuAndPagePermissions
            .Where(p => newRoleIds.Contains(p.RoleId))
            .Select(p => new { p.RoleId, p.PageId, p.PageModuleId, p.ActionId })
            .ToListAsync(cancellationToken))
            .Select(x => (x.RoleId, x.PageId, x.PageModuleId, x.ActionId))
            .ToHashSet();

        var newPerms = new List<MenuAndPagePermission>();

        foreach (var roleId in newRoleIds)
        {
            foreach (var m in mappings)
            {
                // HashSet.Add returns false when the key is already present
                if (existingKeys.Add((roleId, m.PageId, m.PageModuleId, m.ActionId)))
                {
                    newPerms.Add(new MenuAndPagePermission
                    {
                        MenuId       = m.MenuId,
                        PageId       = m.PageId,
                        PageModuleId = m.PageModuleId,
                        ActionId     = m.ActionId,
                        RoleId       = roleId,
                        IsAllowed    = false,
                    });
                }
            }
        }

        if (newPerms.Count > 0)
        {
            await _context.MenuAndPagePermissions.AddRangeAsync(newPerms, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private static bool HasChanged(Role defined, Role db)
        => defined.Name        != db.Name
        || defined.Description != db.Description
        || defined.IsOrgRole   != db.IsOrgRole;

    private static List<Role> GetDefinedRoles() =>
        new List<Role>
        {
        // ── System / admin roles (IsOrgRole = false) ─────────────────────────
        new() { Id = (int)UserRole.OwnerAdmin,          Name = "Owner Admin",          Description = "Organisation owner — full system access",     IsOrgRole = false },
        new() { Id = (int)UserRole.SuperAdmin,          Name = "Super Admin",          Description = "Super administrator — manages all schools",   IsOrgRole = false },
        new() { Id = (int)UserRole.Admin,               Name = "Admin",                Description = "School-level administrator",                  IsOrgRole = false },

        // ── Academic roles (IsOrgRole = true) ────────────────────────────────
        new() { Id = (int)UserRole.Teacher,             Name = "Teacher",              Description = "Class teacher",                               IsOrgRole = true },
        new() { Id = (int)UserRole.Student,             Name = "Student",              Description = "Enrolled student",                            IsOrgRole = true },
        new() { Id = (int)UserRole.HeadTeacher,         Name = "Head Teacher",         Description = "Head of teaching staff",                      IsOrgRole = true },
        new() { Id = (int)UserRole.Principal,           Name = "Principal",            Description = "School principal",                            IsOrgRole = true },
        new() { Id = (int)UserRole.VicePrincipal,       Name = "Vice Principal",       Description = "Deputy principal",                            IsOrgRole = true },
        new() { Id = (int)UserRole.Coordinator,         Name = "Coordinator",          Description = "Academic or activity coordinator",             IsOrgRole = true },

        // ── Parent / guardian roles ───────────────────────────────────────────
        new() { Id = (int)UserRole.Parent,              Name = "Parent",               Description = "Parent of an enrolled student",               IsOrgRole = true },
        new() { Id = (int)UserRole.Guardian,            Name = "Guardian",             Description = "Legal guardian of an enrolled student",       IsOrgRole = true },

        // ── Administrative / office roles ─────────────────────────────────────
        new() { Id = (int)UserRole.SchoolAdministrator, Name = "School Administrator", Description = "School administration manager",               IsOrgRole = true },
        new() { Id = (int)UserRole.OfficeStaff,         Name = "Office Staff",         Description = "General office staff member",                 IsOrgRole = true },
        new() { Id = (int)UserRole.Clerk,               Name = "Clerk",                Description = "Administrative clerk",                        IsOrgRole = true },
        new() { Id = (int)UserRole.Accountant,          Name = "Accountant",           Description = "School accountant",                           IsOrgRole = true },
        new() { Id = (int)UserRole.Librarian,           Name = "Librarian",            Description = "Library manager",                             IsOrgRole = true },
        new() { Id = (int)UserRole.LabAssistant,        Name = "Lab Assistant",        Description = "Laboratory assistant",                        IsOrgRole = true },
        new() { Id = (int)UserRole.ITStaff,             Name = "IT Staff",             Description = "IT support staff",                            IsOrgRole = true },
        new() { Id = (int)UserRole.Receptionist,        Name = "Receptionist",         Description = "Front-desk receptionist",                     IsOrgRole = true },
        new() { Id = (int)UserRole.Counselor,           Name = "Counselor",            Description = "Student counselor",                           IsOrgRole = true },
        new() { Id = (int)UserRole.SpecialEducator,     Name = "Special Educator",     Description = "Special needs educator",                      IsOrgRole = true },

        // ── Health roles ──────────────────────────────────────────────────────
        new() { Id = (int)UserRole.Nurse,               Name = "Nurse",                Description = "School nurse",                                IsOrgRole = true },
        new() { Id = (int)UserRole.MedicalStaff,        Name = "Medical Staff",        Description = "Medical support staff",                       IsOrgRole = true },

        // ── Support / operations roles ────────────────────────────────────────
        new() { Id = (int)UserRole.Driver,              Name = "Driver",               Description = "School transport driver",                     IsOrgRole = true },
        new() { Id = (int)UserRole.Conductor,           Name = "Conductor",            Description = "School bus conductor",                        IsOrgRole = true },
        new() { Id = (int)UserRole.Attendant,           Name = "Attendant",            Description = "Student attendant",                           IsOrgRole = true },
        new() { Id = (int)UserRole.SecurityGuard,       Name = "Security Guard",       Description = "Campus security guard",                       IsOrgRole = true },
        new() { Id = (int)UserRole.Cleaner,             Name = "Cleaner",              Description = "Cleaning staff",                              IsOrgRole = true },
        new() { Id = (int)UserRole.MaintenanceStaff,    Name = "Maintenance Staff",    Description = "Campus maintenance staff",                    IsOrgRole = true },
        };
}
