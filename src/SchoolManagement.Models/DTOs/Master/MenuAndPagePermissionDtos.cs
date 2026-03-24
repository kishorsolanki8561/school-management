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
