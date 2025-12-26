namespace SignalEngine.Application.Common.DTOs;

/// <summary>
/// DTO for LookupValue entity.
/// </summary>
public record LookupValueDto(
    int Id,
    string Code,
    string Name,
    int SortOrder,
    bool IsActive);
