namespace SignalEngine.Domain.Exceptions;

/// <summary>
/// Exception thrown when a lookup value is not found.
/// </summary>
public class LookupNotFoundException : DomainException
{
    public string LookupTypeCode { get; }
    public string LookupValueCode { get; }

    public LookupNotFoundException(string lookupTypeCode, string lookupValueCode)
        : base($"Lookup value '{lookupValueCode}' not found for type '{lookupTypeCode}'.")
    {
        LookupTypeCode = lookupTypeCode;
        LookupValueCode = lookupValueCode;
    }
}
