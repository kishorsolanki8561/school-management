namespace SchoolManagement.Models.DTOs.Master;

public sealed class CreateMenuRequest
{
    public string  Name                   { get; init; } = string.Empty;
    public bool    HasChild               { get; init; }
    public int?    ParentMenuId           { get; init; }
    public int     Position               { get; init; }
    public string? IconClass              { get; init; }
    public bool    IsUseMenuForOwnerAdmin { get; init; }
}

public sealed class UpdateMenuRequest
{
    public string  Name                   { get; init; } = string.Empty;
    public bool    HasChild               { get; init; }
    public int?    ParentMenuId           { get; init; }
    public int     Position               { get; init; }
    public string? IconClass              { get; init; }
    public bool    IsActive               { get; init; } = true;
    public bool    IsUseMenuForOwnerAdmin { get; init; }
}

public sealed class MenuResponse
{
    public int      Id                    { get; init; }
    public string   Name                  { get; init; } = string.Empty;
    public bool     HasChild              { get; init; }
    public int?     ParentMenuId          { get; init; }
    public int      Position              { get; init; }
    public string?  IconClass             { get; init; }
    public bool     IsActive              { get; init; }
    public bool     IsUseMenuForOwnerAdmin { get; init; }
    public DateTime CreatedAt             { get; init; }
}
