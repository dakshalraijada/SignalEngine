using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Constants;

namespace SignalEngine.Infrastructure.Services.DataSources;

/// <summary>
/// Binance data source provider that fetches real-time cryptocurrency prices
/// from the Binance public REST API.
/// 
/// Uses the Binance Ticker Price endpoint:
/// GET https://api.binance.com/api/v3/ticker/price?symbol=BTCUSDT
/// 
/// Note: Asset identifiers should match Binance symbol format (e.g., "BTCUSDT").
/// The provider maps common short forms (BTC -> BTCUSDT) for convenience.
/// </summary>
public class BinanceDataSourceProvider : IDataSourceProvider
{
    private const string BinanceApiBase = "https://api.binance.com/api/v3";
    
    private readonly HttpClient _httpClient;
    private readonly ILogger<BinanceDataSourceProvider> _logger;

    public BinanceDataSourceProvider(
        HttpClient httpClient,
        ILogger<BinanceDataSourceProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public string DataSourceCode => DataSourceCodes.Binance;

    public async Task<IReadOnlyDictionary<string, DataSourceFetchResult>> FetchBatchAsync(
        IReadOnlyList<string> assetIdentifiers,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Fetching {Count} assets from Binance: {Assets}",
            assetIdentifiers.Count,
            string.Join(", ", assetIdentifiers));

        var results = new Dictionary<string, DataSourceFetchResult>();
        var timestamp = DateTime.UtcNow;

        // Process each asset identifier
        foreach (var identifier in assetIdentifiers)
        {
            var result = await FetchSingleAssetAsync(identifier, timestamp, cancellationToken);
            results[identifier] = result;
        }

        var successCount = results.Count(r => r.Value.Success);
        _logger.LogDebug(
            "Fetched {SuccessCount}/{TotalCount} assets from Binance",
            successCount,
            results.Count);

        return results;
    }

    private async Task<DataSourceFetchResult> FetchSingleAssetAsync(
        string identifier,
        DateTime timestamp,
        CancellationToken cancellationToken)
    {
        try
        {
            // Map common short forms to Binance symbols
            var symbol = NormalizeSymbol(identifier);
            
            var url = $"{BinanceApiBase}/ticker/price?symbol={symbol}";
            
            _logger.LogDebug("Calling Binance API: {Url}", url);
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Binance API returned {StatusCode} for {Symbol}: {Error}",
                    response.StatusCode,
                    symbol,
                    errorContent);
                    
                return DataSourceFetchResult.Failure(
                    identifier,
                    $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
            }

            var tickerData = await response.Content.ReadFromJsonAsync<BinanceTickerResponse>(
                JsonSerializerOptions,
                cancellationToken);

            if (tickerData == null || string.IsNullOrEmpty(tickerData.Price))
            {
                return DataSourceFetchResult.Failure(identifier, "Invalid response from Binance");
            }

            // Parse the price
            if (!decimal.TryParse(tickerData.Price, out var price))
            {
                return DataSourceFetchResult.Failure(identifier, $"Could not parse price: {tickerData.Price}");
            }

            _logger.LogDebug(
                "Binance {Symbol} price: {Price}",
                symbol,
                price);

            // Return the price metric
            var values = new List<FetchedMetricValue>
            {
                new("price", price, timestamp)
            };

            return DataSourceFetchResult.Ok(identifier, values);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching {Identifier} from Binance", identifier);
            return DataSourceFetchResult.Failure(identifier, $"HTTP error: {ex.Message}");
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching {Identifier} from Binance", identifier);
            return DataSourceFetchResult.Failure(identifier, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Normalizes common short forms to Binance trading pair symbols.
    /// E.g., "BTC" -> "BTCUSDT", "ETH" -> "ETHUSDT"
    /// If already a full symbol (contains USDT/BUSD), returns as-is.
    /// </summary>
    private static string NormalizeSymbol(string identifier)
    {
        var upper = identifier.ToUpperInvariant().Trim();
        
        // If already a full trading pair, return as-is
        if (upper.EndsWith("USDT") || upper.EndsWith("BUSD") || upper.EndsWith("BTC"))
        {
            return upper;
        }

        // Default to USDT pair
        return upper + "USDT";
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Response model for Binance ticker price endpoint.
    /// Example: {"symbol":"BTCUSDT","price":"95234.56000000"}
    /// </summary>
    private sealed class BinanceTickerResponse
    {
        [JsonPropertyName("symbol")]
        public string? Symbol { get; set; }

        [JsonPropertyName("price")]
        public string? Price { get; set; }
    }
}
