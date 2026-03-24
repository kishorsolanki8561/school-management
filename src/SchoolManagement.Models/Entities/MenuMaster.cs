namespace SchoolManagement.Models.Entities;

public sealed class MenuMaster : BaseEntity
{
    public string  Name                    { get; set; } = string.Empty;
    public bool    HasChild                { get; set; }
    public int?    ParentMenuId            { get; set; }
    public int     Position                { get; set; }
    public string? IconClass               { get; set; }
    public bool    IsActive                { get; set; } = true;
    public bool    IsUseMenuForOwnerAdmin  { get; set; }

    public MenuMaster?             ParentMenu { get; init; }
    public ICollection<MenuMaster> ChildMenus { get; init; } = new List<MenuMaster>();
    public ICollection<PageMaster> Pages      { get; init; } = new List<PageMaster>();
}
