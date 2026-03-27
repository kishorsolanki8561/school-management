using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure.Configurations;

internal sealed class OrgFileUploadConfigConfiguration : IEntityTypeConfiguration<OrgFileUploadConfig>
{
    public void Configure(EntityTypeBuilder<OrgFileUploadConfig> builder)
    {
        builder.HasQueryFilter(c => !c.IsDeleted);

        builder.Property(c => c.AllowedExtensions).HasMaxLength(1000).IsRequired();
        builder.Property(c => c.AllowedMimeTypes).HasMaxLength(1000).IsRequired();

        // One config per screen per org
        builder.HasIndex(c => new { c.OrgId, c.PageId }).IsUnique();

        builder.HasOne(c => c.Organization)
               .WithMany()
               .HasForeignKey(c => c.OrgId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Page)
               .WithMany()
               .HasForeignKey(c => c.PageId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
