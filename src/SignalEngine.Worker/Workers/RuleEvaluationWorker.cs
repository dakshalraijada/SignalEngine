using Microsoft.Extensions.Options;
using SignalEngine.Application.Rules.Commands;
using SignalEngine.Worker.Options;
using SignalEngine.Worker.Services;

namespace SignalEngine.Worker.Workers;

/// <summary>
/// Background service that periodically evaluates rules and generates signals.
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

            _logger.LogInformation(
                "Rule evaluation cycle completed. Rules: {RulesEvaluated}, Signals: {SignalsCreated}, Errors: {Errors}",
                result.RulesEvaluated,
                result.SignalsCreated,
                result.Errors);
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
