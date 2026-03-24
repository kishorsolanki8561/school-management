using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure.Configurations;

internal sealed class PageMasterModuleConfiguration : IEntityTypeConfiguration<PageMasterModule>
{
    public void Configure(EntityTypeBuilder<PageMasterModule> builder)
    {
        builder.HasQueryFilter(m => !m.IsDeleted);

        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();

        builder.HasOne(m => m.Page)
               .WithMany(p => p.Modules)
               .HasForeignKey(m => m.PageId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
