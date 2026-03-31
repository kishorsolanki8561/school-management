using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure.Configurations;

internal sealed class MenuAndPagePermissionConfiguration : IEntityTypeConfiguration<MenuAndPagePermission>
{
    public void Configure(EntityTypeBuilder<MenuAndPagePermission> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.ActionId).HasConversion<int>();

        // Unique per org — null OrgId = system template, non-null = org-specific copy
        builder.HasIndex(p => new { p.MenuId, p.PageId, p.PageModuleId, p.ActionId, p.RoleId, p.OrgId }).IsUnique();

        builder.HasOne(p => p.Menu)
               .WithMany()
               .HasForeignKey(p => p.MenuId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Page)
               .WithMany(pg => pg.Permissions)
               .HasForeignKey(p => p.PageId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.PageModule)
               .WithMany(pm => pm.Permissions)
               .HasForeignKey(p => p.PageModuleId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Role)
               .WithMany(r => r.Permissions)
               .HasForeignKey(p => p.RoleId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Organization)
               .WithMany()
               .HasForeignKey(p => p.OrgId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);
    }
}
