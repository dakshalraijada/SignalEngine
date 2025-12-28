using SignalEngine.Domain.Common;

namespace SignalEngine.Domain.Entities;

/// <summary>
/// Represents an asset being monitored.
/// AssetTypeId references LookupValues (ASSET_TYPE: CRYPTO, WEBSITE, SERVICE).
/// DataSourceId references LookupValues (DATA_SOURCE: BINANCE, COINBASE, etc.).
/// Assets define WHERE data comes from; Metrics define WHAT is measured.
/// 
/// Ingestion scheduling is owned by the Asset because:
/// - Different assets may need different polling frequencies (BTC every 30s, website every 5min)
/// - The Asset defines the DataSource, so it owns the ingestion cadence
/// - Grouping for efficient API calls is done by DataSourceId + Identifier pattern
/// </summary>
public class Asset : AuditableEntity, ITenantScoped
{
    public int TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Identifier { get; private set; } = null!;
    public int AssetTypeId { get; private set; }
    public int DataSourceId { get; private set; }
    public string? Description { get; private set; }
    public string? Metadata { get; private set; }
    public bool IsActive { get; private set; }

    // Ingestion scheduling properties
    /// <summary>
    /// How often to ingest data for this asset, in seconds.
    /// Default is 60 seconds. Minimum is 10 seconds.
    /// </summary>
    public int IngestionIntervalSeconds { get; private set; } = 60;

    /// <summary>
    /// When the asset was last successfully ingested (UTC).
    /// Null if never ingested.
    /// </summary>
    public DateTime? LastIngestedAtUtc { get; private set; }

    /// <summary>
    /// When the next ingestion is due (UTC).
    /// Calculated as LastIngestedAtUtc + IngestionIntervalSeconds.
    /// Null means "due now" for new assets.
    /// </summary>
    public DateTime? NextIngestionAtUtc { get; private set; }

    // Navigation properties
    public LookupValue? AssetType { get; private set; }
    public LookupValue? DataSource { get; private set; }
    public Tenant? Tenant { get; private set; }

    private readonly List<Metric> _metrics = new();
    public IReadOnlyCollection<Metric> Metrics => _metrics.AsReadOnly();

    private readonly List<Rule> _rules = new();
    public IReadOnlyCollection<Rule> Rules => _rules.AsReadOnly();

    private Asset() { } // EF Core

    public Asset(int tenantId, string name, string identifier, int assetTypeId, int dataSourceId, string? description = null, string? metadata = null)
    {
        if (tenantId <= 0)
            throw new ArgumentException("Tenant ID must be positive.", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Asset name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Asset identifier is required.", nameof(identifier));

        if (assetTypeId <= 0)
            throw new ArgumentException("Asset type ID must be positive.", nameof(assetTypeId));

        if (dataSourceId <= 0)
            throw new ArgumentException("Data source ID must be positive.", nameof(dataSourceId));

        TenantId = tenantId;
        Name = name;
        Identifier = identifier;
        AssetTypeId = assetTypeId;
        DataSourceId = dataSourceId;
        Description = description;
        Metadata = metadata;
        IsActive = true;
        IngestionIntervalSeconds = 60; // Default to 1 minute
        NextIngestionAtUtc = null; // Null means "due now" for new assets
    }

    public void Update(string name, string identifier, string? description, string? metadata)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Asset name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Asset identifier is required.", nameof(identifier));

        Name = name;
        Identifier = identifier;
        Description = description;
        Metadata = metadata;
    }

    /// <summary>
    /// Sets the ingestion interval for this asset.
    /// </summary>
    /// <param name="intervalSeconds">Interval in seconds. Minimum is 10 seconds.</param>
    public void SetIngestionInterval(int intervalSeconds)
    {
        if (intervalSeconds < 10)
            throw new ArgumentException("Ingestion interval must be at least 10 seconds.", nameof(intervalSeconds));

        IngestionIntervalSeconds = intervalSeconds;
    }

    /// <summary>
    /// Marks the asset as successfully ingested and schedules the next ingestion.
    /// </summary>
    /// <param name="ingestedAtUtc">When the ingestion occurred (UTC).</param>
    public void MarkIngested(DateTime ingestedAtUtc)
    {
        LastIngestedAtUtc = ingestedAtUtc;
        NextIngestionAtUtc = ingestedAtUtc.AddSeconds(IngestionIntervalSeconds);
    }

    /// <summary>
    /// Checks if this asset is due for ingestion.
    /// </summary>
    /// <param name="nowUtc">Current UTC time.</param>
    /// <returns>True if ingestion is due.</returns>
    public bool IsDueForIngestion(DateTime nowUtc)
    {
        if (!IsActive) return false;
        if (NextIngestionAtUtc == null) return true; // Never ingested
        return nowUtc >= NextIngestionAtUtc.Value;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
