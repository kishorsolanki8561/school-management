using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure.Configurations;

internal sealed class PageMasterConfiguration : IEntityTypeConfiguration<PageMaster>
{
    public void Configure(EntityTypeBuilder<PageMaster> builder)
    {
        builder.HasQueryFilter(p => !p.IsDeleted);

        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.PageUrl).HasMaxLength(500).IsRequired();
        builder.Property(p => p.IconClass).HasMaxLength(100);

        builder.HasOne(p => p.Menu)
               .WithMany(m => m.Pages)
               .HasForeignKey(p => p.MenuId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
