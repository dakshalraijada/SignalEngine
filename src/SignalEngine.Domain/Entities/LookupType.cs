using SignalEngine.Domain.Common;

namespace SignalEngine.Domain.Entities;

/// <summary>
/// Represents a lookup type category (e.g., TENANT_TYPE, PLAN_CODE).
/// This is the parent table for lookup values.
/// </summary>
public class LookupType : Entity
{
    public string Code { get; private set; } = null!;
    public string Description { get; private set; } = null!;

    private readonly List<LookupValue> _lookupValues = new();
    public IReadOnlyCollection<LookupValue> LookupValues => _lookupValues.AsReadOnly();

    private LookupType() { } // EF Core

    public LookupType(string code, string description)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Lookup type code is required.", nameof(code));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Lookup type description is required.", nameof(description));

        Code = code.ToUpperInvariant();
        Description = description;
    }

    public void Update(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Lookup type description is required.", nameof(description));

        Description = description;
    }
}
