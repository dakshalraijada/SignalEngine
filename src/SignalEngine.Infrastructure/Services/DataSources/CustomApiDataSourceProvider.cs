using Microsoft.Extensions.Logging;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Constants;

namespace SignalEngine.Infrastructure.Services.DataSources;

/// <summary>
/// Custom API data source provider.
/// Fetches data from user-defined endpoints stored in Asset.Metadata.
/// </summary>
public class CustomApiDataSourceProvider : IDataSourceProvider
{
    private readonly ILogger<CustomApiDataSourceProvider> _logger;

    public CustomApiDataSourceProvider(ILogger<CustomApiDataSourceProvider> logger)
    {
        _logger = logger;
    }

    public string DataSourceCode => DataSourceCodes.CustomApi;

    public Task<IReadOnlyDictionary<string, DataSourceFetchResult>> FetchBatchAsync(
        IReadOnlyList<string> assetIdentifiers,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Custom API ingestion for {Count} assets: {Assets}",
            assetIdentifiers.Count,
            string.Join(", ", assetIdentifiers));

        var results = new Dictionary<string, DataSourceFetchResult>();
        var timestamp = DateTime.UtcNow;

        // Custom APIs are per-asset, so we can't batch efficiently
        // Each identifier represents a different custom endpoint
        foreach (var identifier in assetIdentifiers)
        {
            // In production: Parse metadata to get endpoint URL, make HTTP call
            // For now: Return empty (no data) since we don't know the schema
            results[identifier] = DataSourceFetchResult.Ok(identifier, Array.Empty<FetchedMetricValue>());
        }

        return Task.FromResult<IReadOnlyDictionary<string, DataSourceFetchResult>>(results);
    }
}
