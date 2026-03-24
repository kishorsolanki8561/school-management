using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure.Configurations;

internal sealed class PageMasterModuleActionMappingConfiguration : IEntityTypeConfiguration<PageMasterModuleActionMapping>
{
    public void Configure(EntityTypeBuilder<PageMasterModuleActionMapping> builder)
    {
        builder.HasQueryFilter(m => !m.IsDeleted);

        builder.Property(m => m.ActionId).HasConversion<int>();

        builder.HasIndex(m => new { m.PageId, m.PageModuleId, m.ActionId }).IsUnique();

        builder.HasOne(m => m.Page)
               .WithMany()
               .HasForeignKey(m => m.PageId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.PageModule)
               .WithMany(pm => pm.ActionMappings)
               .HasForeignKey(m => m.PageModuleId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
