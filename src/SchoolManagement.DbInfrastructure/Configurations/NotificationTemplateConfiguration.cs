using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure.Configurations;

internal sealed class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.EventType).HasConversion<int>();
        builder.Property(e => e.Channel).HasConversion<int>();
        builder.Property(e => e.Subject).HasMaxLength(500);
        builder.Property(e => e.ToAddresses).HasMaxLength(1000);
        builder.Property(e => e.CcAddresses).HasMaxLength(1000);
        builder.Property(e => e.BccAddresses).HasMaxLength(1000);

        // One template per (OrgId, EventType, Channel) — all three can be null
        builder.HasIndex(e => new { e.OrgId, e.EventType, e.Channel }).IsUnique();

        builder.HasOne(e => e.Organization)
               .WithMany()
               .HasForeignKey(e => e.OrgId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);
    }
}
