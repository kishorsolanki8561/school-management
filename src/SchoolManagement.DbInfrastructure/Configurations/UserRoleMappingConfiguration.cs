using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure.Configurations;

internal sealed class UserRoleMappingConfiguration : IEntityTypeConfiguration<UserRoleMapping>
{
    public void Configure(EntityTypeBuilder<UserRoleMapping> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.HasOne(urm => urm.User)
               .WithMany(u => u.UserRoleMappings)
               .HasForeignKey(urm => urm.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(urm => urm.Role)
               .WithMany(r => r.UserRoleMappings)
               .HasForeignKey(urm => urm.RoleId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(urm => new { urm.UserId, urm.RoleId }).IsUnique();
    }
}
