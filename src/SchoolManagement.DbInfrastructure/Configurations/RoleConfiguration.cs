using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever(); // IDs are fixed — mirror UserRole enum values
        builder.HasIndex(r => r.Name).IsUnique();
        builder.Property(r => r.Name).HasMaxLength(50).IsRequired();
        builder.Property(r => r.Description).HasMaxLength(200);
    }
}
