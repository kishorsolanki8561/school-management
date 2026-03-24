namespace SchoolManagement.Models.Entities;

public sealed class PageMaster : BaseEntity
{
    public string  Name                    { get; set; } = string.Empty;
    public string? IconClass               { get; set; }
    public string  PageUrl                 { get; set; } = string.Empty;
    public int     MenuId                  { get; set; }
    public bool    IsActive                { get; set; } = true;
    public bool    IsUsePageForOwnerAdmin  { get; set; }

    public MenuMaster?                        Menu        { get; init; }
    public ICollection<PageMasterModule>      Modules     { get; init; } = new List<PageMasterModule>();
    public ICollection<MenuAndPagePermission> Permissions { get; init; } = new List<MenuAndPagePermission>();
}
