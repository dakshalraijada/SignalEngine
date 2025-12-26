using MediatR;
using SignalEngine.Application.Common.DTOs;

namespace SignalEngine.Application.Signals.Queries;

/// <summary>
/// Query to get signals for the current tenant.
/// </summary>
public record GetSignalsQuery : IRequest<IReadOnlyList<SignalDto>>
{
    public bool OpenOnly { get; init; } = false;
}
