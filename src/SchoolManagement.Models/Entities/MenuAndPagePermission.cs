using SchoolManagement.Models.Enums;

namespace SchoolManagement.Models.Entities;

public sealed class MenuAndPagePermission : BaseEntity
{
    public int        MenuId       { get; init; }
    public int        PageId       { get; init; }
    public int        PageModuleId { get; init; }
    public ActionType ActionId     { get; init; }
    public int        RoleId       { get; init; }
    public bool       IsAllowed    { get; set; }

    public MenuMaster?       Menu       { get; init; }
    public PageMaster?       Page       { get; init; }
    public PageMasterModule? PageModule { get; init; }
    public Role?             Role       { get; init; }
}
