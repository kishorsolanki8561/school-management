namespace SchoolManagement.Models.DTOs;

// ── User Management ───────────────────────────────────────────────────────────

public sealed class CreateUserRequest
{
    public string  Username { get; init; } = string.Empty;
    public string  Email    { get; init; } = string.Empty;
    public string  Password { get; init; } = string.Empty;
    public int[]?  RoleIds  { get; init; }
}

public sealed class UpdateUserRequest
{
    public string  Username { get; init; } = string.Empty;
    public string  Email    { get; init; } = string.Empty;
    public bool    IsActive { get; init; } = true;
}

public sealed class UserResponse
{
    public int      Id        { get; init; }
    public string   Username  { get; init; } = string.Empty;
    public string   Email     { get; init; } = string.Empty;
    public bool     IsActive  { get; init; }
    public DateTime CreatedAt { get; init; }

    /// <summary>Roles assigned to this user within the current organisation.</summary>
    public IList<UserRoleResponse> Roles { get; init; } = new List<UserRoleResponse>();
}

public sealed class UserRoleResponse
{
    public int    RoleId   { get; init; }
    public string RoleName { get; init; } = string.Empty;
    public int?   OrgId    { get; init; }
}

public sealed class AssignRoleRequest
{
    public int RoleId { get; init; }
}

// ── Role Upgrade / Downgrade ──────────────────────────────────────────────────

public sealed class ChangeRoleLevelRequest
{
    /// <summary>Target system role: "SuperAdmin" or "Admin".</summary>
    public string TargetRole { get; init; } = string.Empty;
}

// ── Switch School ─────────────────────────────────────────────────────────────

public sealed class SwitchSchoolRequest
{
    public int OrgId { get; init; }
}
