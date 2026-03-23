namespace SchoolManagement.Models.Entities;

public sealed class Country : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;   // ISO alpha-3, e.g. "IND"
    public bool IsActive { get; set; } = true;

    public ICollection<State> States { get; init; } = new List<State>();
}
