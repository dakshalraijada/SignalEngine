using MediatR;
using SignalEngine.Application.Rules.Commands;

namespace SignalEngine.Worker.Services;

/// <summary>
/// Orchestrates rule evaluation by sending the command via MediatR.
/// This service is scoped and should be resolved within a DI scope.
/// 
/// Separation of concerns:
/// - Worker: Timing, lifecycle, error handling at host level
/// - Runner: MediatR dispatch, logging at application level
/// - Handler: Business logic, database operations
/// </summary>
public class RuleEvaluationRunner
{
    private readonly IMediator _mediator;
    private readonly ILogger<RuleEvaluationRunner> _logger;

    public RuleEvaluationRunner(IMediator mediator, ILogger<RuleEvaluationRunner> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Executes the rule evaluation process.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing evaluation statistics.</returns>
    public async Task<EvaluateRulesResult> RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting rule evaluation cycle via MediatR");

        try
        {
            var command = new EvaluateRulesCommand();
            var result = await _mediator.Send(command, cancellationToken);

            if (result.RulesEvaluated > 0 || result.SignalsCreated > 0 || result.Errors > 0)
            {
                _logger.LogInformation(
                    "Rule evaluation completed. Evaluated: {Evaluated}, Signals: {Signals}, Skipped: {Skipped}, Errors: {Errors}, Duration: {Duration}ms",
                    result.RulesEvaluated,
                    result.SignalsCreated,
                    result.RulesSkipped,
                    result.Errors,
                    result.Duration.TotalMilliseconds);
            }
            else
            {
                _logger.LogDebug("Rule evaluation completed - no active rules or no data");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during rule evaluation cycle");
            throw;
        }
    }
}
