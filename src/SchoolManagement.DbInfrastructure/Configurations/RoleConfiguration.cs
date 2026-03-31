using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasQueryFilter(r => !r.IsDeleted);

        // System roles (OrgId IS NULL) keep fixed IDs matching UserRole enum.
        // Org-specific copies get auto-generated IDs.
        builder.Property(r => r.Id).ValueGeneratedOnAdd();

        // Name must be unique within an org (or system-level when OrgId is null).
        builder.HasIndex(r => new { r.Name, r.OrgId }).IsUnique();

        builder.Property(r => r.Name).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(200);

        builder.HasOne(r => r.Organization)
               .WithMany()
               .HasForeignKey(r => r.OrgId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);

        builder.HasOne(r => r.SystemRole)
               .WithMany()
               .HasForeignKey(r => r.SystemRoleId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);
    }
}
