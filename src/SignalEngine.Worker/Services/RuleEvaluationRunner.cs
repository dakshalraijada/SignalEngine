using MediatR;
using SignalEngine.Application.Rules.Commands;

namespace SignalEngine.Worker.Services;

/// <summary>
/// Orchestrates rule evaluation by sending the command via MediatR.
/// This service is scoped and should be resolved within a DI scope.
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
    /// <returns>Result containing the number of rules evaluated and signals created.</returns>
    public async Task<EvaluateRulesResult> RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting rule evaluation cycle");

        try
        {
            var command = new EvaluateRulesCommand();
            var result = await _mediator.Send(command, cancellationToken);

            _logger.LogInformation(
                "Rule evaluation completed. Rules evaluated: {RulesEvaluated}, Signals created: {SignalsCreated}",
                result.RulesEvaluated,
                result.SignalsCreated);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during rule evaluation cycle");
            throw;
        }
    }
}
