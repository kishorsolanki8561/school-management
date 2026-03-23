using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure.Configurations;

internal sealed class StateConfiguration : IEntityTypeConfiguration<State>
{
    public void Configure(EntityTypeBuilder<State> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);
        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Code).HasMaxLength(10).IsRequired();
        builder.HasIndex(s => new { s.Name, s.CountryId }).IsUnique();
        builder.HasOne(s => s.Country)
               .WithMany(c => c.States)
               .HasForeignKey(s => s.CountryId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
