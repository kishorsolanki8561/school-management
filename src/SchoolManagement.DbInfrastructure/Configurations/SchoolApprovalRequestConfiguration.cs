using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SchoolManagement.Models.Entities;

namespace SchoolManagement.DbInfrastructure.Configurations;

internal sealed class SchoolApprovalRequestConfiguration : IEntityTypeConfiguration<SchoolApprovalRequest>
{
    public void Configure(EntityTypeBuilder<SchoolApprovalRequest> builder)
    {
        builder.HasQueryFilter(e => !e.IsDeleted);

        builder.Property(e => e.Status).HasConversion<int>().IsRequired();
        builder.Property(e => e.RejectionReason).HasMaxLength(1000);

        builder.HasOne(e => e.Organization)
               .WithMany(o => o.ApprovalRequests)
               .HasForeignKey(e => e.OrgId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.RequestedByUser)
               .WithMany()
               .HasForeignKey(e => e.RequestedByUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ReviewedByUser)
               .WithMany()
               .HasForeignKey(e => e.ReviewedByUserId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);
    }
}
