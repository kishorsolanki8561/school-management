using SchoolManagement.Models.Enums;

namespace SchoolManagement.Models.DTOs.Master;

// ── Nested module input (used inside Create / Update page requests) ────────────

/// <summary>
/// Module entry nested inside <see cref="CreatePageRequest"/> or <see cref="UpdatePageRequest"/>.
/// When <see cref="Actions"/> is null or empty every <see cref="ActionType"/> value is applied.
/// </summary>
public sealed class CreatePageModuleInput
{
    public string             Name    { get; init; } = string.Empty;
    public IList<ActionType>? Actions { get; init; }   // null / empty → default all ActionTypes
}

// ── Page ──────────────────────────────────────────────────────────────────────

public sealed class CreatePageRequest
{
    public string  Name                   { get; init; } = string.Empty;
    public string? IconClass              { get; init; }
    public string  PageUrl                { get; init; } = string.Empty;
    public int     MenuId                 { get; init; }
    public bool    IsUsePageForOwnerAdmin { get; init; }
    /// <summary>Zero or more modules to create together with this page.</summary>
    public IList<CreatePageModuleInput> Modules { get; init; } = new List<CreatePageModuleInput>();
}

public sealed class UpdatePageRequest
{
    public string  Name                   { get; init; } = string.Empty;
    public string? IconClass              { get; init; }
    public string  PageUrl                { get; init; } = string.Empty;
    public int     MenuId                 { get; init; }
    public bool    IsActive               { get; init; } = true;
    public bool    IsUsePageForOwnerAdmin { get; init; }
    /// <summary>
    /// null = leave existing modules / actions untouched.
    /// Non-null = upsert supplied modules (skips duplicates; never deletes existing).
    /// </summary>
    public IList<CreatePageModuleInput>? Modules { get; init; }
}

public sealed class PageResponse
{
    public int      Id                    { get; init; }
    public string   Name                  { get; init; } = string.Empty;
    public string?  IconClass             { get; init; }
    public string   PageUrl               { get; init; } = string.Empty;
    public int      MenuId                { get; init; }
    public bool     IsActive              { get; init; }
    public bool     IsUsePageForOwnerAdmin { get; init; }
    public DateTime CreatedAt             { get; init; }
}
