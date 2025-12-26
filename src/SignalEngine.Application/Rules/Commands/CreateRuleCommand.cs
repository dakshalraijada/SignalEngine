using MediatR;

namespace SignalEngine.Application.Rules.Commands;

/// <summary>
/// Command to create a new rule.
/// </summary>
public record CreateRuleCommand : IRequest<int>
{
    public int AssetId { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string MetricName { get; init; } = null!;
    public string OperatorCode { get; init; } = null!;
    public decimal Threshold { get; init; }
    public string SeverityCode { get; init; } = null!;
    public string EvaluationFrequencyCode { get; init; } = null!;
    public int ConsecutiveBreachesRequired { get; init; } = 1;
}
