using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasIndex(a => a.EntityName);
        builder.HasIndex(a => a.EntityId);
        builder.HasIndex(a => a.Timestamp);
        builder.HasIndex(a => a.ScreenName);
        builder.HasIndex(a => a.TableName);

        builder.Property(a => a.BatchId).HasMaxLength(36).IsRequired(false);
        builder.HasIndex(a => a.BatchId);

        builder.Property(a => a.ParentAuditLogId).IsRequired(false);
        builder.HasIndex(a => a.ParentAuditLogId);
        // No FK constraint — audit rows may be archived/purged independently
    }
}
