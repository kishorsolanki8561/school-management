using SchoolManagement.Models.Enums;

namespace SchoolManagement.Models.DTOs.Master;

public sealed class UpdatePermissionRequest
{
    public bool IsAllowed { get; init; }
}

public sealed class MenuAndPagePermissionResponse
{
    public int        Id           { get; init; }
    public int        MenuId       { get; init; }
    public int        PageId       { get; init; }
    public int        PageModuleId { get; init; }
    public ActionType ActionId     { get; init; }
    public int        RoleId       { get; init; }
    public bool       IsAllowed    { get; init; }
    public DateTime   CreatedAt    { get; init; }
}

/// <summary>
/// Enriched permission row — equivalent to the SQL query that uses Fn_GetMenus.
/// MenuName is the full breadcrumb path (e.g. "Settings, Users, Permissions").
/// </summary>
public sealed class PermissionDetailResponse
{
    public int        Id             { get; init; }
    public string     MenuName       { get; init; } = string.Empty;
    public string     PageName       { get; init; } = string.Empty;
    public int        PageModuleId   { get; init; }
    public string     PageModuleName { get; init; } = string.Empty;
    public ActionType ActionId       { get; init; }
    public bool       IsAllowed      { get; init; }
    public int        RoleId         { get; init; }
}
