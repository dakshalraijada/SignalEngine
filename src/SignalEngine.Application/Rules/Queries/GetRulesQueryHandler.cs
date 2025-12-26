using MediatR;
using SignalEngine.Application.Common.DTOs;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Constants;

namespace SignalEngine.Application.Rules.Queries;

/// <summary>
/// Handler for GetRulesQuery.
/// </summary>
public class GetRulesQueryHandler : IRequestHandler<GetRulesQuery, IReadOnlyList<RuleDto>>
{
    private readonly IRuleRepository _ruleRepository;
    private readonly ILookupRepository _lookupRepository;
    private readonly ICurrentUserService _currentUserService;

    public GetRulesQueryHandler(
        IRuleRepository ruleRepository,
        ILookupRepository lookupRepository,
        ICurrentUserService currentUserService)
    {
        _ruleRepository = ruleRepository;
        _lookupRepository = lookupRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<RuleDto>> Handle(GetRulesQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId
            ?? throw new InvalidOperationException("User must be associated with a tenant.");

        var rules = request.ActiveOnly
            ? await _ruleRepository.GetActiveByTenantIdAsync(tenantId, cancellationToken)
            : await _ruleRepository.GetByTenantIdAsync(tenantId, cancellationToken);

        var result = new List<RuleDto>();

        foreach (var rule in rules)
        {
            var operatorCode = await _lookupRepository.ResolveLookupCodeAsync(rule.OperatorId, cancellationToken);
            var severityCode = await _lookupRepository.ResolveLookupCodeAsync(rule.SeverityId, cancellationToken);
            var frequencyCode = await _lookupRepository.ResolveLookupCodeAsync(rule.EvaluationFrequencyId, cancellationToken);

            result.Add(new RuleDto(
                rule.Id,
                rule.TenantId,
                rule.AssetId,
                rule.Name,
                rule.Description,
                rule.MetricName,
                operatorCode,
                rule.Threshold,
                severityCode,
                frequencyCode,
                rule.ConsecutiveBreachesRequired,
                rule.IsActive,
                rule.CreatedAt));
        }

        return result;
    }
}
