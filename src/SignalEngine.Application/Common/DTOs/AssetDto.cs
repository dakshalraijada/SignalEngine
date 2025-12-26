namespace SignalEngine.Application.Common.DTOs;

/// <summary>
/// DTO for Asset entity.
/// </summary>
public record AssetDto
{
    public int Id { get; init; }
    public int TenantId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int AssetTypeLookupValueId { get; init; }
    public string? AssetTypeName { get; init; }
    public int StatusLookupValueId { get; init; }
    public string? StatusName { get; init; }
    public string? ExternalId { get; init; }
    public string? Metadata { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
