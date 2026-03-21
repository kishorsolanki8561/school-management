namespace SchoolManagement.Models.Entities;

public sealed class Role
{
    public int     Id          { get; init; }
    public string  Name        { get; init; } = string.Empty;
    public string? Description { get; init; }
}
