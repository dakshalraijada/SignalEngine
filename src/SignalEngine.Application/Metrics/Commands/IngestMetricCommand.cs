using MediatR;

namespace SignalEngine.Application.Metrics.Commands;

/// <summary>
/// Command to ingest a metric value.
/// </summary>
public record IngestMetricCommand : IRequest<int>
{
    public int AssetId { get; init; }
    public string Name { get; init; } = null!;
    public string MetricTypeCode { get; init; } = null!;
    public decimal Value { get; init; }
    public DateTime? Timestamp { get; init; }
    public string? Unit { get; init; }
    public string? Source { get; init; }
}
