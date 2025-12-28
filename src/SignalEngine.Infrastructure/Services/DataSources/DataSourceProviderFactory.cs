using SignalEngine.Application.Common.Interfaces;

namespace SignalEngine.Infrastructure.Services.DataSources;

/// <summary>
/// Factory for resolving data source providers by code.
/// Providers are registered via DI and resolved at runtime.
/// </summary>
public class DataSourceProviderFactory : IDataSourceProviderFactory
{
    private readonly Dictionary<string, IDataSourceProvider> _providers;

    public DataSourceProviderFactory(IEnumerable<IDataSourceProvider> providers)
    {
        _providers = providers.ToDictionary(
            p => p.DataSourceCode.ToUpperInvariant(),
            p => p,
            StringComparer.OrdinalIgnoreCase);
    }

    public IDataSourceProvider? GetProvider(string dataSourceCode)
    {
        return _providers.TryGetValue(dataSourceCode.ToUpperInvariant(), out var provider)
            ? provider
            : null;
    }

    public IReadOnlyList<string> GetRegisteredDataSourceCodes()
    {
        return _providers.Keys.ToList();
    }
}
