namespace SignalEngine.Application.Common.Interfaces;

/// <summary>
/// Result of fetching metric data from an external data source.
/// </summary>
public record FetchedMetricValue(
    /// <summary>
    /// The metric name (e.g., "price", "volume", "latency").
    /// </summary>
    string MetricName,
    
    /// <summary>
    /// The fetched value.
    /// </summary>
    decimal Value,
    
    /// <summary>
    /// The timestamp when the value was recorded by the source.
    /// </summary>
    DateTime Timestamp);

/// <summary>
/// Result of a data source fetch operation.
/// </summary>
public record DataSourceFetchResult
{
    /// <summary>
    /// True if the fetch was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The asset identifier that was fetched.
    /// </summary>
    public string AssetIdentifier { get; init; } = string.Empty;

    /// <summary>
    /// The fetched metric values. Empty if fetch failed.
    /// </summary>
    public IReadOnlyList<FetchedMetricValue> Values { get; init; } = Array.Empty<FetchedMetricValue>();

    /// <summary>
    /// Error message if fetch failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    public static DataSourceFetchResult Failure(string assetIdentifier, string errorMessage) =>
        new() { Success = false, AssetIdentifier = assetIdentifier, ErrorMessage = errorMessage };

    public static DataSourceFetchResult Ok(string assetIdentifier, IReadOnlyList<FetchedMetricValue> values) =>
        new() { Success = true, AssetIdentifier = assetIdentifier, Values = values };
}

/// <summary>
/// Provides data from an external data source (e.g., Binance, Coinbase, custom API).
/// Implementations are responsible for:
/// - Batching requests efficiently
/// - Handling rate limits
/// - Retrying transient failures
/// </summary>
public interface IDataSourceProvider
{
    /// <summary>
    /// The data source code this provider handles (e.g., "BINANCE", "COINBASE").
    /// </summary>
    string DataSourceCode { get; }

    /// <summary>
    /// Fetches metric data for multiple asset identifiers in a single batch.
    /// This allows efficient API usage (e.g., one API call for multiple symbols).
    /// </summary>
    /// <param name="assetIdentifiers">The asset identifiers to fetch (e.g., ["BTC", "ETH"]).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Fetch results keyed by asset identifier.</returns>
    Task<IReadOnlyDictionary<string, DataSourceFetchResult>> FetchBatchAsync(
        IReadOnlyList<string> assetIdentifiers,
        CancellationToken cancellationToken = default);
}
