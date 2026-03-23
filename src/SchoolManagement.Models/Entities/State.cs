namespace SchoolManagement.Models.Entities;

public sealed class State : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int CountryId { get; set; }
    public bool IsActive { get; set; } = true;

    public Country Country { get; init; } = null!;
    public ICollection<City> Cities { get; init; } = new List<City>();
}
