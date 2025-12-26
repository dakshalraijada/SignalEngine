using MediatR;
using SignalEngine.Application.Common.DTOs;

namespace SignalEngine.Application.Rules.Queries;

/// <summary>
/// Query to get rules for the current tenant.
/// </summary>
public record GetRulesQuery : IRequest<IReadOnlyList<RuleDto>>
{
    public bool ActiveOnly { get; init; } = false;
}
