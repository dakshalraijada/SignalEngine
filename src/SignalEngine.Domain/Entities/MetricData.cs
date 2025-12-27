using SignalEngine.Domain.Common;

namespace SignalEngine.Domain.Entities;

/// <summary>
/// Represents a time-series data point for a metric.
/// This table stores the actual metric values over time (append-only).
/// </summary>
public class MetricData : Entity, ITenantScoped
{
    public int TenantId { get; private set; }
    public int MetricId { get; private set; }
    public decimal Value { get; private set; }
    public DateTime Timestamp { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public Metric Metric { get; private set; } = null!;
    public Tenant Tenant { get; private set; } = null!;

    private MetricData() { } // EF Core

    public MetricData(int tenantId, int metricId, decimal value, DateTime timestamp)
    {
        if (tenantId <= 0)
            throw new ArgumentException("Tenant ID must be positive.", nameof(tenantId));

        if (metricId <= 0)
            throw new ArgumentException("Metric ID must be positive.", nameof(metricId));

        TenantId = tenantId;
        MetricId = metricId;
        Value = value;
        Timestamp = timestamp;
        CreatedAt = DateTime.UtcNow;
    }
}
