namespace SchoolManagement.Models.Entities;

public sealed class PageMasterModule : BaseEntity
{
    public string Name     { get; set; } = string.Empty;
    public int    PageId   { get; set; }
    public bool   IsActive { get; set; } = true;

    public PageMaster?                                Page           { get; init; }
    public ICollection<PageMasterModuleActionMapping> ActionMappings { get; init; } = new List<PageMasterModuleActionMapping>();
    public ICollection<MenuAndPagePermission>         Permissions    { get; init; } = new List<MenuAndPagePermission>();
}
