using SignalEngine.Domain.Common;

namespace SignalEngine.Domain.Entities;

/// <summary>
/// Represents an asset being monitored.
/// AssetTypeId references LookupValues (ASSET_TYPE: CRYPTO, WEBSITE, SERVICE).
/// </summary>
public class Asset : AuditableEntity, ITenantScoped
{
    public int TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Identifier { get; private set; } = null!;
    public int AssetTypeId { get; private set; }
    public string? Description { get; private set; }
    public string? Metadata { get; private set; }
    public bool IsActive { get; private set; }

    private readonly List<Metric> _metrics = new();
    public IReadOnlyCollection<Metric> Metrics => _metrics.AsReadOnly();

    private readonly List<Rule> _rules = new();
    public IReadOnlyCollection<Rule> Rules => _rules.AsReadOnly();

    private Asset() { } // EF Core

    public Asset(int tenantId, string name, string identifier, int assetTypeId, string? description = null, string? metadata = null)
    {
        if (tenantId <= 0)
            throw new ArgumentException("Tenant ID must be positive.", nameof(tenantId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Asset name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentException("Asset identifier is required.", nameof(identifier));

        if (assetTypeId <= 0)
            throw new ArgumentException("Asset type ID must be positive.", nameof(assetTypeId));

        TenantId = tenantId;
        Name = name;
        Identifier = identifier;
        AssetTypeId = assetTypeId;
        Description = description;
        Metadata = metadata;
        IsActive = true;
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

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
