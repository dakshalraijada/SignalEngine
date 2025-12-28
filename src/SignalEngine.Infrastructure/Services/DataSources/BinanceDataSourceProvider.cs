using Microsoft.Extensions.Logging;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Constants;

namespace SignalEngine.Infrastructure.Services.DataSources;

/// <summary>
/// Mock Binance data source provider for development/testing.
/// In production, this would make real API calls to Binance.
/// </summary>
public class BinanceDataSourceProvider : IDataSourceProvider
{
    private readonly ILogger<BinanceDataSourceProvider> _logger;
    private readonly Random _random = new();

    public BinanceDataSourceProvider(ILogger<BinanceDataSourceProvider> logger)
    {
        _logger = logger;
    }

    public string DataSourceCode => DataSourceCodes.Binance;

    public Task<IReadOnlyDictionary<string, DataSourceFetchResult>> FetchBatchAsync(
        IReadOnlyList<string> assetIdentifiers,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Fetching {Count} assets from Binance: {Assets}",
            assetIdentifiers.Count,
            string.Join(", ", assetIdentifiers));

        var results = new Dictionary<string, DataSourceFetchResult>();
        var timestamp = DateTime.UtcNow;

        foreach (var identifier in assetIdentifiers)
        {
            // In production: Make actual API call to Binance
            // For now: Generate mock data
            var values = GenerateMockValues(identifier, timestamp);
            results[identifier] = DataSourceFetchResult.Ok(identifier, values);
        }

        _logger.LogDebug("Fetched {Count} assets from Binance", results.Count);

        return Task.FromResult<IReadOnlyDictionary<string, DataSourceFetchResult>>(results);
    }

    private List<FetchedMetricValue> GenerateMockValues(string symbol, DateTime timestamp)
    {
        // Generate realistic mock crypto data
        var basePrice = symbol.ToUpperInvariant() switch
        {
            "BTC" => 95000m + (decimal)(_random.NextDouble() * 2000 - 1000),
            "ETH" => 3400m + (decimal)(_random.NextDouble() * 200 - 100),
            "SOL" => 190m + (decimal)(_random.NextDouble() * 20 - 10),
            _ => 100m + (decimal)(_random.NextDouble() * 20 - 10)
        };

        return new List<FetchedMetricValue>
        {
            new("price", Math.Round(basePrice, 2), timestamp),
            new("volume_24h", Math.Round((decimal)(_random.NextDouble() * 1000000000), 0), timestamp),
            new("price_change_24h", Math.Round((decimal)(_random.NextDouble() * 10 - 5), 2), timestamp)
        };
    }
}
