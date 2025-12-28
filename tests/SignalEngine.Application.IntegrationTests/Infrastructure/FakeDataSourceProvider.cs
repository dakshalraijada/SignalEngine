using SignalEngine.Application.Common.Interfaces;

namespace SignalEngine.Application.IntegrationTests.Infrastructure;

/// <summary>
/// Fake data source provider for testing ingestion.
/// Returns predefined results without making external API calls.
/// </summary>
public class FakeDataSourceProvider : IDataSourceProvider
{
    private readonly string _dataSourceCode;
    private readonly Dictionary<string, DataSourceFetchResult> _results = new();
    private int _fetchCallCount;

    public FakeDataSourceProvider(string dataSourceCode)
    {
        _dataSourceCode = dataSourceCode;
    }

    public string DataSourceCode => _dataSourceCode;

    /// <summary>
    /// Number of times FetchBatchAsync was called.
    /// Used to verify batching behavior.
    /// </summary>
    public int FetchCallCount => _fetchCallCount;

    /// <summary>
    /// Configures a successful fetch result for an asset identifier.
    /// </summary>
    public void SetupSuccess(string assetIdentifier, IReadOnlyList<FetchedMetricValue> values)
    {
        _results[assetIdentifier] = DataSourceFetchResult.Ok(assetIdentifier, values);
    }

    /// <summary>
    /// Configures a failure result for an asset identifier.
    /// </summary>
    public void SetupFailure(string assetIdentifier, string errorMessage)
    {
        _results[assetIdentifier] = DataSourceFetchResult.Failure(assetIdentifier, errorMessage);
    }

    /// <summary>
    /// Configures the provider to throw an exception.
    /// </summary>
    public Exception? ThrowOnFetch { get; set; }

    public Task<IReadOnlyDictionary<string, DataSourceFetchResult>> FetchBatchAsync(
        IReadOnlyList<string> assetIdentifiers,
        CancellationToken cancellationToken = default)
    {
        _fetchCallCount++;

        if (ThrowOnFetch != null)
        {
            throw ThrowOnFetch;
        }

        var results = new Dictionary<string, DataSourceFetchResult>();

        foreach (var identifier in assetIdentifiers)
        {
            if (_results.TryGetValue(identifier, out var result))
            {
                results[identifier] = result;
            }
            else
            {
                // No setup = failure
                results[identifier] = DataSourceFetchResult.Failure(identifier, "No result configured");
            }
        }

        return Task.FromResult<IReadOnlyDictionary<string, DataSourceFetchResult>>(results);
    }
}

/// <summary>
/// Fake factory that returns configured fake providers.
/// </summary>
public class FakeDataSourceProviderFactory : IDataSourceProviderFactory
{
    private readonly Dictionary<string, FakeDataSourceProvider> _providers = new();

    public FakeDataSourceProvider AddProvider(string dataSourceCode)
    {
        var provider = new FakeDataSourceProvider(dataSourceCode);
        _providers[dataSourceCode.ToUpperInvariant()] = provider;
        return provider;
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

    public FakeDataSourceProvider? GetFakeProvider(string dataSourceCode)
    {
        return _providers.TryGetValue(dataSourceCode.ToUpperInvariant(), out var provider) 
            ? provider 
            : null;
    }
}
