using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure.Configurations;

internal sealed class MenuMasterConfiguration : IEntityTypeConfiguration<MenuMaster>
{
    public void Configure(EntityTypeBuilder<MenuMaster> builder)
    {
        builder.HasQueryFilter(m => !m.IsDeleted);

        builder.Property(m => m.Name).HasMaxLength(200).IsRequired();
        builder.Property(m => m.IconClass).HasMaxLength(100);

        builder.HasOne(m => m.ParentMenu)
               .WithMany(m => m.ChildMenus)
               .HasForeignKey(m => m.ParentMenuId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
