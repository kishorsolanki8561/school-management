using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure.Configurations;

internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasQueryFilter(o => !o.IsDeleted);
        builder.HasIndex(o => o.Name).IsUnique();
        builder.Property(o => o.Name).HasMaxLength(200).IsRequired();
        builder.Property(o => o.Address).HasMaxLength(500);
        builder.Property(o => o.SchoolCode).HasMaxLength(50);
        builder.Property(o => o.ApprovedBy).HasMaxLength(200);
    }
}
