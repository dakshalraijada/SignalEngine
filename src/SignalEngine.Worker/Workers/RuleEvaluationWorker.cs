using Microsoft.Extensions.Options;
using SignalEngine.Worker.Options;
using SignalEngine.Worker.Services;

namespace SignalEngine.Worker.Workers;

/// <summary>
/// Background service that periodically evaluates rules and generates signals.
/// 
/// === DESIGN PRINCIPLES ===
/// 
/// 1. FIXED INTERVAL EXECUTION
///    - Runs on a configurable interval (default: 5 minutes)
///    - Uses PeriodicTimer for drift-free timing
///    - Executes immediately on startup, then periodically
/// 
/// 2. DI SCOPE PER CYCLE
///    - Creates fresh IServiceScope for each evaluation cycle
///    - Ensures proper DbContext lifecycle
///    - Prevents entity tracking issues across cycles
/// 
/// 3. CQRS VIA MEDIATR
///    - Dispatches EvaluateRulesCommand through MediatR
///    - Handler contains all business logic
///    - Clean separation of concerns
/// 
/// 4. MULTI-INSTANCE SAFETY
///    - Safe to run multiple worker instances
///    - SignalState provides deduplication
///    - Each breach cycle produces exactly one signal
///    - Note: For strict single-processing, add distributed locking (future)
/// 
/// 5. IDEMPOTENT UNDER RETRIES
///    - SignalState.ConsecutiveBreaches tracks breach cycle
///    - SignalState.Reset() after signal creation
///    - Same metric state = same evaluation result
/// 
/// === FAILURE HANDLING ===
/// 
/// - Individual rule failures: Logged, rule skipped, others continue
/// - Cycle-level failures: Logged, retried on next interval
/// - Never crashes the host process
/// - Transaction rollback on SaveChanges failure
/// 
/// === WHAT THIS WORKER DOES NOT DO ===
/// 
/// - Does NOT ingest metric data (MetricIngestionWorker)
/// - Does NOT dispatch notifications (future NotificationWorker)
/// - Does NOT call external HTTP APIs
/// - Does NOT modify existing signals
/// </summary>
public class RuleEvaluationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<RuleEvaluationOptions> _options;
    private readonly ILogger<RuleEvaluationWorker> _logger;

    public RuleEvaluationWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<RuleEvaluationOptions> options,
        ILogger<RuleEvaluationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Rule Evaluation Worker starting. Evaluation interval: {Interval}",
            _options.Value.Interval);

        // Use PeriodicTimer for efficient, drift-free timing
        using var timer = new PeriodicTimer(_options.Value.Interval);

        // Run immediately on startup, then on the timer
        await EvaluateRulesAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await EvaluateRulesAsync(stoppingToken);
        }
    }

    private async Task EvaluateRulesAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Rule evaluation cycle triggered at {Time}", DateTimeOffset.UtcNow);

        try
        {
            // Create a new scope for each evaluation cycle
            // This ensures proper disposal of scoped services like DbContext
            await using var scope = _scopeFactory.CreateAsyncScope();
            
            var runner = scope.ServiceProvider.GetRequiredService<RuleEvaluationRunner>();
            var result = await runner.RunAsync(cancellationToken);

            // Log warnings if error rate is high
            if (result.Errors > 0 && result.RulesEvaluated > 0)
            {
                var totalProcessed = result.RulesEvaluated + result.Errors;
                var errorRate = (double)result.Errors / totalProcessed;
                if (errorRate > 0.1) // More than 10% errors
                {
                    _logger.LogWarning(
                        "High error rate in rule evaluation: {ErrorRate:P1} ({Errors}/{Total})",
                        errorRate,
                        result.Errors,
                        totalProcessed);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Rule evaluation cancelled due to shutdown");
            throw; // Re-throw to allow graceful shutdown
        }
        catch (Exception ex)
        {
            // Log and continue - never crash the host
            _logger.LogError(ex, "Unhandled exception during rule evaluation cycle. Will retry on next interval.");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Rule Evaluation Worker stopping");
        await base.StopAsync(cancellationToken);
    }
}
