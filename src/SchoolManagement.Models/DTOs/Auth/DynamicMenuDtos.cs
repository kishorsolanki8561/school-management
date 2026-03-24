using SchoolManagement.Models.Enums;

namespace SchoolManagement.Models.DTOs.Auth;

public sealed class DynamicModuleResponse
{
    public int        Id      { get; init; }
    public string     Name    { get; init; } = string.Empty;
    public int        PageId  { get; init; }

    /// <summary>Action types the role is allowed to perform in this module.</summary>
    public IList<ActionType> Actions { get; set; } = new List<ActionType>();
}

public sealed class DynamicPageResponse
{
    public int     Id      { get; init; }
    public int     MenuId  { get; init; }
    public string  Name    { get; init; } = string.Empty;
    public string? Icon    { get; init; }
    public string  PageUrl { get; init; } = string.Empty;

    /// <summary>Modules (and their allowed actions) the role can access on this page.</summary>
    public IList<DynamicModuleResponse> Modules { get; set; } = new List<DynamicModuleResponse>();
}

public sealed class DynamicMenuResponse
{
    public int     Id           { get; init; }
    public string  Name         { get; init; } = string.Empty;
    public bool    HasChild     { get; init; }
    public int?    ParentMenuId { get; init; }
    public int     Position     { get; init; }
    public string? IconClass    { get; init; }

    public IList<DynamicPageResponse> Pages    { get; set; } = new List<DynamicPageResponse>();
    public IList<DynamicMenuResponse> Children { get; set; } = new List<DynamicMenuResponse>();
}
