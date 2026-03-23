namespace SchoolManagement.Models.Entities;

public sealed class City : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int StateId { get; set; }
    public bool IsActive { get; set; } = true;

    public State State { get; init; } = null!;
}
