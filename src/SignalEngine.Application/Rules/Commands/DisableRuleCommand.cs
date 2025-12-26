using MediatR;

namespace SignalEngine.Application.Rules.Commands;

/// <summary>
/// Command to disable a rule.
/// </summary>
public record DisableRuleCommand(int RuleId) : IRequest<bool>;
