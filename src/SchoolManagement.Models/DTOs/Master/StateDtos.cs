namespace SchoolManagement.Models.DTOs.Master;

public sealed class CreateStateRequest
{
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public int CountryId { get; init; }
}

public sealed class UpdateStateRequest
{
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
}

public sealed class StateResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public int CountryId { get; init; }
    public string CountryName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
}
