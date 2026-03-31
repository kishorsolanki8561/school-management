using SchoolManagement.Models.Enums;

namespace SchoolManagement.Models.Entities;

public sealed class SchoolApprovalRequest : BaseEntity
{
    public int            OrgId               { get; init; }
    public int            RequestedByUserId   { get; init; }
    public ApprovalStatus Status              { get; set; } = ApprovalStatus.Pending;
    public string?        RejectionReason     { get; set; }
    public int?           ReviewedByUserId    { get; set; }
    public DateTime?      ReviewedAt          { get; set; }

    public Organization Organization      { get; init; } = null!;
    public User         RequestedByUser   { get; init; } = null!;
    public User?        ReviewedByUser    { get; set; }
}
