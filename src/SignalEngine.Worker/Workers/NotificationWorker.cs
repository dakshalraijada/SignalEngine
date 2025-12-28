using Microsoft.Extensions.Options;
using SignalEngine.Worker.Options;
using SignalEngine.Worker.Services;

namespace SignalEngine.Worker.Workers;

/// <summary>
/// Background service that periodically dispatches pending notifications.
/// 
/// Design principles:
/// - Runs on a configurable interval (default: 30 seconds)
/// - Picks up notifications that are not yet sent and have not exceeded retry limit
/// - Dispatches via the configured channel (Email, Webhook, Slack)
/// - Marks notifications as sent/failed after dispatch attempt
/// - Does NOT create signals or notifications (separate responsibility)
/// - Multi-instance safe: Multiple workers may process notifications, but:
///   - Each notification is marked sent/failed atomically
///   - Duplicate dispatch attempts are acceptable (idempotent webhooks)
///   - For strict single-processing, add distributed locking
/// </summary>
public class NotificationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<NotificationOptions> _options;
    private readonly ILogger<NotificationWorker> _logger;

    public NotificationWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<NotificationOptions> options,
        ILogger<NotificationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Notification Worker starting. Tick interval: {Interval}, Enabled: {Enabled}",
            _options.Value.TickInterval,
            _options.Value.Enabled);

        if (!_options.Value.Enabled)
        {
            _logger.LogWarning("Notification dispatch is disabled via configuration");
            return;
        }

        // Use PeriodicTimer for efficient, drift-free timing
        using var timer = new PeriodicTimer(_options.Value.TickInterval);

        // Run immediately on startup, then on the timer
        await DispatchNotificationsAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await DispatchNotificationsAsync(stoppingToken);
        }
    }

    private async Task DispatchNotificationsAsync(CancellationToken cancellationToken)
    {
        if (!_options.Value.Enabled)
        {
            return;
        }

        _logger.LogDebug("Notification dispatch tick at {Time}", DateTimeOffset.UtcNow);

        try
        {
            // Create a new scope for each dispatch cycle
            // This ensures proper disposal of scoped services like DbContext
            await using var scope = _scopeFactory.CreateAsyncScope();

            var runner = scope.ServiceProvider.GetRequiredService<NotificationDispatchRunner>();
            var result = await runner.RunAsync(
                _options.Value.MaxNotificationsPerTick,
                _options.Value.MaxRetryCount,
                cancellationToken);

            // Log warnings if failure rate is high
            var total = result.NotificationsSent + result.NotificationsFailed;
            if (result.NotificationsFailed > 0 && total > 0)
            {
                var failureRate = (double)result.NotificationsFailed / total;
                if (failureRate > 0.25) // More than 25% failures
                {
                    _logger.LogWarning(
                        "High failure rate in notification dispatch: {FailureRate:P1} ({Failed}/{Total})",
                        failureRate,
                        result.NotificationsFailed,
                        total);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Notification dispatch cancelled");
        }
        catch (Exception ex)
        {
            // Log but don't crash - worker should continue on next tick
            _logger.LogError(ex, "Error during notification dispatch cycle");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Notification Worker stopping");
        await base.StopAsync(cancellationToken);
    }
}
