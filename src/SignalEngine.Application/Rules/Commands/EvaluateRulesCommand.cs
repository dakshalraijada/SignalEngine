using MediatR;

namespace SignalEngine.Application.Rules.Commands;

/// <summary>
/// Command to evaluate all active rules and generate signals when thresholds are breached.
/// </summary>
public record EvaluateRulesCommand : IRequest<EvaluateRulesResult>
{
    /// <summary>
    /// Optional: Specific evaluation frequency code to filter rules.
    /// If null, evaluates all active rules.
    /// </summary>
    public string? EvaluationFrequencyCode { get; init; }

    /// <summary>
    /// Optional: Specific tenant ID to evaluate rules for.
    /// If null, evaluates rules for all tenants.
    /// </summary>
    public int? TenantId { get; init; }
}

/// <summary>
/// Result of rule evaluation.
/// </summary>
public record EvaluateRulesResult(
    int RulesEvaluated,
    int SignalsCreated,
    int Errors)
{
    public static EvaluateRulesResult Empty => new(0, 0, 0);
}
