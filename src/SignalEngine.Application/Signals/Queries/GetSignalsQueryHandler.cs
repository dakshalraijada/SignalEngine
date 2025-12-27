using MediatR;
using SignalEngine.Application.Common.DTOs;
using SignalEngine.Application.Common.Interfaces;

namespace SignalEngine.Application.Signals.Queries;

/// <summary>
/// Handler for GetSignalsQuery.
/// </summary>
public class GetSignalsQueryHandler : IRequestHandler<GetSignalsQuery, IReadOnlyList<SignalDto>>
{
    private readonly ISignalRepository _signalRepository;
    private readonly ISignalResolutionRepository _signalResolutionRepository;
    private readonly ILookupRepository _lookupRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetSignalsQueryHandler(
        ISignalRepository signalRepository,
        ISignalResolutionRepository signalResolutionRepository,
        ILookupRepository lookupRepository,
        ICurrentUserService currentUserService)
    {
        _signalRepository = signalRepository;
        _signalResolutionRepository = signalResolutionRepository;
        _lookupRepository = lookupRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<SignalDto>> Handle(GetSignalsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId
            ?? throw new InvalidOperationException("User must be associated with a tenant.");

        var signals = request.OpenOnly
            ? await _signalRepository.GetOpenByTenantIdAsync(tenantId, cancellationToken)
            : await _signalRepository.GetByTenantIdAsync(tenantId, cancellationToken);

        var result = new List<SignalDto>();

        foreach (var signal in signals)
        {
            var statusCode = await _lookupRepository.ResolveLookupCodeAsync(signal.SignalStatusId, cancellationToken);
            
            // Get the latest resolution for this signal if it exists
            var resolution = await _signalResolutionRepository.GetLatestBySignalIdAsync(signal.Id, cancellationToken);
            SignalResolutionDto? resolutionDto = resolution != null
                ? new SignalResolutionDto(resolution.Id, resolution.ResolvedAt, resolution.ResolvedByUserId, resolution.Notes)
                : null;

            result.Add(new SignalDto(
                signal.Id,
                signal.TenantId,
                signal.RuleId,
                signal.AssetId,
                statusCode,
                signal.Title,
                signal.Description,
                signal.TriggerValue,
                signal.ThresholdValue,
                signal.TriggeredAt,
                resolutionDto));
        }

        return result;
    }
}
