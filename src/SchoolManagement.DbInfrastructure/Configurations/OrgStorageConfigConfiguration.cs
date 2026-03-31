using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure.Configurations;

internal sealed class OrgStorageConfigConfiguration : IEntityTypeConfiguration<OrgStorageConfig>
{
    public void Configure(EntityTypeBuilder<OrgStorageConfig> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.StorageType).HasConversion<int>();
        builder.Property(e => e.BasePath).HasMaxLength(500);
        builder.Property(e => e.BucketName).HasMaxLength(200);
        builder.Property(e => e.Region).HasMaxLength(100);
        builder.Property(e => e.AccessKey).HasMaxLength(200);
        builder.Property(e => e.SecretKey).HasMaxLength(500);
        builder.Property(e => e.ContainerName).HasMaxLength(200);
        builder.Property(e => e.ConnectionString).HasMaxLength(1000);

        // One active config per org
        builder.HasIndex(e => e.OrgId).IsUnique();

        builder.HasOne(e => e.Organization)
               .WithMany()
               .HasForeignKey(e => e.OrgId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
