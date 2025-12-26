namespace SignalEngine.Application.Common.DTOs;

/// <summary>
/// DTO for Metric entity.
/// </summary>
public record MetricDto(
    int Id,
    int TenantId,
    int AssetId,
    string Name,
    string MetricTypeCode,
    decimal Value,
    DateTime Timestamp,
    string? Unit,
    string? Source);
