using SignalEngine.Domain.Common;

namespace SignalEngine.Domain.Entities;

/// <summary>
/// Represents a metric definition for an asset.
/// Actual time-series values are stored in MetricData (append-only).
/// MetricTypeId references LookupValues (METRIC_TYPE: NUMERIC, PERCENTAGE, RATE).
/// Data source is now defined at the Asset level, not the Metric level.
/// </summary>
public class Metric : AuditableEntity, ITenantScoped
{
    public int TenantId { get; private set; }
    public int AssetId { get; private set; }
    public string Name { get; private set; } = null!;
    public int MetricTypeId { get; private set; }
    public string? Unit { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation properties
    public LookupValue? MetricType { get; private set; }
    public Asset? Asset { get; private set; }

    private readonly List<MetricData> _dataPoints = new();
    public IReadOnlyCollection<MetricData> DataPoints => _dataPoints.AsReadOnly();

    private Metric() { } // EF Core

    public Metric(
        int tenantId,
        int assetId,
        string name,
        int metricTypeId,
        string? unit = null)
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
        Unit = unit;
        IsActive = true;
    }

    public void Update(string name, string? unit)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Metric name is required.", nameof(name));

        Name = name;
        Unit = unit;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
