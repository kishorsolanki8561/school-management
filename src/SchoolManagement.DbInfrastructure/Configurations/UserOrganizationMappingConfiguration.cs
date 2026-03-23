using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure.Configurations;

internal sealed class UserOrganizationMappingConfiguration : IEntityTypeConfiguration<UserOrganizationMapping>
{
    public void Configure(EntityTypeBuilder<UserOrganizationMapping> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasOne(m => m.User)
               .WithMany(u => u.UserOrganizationMappings)
               .HasForeignKey(m => m.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Organization)
               .WithMany(o => o.UserOrganizationMappings)
               .HasForeignKey(m => m.OrgId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => new { m.UserId, m.OrgId }).IsUnique();
    }
}
