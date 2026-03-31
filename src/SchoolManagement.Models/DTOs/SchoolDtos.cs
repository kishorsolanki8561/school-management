using SchoolManagement.Models.Enums;

namespace SchoolManagement.Models.DTOs;

// ── School Registration & Management ─────────────────────────────────────────

public sealed class RegisterSchoolRequest
{
    public string  Name    { get; init; } = string.Empty;
    public string? Address { get; init; }
}

public sealed class UpdateSchoolRequest
{
    public string  Name      { get; init; } = string.Empty;
    public string? Address   { get; init; }
    public bool    IsActive  { get; init; } = true;
}

public sealed class SchoolResponse
{
    public int       Id          { get; init; }
    public string    Name        { get; init; } = string.Empty;
    public string?   Address     { get; init; }
    public string?   SchoolCode  { get; init; }
    public bool      IsActive    { get; init; }
    public bool      IsApproved  { get; init; }
    public DateTime? ApprovedAt  { get; init; }
    public string?   ApprovedBy  { get; init; }
    public DateTime  CreatedAt   { get; init; }
}

// ── Approval Workflow ─────────────────────────────────────────────────────────

public sealed class RejectSchoolRequest
{
    public string RejectionReason { get; init; } = string.Empty;
}

public sealed class ApprovalRequestResponse
{
    public int            Id                 { get; init; }
    public int            OrgId              { get; init; }
    public string         OrgName            { get; init; } = string.Empty;
    public int            RequestedByUserId  { get; init; }
    public string         RequestedByUsername { get; init; } = string.Empty;
    public ApprovalStatus Status             { get; init; }
    public string?        RejectionReason    { get; init; }
    public string?        ReviewedByUsername { get; init; }
    public DateTime?      ReviewedAt         { get; init; }
    public DateTime       CreatedAt          { get; init; }
}
