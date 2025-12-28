using MediatR;

namespace SignalEngine.Application.Rules.Commands;

/// <summary>
/// Command to evaluate all active rules and generate signals when thresholds are breached.
/// 
/// RESPONSIBILITY BOUNDARIES:
/// ✅ DOES: Read MetricData, Evaluate Rules, Update SignalState, Create Signals
/// ❌ DOES NOT: Ingest data, Deliver notifications, Call HTTP APIs, Mutate existing Signals
/// 
/// This command is idempotent:
/// - Multiple executions with the same metric state produce the same signals
/// - SignalState provides deduplication via ConsecutiveBreaches tracking
/// - Each breach cycle produces exactly one signal
/// </summary>
public record EvaluateRulesCommand : IRequest<EvaluateRulesResult>;

/// <summary>
/// Result of rule evaluation cycle.
/// </summary>
public record EvaluateRulesResult(
    /// <summary>
    /// Number of rules that were evaluated (had metric data available).
    /// </summary>
    int RulesEvaluated,

    /// <summary>
    /// Number of signals created in this cycle.
    /// </summary>
    int SignalsCreated,

    /// <summary>
    /// Number of rules that were skipped due to missing metric data.
    /// </summary>
    int RulesSkipped,

    /// <summary>
    /// Number of errors encountered during evaluation.
    /// </summary>
    int Errors,

    /// <summary>
    /// Duration of the evaluation cycle.
    /// </summary>
    TimeSpan Duration)
{
    public static EvaluateRulesResult Empty(TimeSpan duration) => new(0, 0, 0, 0, duration);
}
