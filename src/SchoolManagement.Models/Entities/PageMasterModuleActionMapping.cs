using SchoolManagement.Models.Enums;

namespace SchoolManagement.Models.Entities;

public sealed class PageMasterModuleActionMapping : BaseEntity
{
    public int        PageId       { get; init; }
    public int        PageModuleId { get; init; }
    public ActionType ActionId     { get; init; }

    public PageMaster?       Page       { get; init; }
    public PageMasterModule? PageModule { get; init; }
}
