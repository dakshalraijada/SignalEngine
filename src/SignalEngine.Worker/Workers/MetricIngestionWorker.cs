using Microsoft.Extensions.Options;
using SignalEngine.Worker.Options;
using SignalEngine.Worker.Services;

namespace SignalEngine.Worker.Workers;

/// <summary>
/// Background service that periodically ingests metrics from external data sources.
/// 
/// Design principles:
/// - Runs on a base tick (e.g., 15 seconds) independent of individual asset frequencies
/// - Each tick queries for assets due for ingestion
/// - Groups assets by DataSource for efficient API batching
/// - Fan-out: One API call -> Multiple tenant-owned MetricData records
/// - Idempotent: Safe to run multiple instances (cursor-based scheduling)
/// - Does NOT evaluate rules (separate worker responsibility)
/// - Does NOT call SystemApi (direct database access via repositories)
/// 
/// Multi-instance safety:
/// - Uses NextIngestionAtUtc as a cursor
/// - Multiple workers may pick up the same asset, but:
///   - Data is append-only (duplicate data points are acceptable)
///   - Cursor update is idempotent (last writer wins)
///   - For strict single-processing, add distributed locking
/// </summary>
public class MetricIngestionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<MetricIngestionOptions> _options;
    private readonly ILogger<MetricIngestionWorker> _logger;

    public MetricIngestionWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<MetricIngestionOptions> options,
        ILogger<MetricIngestionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Metric Ingestion Worker starting. Tick interval: {Interval}, Enabled: {Enabled}",
            _options.Value.TickInterval,
            _options.Value.Enabled);

        if (!_options.Value.Enabled)
        {
            _logger.LogWarning("Metric ingestion is disabled via configuration");
            return;
        }

        // Use PeriodicTimer for efficient, drift-free timing
        using var timer = new PeriodicTimer(_options.Value.TickInterval);

        // Run immediately on startup, then on the timer
        await IngestMetricsAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await IngestMetricsAsync(stoppingToken);
        }
    }

    private async Task IngestMetricsAsync(CancellationToken cancellationToken)
    {
        if (!_options.Value.Enabled)
        {
            return;
        }

        _logger.LogDebug("Metric ingestion tick at {Time}", DateTimeOffset.UtcNow);

        try
        {
            // Create a new scope for each ingestion cycle
            // This ensures proper disposal of scoped services like DbContext
            await using var scope = _scopeFactory.CreateAsyncScope();

            var runner = scope.ServiceProvider.GetRequiredService<MetricIngestionRunner>();
            var result = await runner.RunAsync(cancellationToken);

            // Log warnings if error rate is high
            if (result.Errors > 0 && result.AssetsProcessed > 0)
            {
                var errorRate = (double)result.Errors / (result.AssetsProcessed + result.Errors);
                if (errorRate > 0.1) // More than 10% errors
                {
                    _logger.LogWarning(
                        "High error rate in metric ingestion: {ErrorRate:P1} ({Errors}/{Total})",
                        errorRate,
                        result.Errors,
                        result.AssetsProcessed + result.Errors);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Metric ingestion cancelled due to shutdown");
            throw; // Re-throw to allow graceful shutdown
        }
        catch (Exception ex)
        {
            // Log and continue - never crash the host
            _logger.LogError(ex, "Unhandled exception during metric ingestion cycle. Will retry on next tick.");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Metric Ingestion Worker stopping");
        await base.StopAsync(cancellationToken);
    }
}
