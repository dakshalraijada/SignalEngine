namespace SignalEngine.Application.Common.DTOs;

/// <summary>
/// DTO for Rule entity.
/// </summary>
public record RuleDto(
    int Id,
    int TenantId,
    int AssetId,
    string Name,
    string? Description,
    string MetricName,
    string OperatorCode,
    decimal Threshold,
    string SeverityCode,
    string EvaluationFrequencyCode,
    int ConsecutiveBreachesRequired,
    bool IsActive,
    DateTime CreatedAt);
