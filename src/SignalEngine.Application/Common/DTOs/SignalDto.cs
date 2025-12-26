namespace SignalEngine.Application.Common.DTOs;

/// <summary>
/// DTO for Signal entity.
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
    DateTime? ResolvedAt,
    string? ResolutionNotes);
