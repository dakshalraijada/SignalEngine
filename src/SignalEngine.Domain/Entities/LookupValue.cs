using SignalEngine.Domain.Common;

namespace SignalEngine.Domain.Entities;

/// <summary>
/// Represents a lookup value (e.g., B2C, B2B for TENANT_TYPE).
/// All enum-like concepts are stored as lookup values.
/// </summary>
public class LookupValue : Entity
{
    public int LookupTypeId { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    private LookupValue() { } // EF Core

    public LookupValue(int lookupTypeId, string code, string name, int sortOrder = 0, bool isActive = true)
    {
        if (lookupTypeId <= 0)
            throw new ArgumentException("Lookup type ID must be positive.", nameof(lookupTypeId));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Lookup value code is required.", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Lookup value name is required.", nameof(name));

        LookupTypeId = lookupTypeId;
        Code = code.ToUpperInvariant();
        Name = name;
        SortOrder = sortOrder;
        IsActive = isActive;
    }

    public void Update(string name, int sortOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Lookup value name is required.", nameof(name));

        Name = name;
        SortOrder = sortOrder;
        IsActive = isActive;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
