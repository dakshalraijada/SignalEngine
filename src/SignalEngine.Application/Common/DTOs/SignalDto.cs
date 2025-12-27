namespace SignalEngine.Application.Common.DTOs;

/// <summary>
/// DTO for Signal entity with optional resolution details.
/// </summary>
public record SignalDto(
    int Id,
    int TenantId,
    int RuleId,
    int AssetId,
    string StatusCode,
    string Title,
    string? Description,
    decimal TriggerValue,
    decimal ThresholdValue,
    DateTime TriggeredAt,
    SignalResolutionDto? Resolution);

/// <summary>
/// DTO for SignalResolution entity.
/// </summary>
public record SignalResolutionDto(
    int Id,
    DateTime ResolvedAt,
    int ResolvedByUserId,
    string? Notes);
