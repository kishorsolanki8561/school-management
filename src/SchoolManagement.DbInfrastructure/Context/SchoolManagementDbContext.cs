using Microsoft.EntityFrameworkCore;
using SchoolManagement.Common.Services;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure.Context;

public sealed class SchoolManagementDbContext : DbContext
{
    private readonly IRequestContext? _requestContext;

    public SchoolManagementDbContext(DbContextOptions<SchoolManagementDbContext> options,
        IRequestContext? requestContext = null)
        : base(options)
    {
        _requestContext = requestContext;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();
    public DbSet<UserRoleMapping> UserRoleMappings => Set<UserRoleMapping>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<UserOrganizationMapping> UserOrganizationMappings => Set<UserOrganizationMapping>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<State> States => Set<State>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<MenuMaster>                    MenuMasters                    => Set<MenuMaster>();
    public DbSet<PageMaster>                    PageMasters                    => Set<PageMaster>();
    public DbSet<PageMasterModule>              PageMasterModules               => Set<PageMasterModule>();
    public DbSet<PageMasterModuleActionMapping> PageMasterModuleActionMappings  => Set<PageMasterModuleActionMapping>();
    public DbSet<MenuAndPagePermission>         MenuAndPagePermissions          => Set<MenuAndPagePermission>();
    public DbSet<OrgFileUploadConfig>           OrgFileUploadConfigs            => Set<OrgFileUploadConfig>();
    public DbSet<SchoolApprovalRequest>         SchoolApprovalRequests          => Set<SchoolApprovalRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchoolManagementDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        StampAuditFields();
        return base.SaveChanges();
    }

    private void StampAuditFields()
    {
        var username = _requestContext?.Username ?? "System";
        var ip       = _requestContext?.IpAddress;
        var location = _requestContext?.Location;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case Microsoft.EntityFrameworkCore.EntityState.Added:
                    entry.Property(nameof(BaseEntity.CreatedBy)).CurrentValue = username;
                    entry.Property(nameof(BaseEntity.IpAddress)).CurrentValue = ip;
                    entry.Property(nameof(BaseEntity.   Location)).CurrentValue  = location;
                    break;

                case Microsoft.EntityFrameworkCore.EntityState.Modified:
                    entry.Entity.ModifiedAt = DateTime.UtcNow;
                    entry.Entity.ModifiedBy = username;
                    entry.Entity.IpAddress  = ip;
                    entry.Entity.Location   = location;

                    if (entry.Property(nameof(BaseEntity.IsDeleted)).IsModified
                        && entry.Entity.IsDeleted)
                    {
                        entry.Entity.DeletedBy = username;
                    }
                    break;
            }
        }
    }
}
