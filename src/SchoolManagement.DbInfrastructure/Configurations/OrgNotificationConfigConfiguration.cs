using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure.Configurations;

internal sealed class OrgNotificationConfigConfiguration : IEntityTypeConfiguration<OrgNotificationConfig>
{
    public void Configure(EntityTypeBuilder<OrgNotificationConfig> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Channel).HasConversion<int>();
        builder.Property(e => e.SmsProvider).HasConversion<int>();

        // Email
        builder.Property(e => e.SmtpHost).HasMaxLength(200);
        builder.Property(e => e.SmtpUsername).HasMaxLength(200);
        builder.Property(e => e.SmtpPassword).HasMaxLength(500);
        builder.Property(e => e.FromAddress).HasMaxLength(200);
        builder.Property(e => e.FromName).HasMaxLength(200);

        // SMS
        builder.Property(e => e.ApiKey).HasMaxLength(500);
        builder.Property(e => e.AccountSid).HasMaxLength(200);
        builder.Property(e => e.AuthToken).HasMaxLength(500);
        builder.Property(e => e.SenderNumber).HasMaxLength(50);
        builder.Property(e => e.SenderName).HasMaxLength(200);

        // Push
        builder.Property(e => e.PushServerKey).HasMaxLength(500);
        builder.Property(e => e.PushSenderId).HasMaxLength(200);

        // One config per (OrgId, Channel)
        builder.HasIndex(e => new { e.OrgId, e.Channel }).IsUnique();

        builder.HasOne(e => e.Organization)
               .WithMany()
               .HasForeignKey(e => e.OrgId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
