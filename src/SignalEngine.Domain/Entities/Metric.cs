using SignalEngine.Domain.Common;

namespace SignalEngine.Domain.Entities;

/// <summary>
/// Represents a metric data point for an asset.
/// MetricTypeId references LookupValues (METRIC_TYPE: NUMERIC, PERCENTAGE, RATE).
/// </summary>
public class Metric : AuditableEntity, ITenantScoped
{
    public int TenantId { get; private set; }
    public int AssetId { get; private set; }
    public string Name { get; private set; } = null!;
    public int MetricTypeId { get; private set; }
    public decimal Value { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string? Unit { get; private set; }
    public string? Source { get; private set; }

    private Metric() { } // EF Core

    public Metric(
        int tenantId,
        int assetId,
        string name,
        int metricTypeId,
        decimal value,
        DateTime timestamp,
        string? unit = null,
        string? source = null)
    {
        if (tenantId <= 0)
            throw new ArgumentException("Tenant ID must be positive.", nameof(tenantId));

        if (assetId <= 0)
            throw new ArgumentException("Asset ID must be positive.", nameof(assetId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Metric name is required.", nameof(name));

        if (metricTypeId <= 0)
            throw new ArgumentException("Metric type ID must be positive.", nameof(metricTypeId));

        TenantId = tenantId;
        AssetId = assetId;
        Name = name;
        MetricTypeId = metricTypeId;
        Value = value;
        Timestamp = timestamp;
        Unit = unit;
        Source = source;
    }

    public void UpdateValue(decimal value, DateTime timestamp)
    {
        Value = value;
        Timestamp = timestamp;
    }
}
