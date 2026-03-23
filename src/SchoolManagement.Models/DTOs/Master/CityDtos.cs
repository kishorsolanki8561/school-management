namespace SchoolManagement.Models.DTOs.Master;

public sealed class CreateCityRequest
{
    public string Name { get; init; } = string.Empty;
    public int StateId { get; init; }
}

public sealed class UpdateCityRequest
{
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
}

public sealed class CityResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int StateId { get; init; }
    public string StateName { get; init; } = string.Empty;
    public int CountryId { get; init; }
    public string CountryName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}
