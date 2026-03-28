using SchoolManagement.Models.DTOs;

namespace SchoolManagement.Services.Interfaces;

public interface IDropdownService
{
    /// <summary>
    /// Returns dropdown items for the given key.
    /// Each item always contains "name" and "value"; extra columns appear as additional camelCase keys.
    /// </summary>
    Task<IEnumerable<Dictionary<string, object?>>> GetDropdownAsync(
        DropdownRequest request,
        CancellationToken cancellationToken = default);
}
