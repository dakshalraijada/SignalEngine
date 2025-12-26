using MediatR;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Constants;
using SignalEngine.Domain.Entities;
using SignalEngine.Domain.Exceptions;

namespace SignalEngine.Application.Rules.Commands;

/// <summary>
/// Handler for CreateRuleCommand.
/// </summary>
public class CreateRuleCommandHandler : IRequestHandler<CreateRuleCommand, int>
{
    private readonly IRuleRepository _ruleRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly ITenantRepository _tenantRepository;
    private readonly IPlanRepository _planRepository;
    private readonly ILookupRepository _lookupRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateRuleCommandHandler(
        IRuleRepository ruleRepository,
        IAssetRepository assetRepository,
        ITenantRepository tenantRepository,
        IPlanRepository planRepository,
        ILookupRepository lookupRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _ruleRepository = ruleRepository;
        _assetRepository = assetRepository;
        _tenantRepository = tenantRepository;
        _planRepository = planRepository;
        _lookupRepository = lookupRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CreateRuleCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId
            ?? throw new InvalidOperationException("User must be associated with a tenant.");

        // Validate asset belongs to tenant
        var asset = await _assetRepository.GetByIdAsync(request.AssetId, cancellationToken)
            ?? throw new EntityNotFoundException("Asset", request.AssetId);

        if (asset.TenantId != tenantId)
            throw new TenantAccessDeniedException(tenantId, asset.TenantId);

        // Check plan limits
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new EntityNotFoundException("Tenant", tenantId);

        var plan = await _planRepository.GetByIdAsync(tenant.PlanId, cancellationToken)
            ?? throw new EntityNotFoundException("Plan", tenant.PlanId);

        var currentRuleCount = await _ruleRepository.GetCountByTenantIdAsync(tenantId, cancellationToken);
        if (currentRuleCount >= plan.MaxRules)
            throw new PlanLimitExceededException("Rules", currentRuleCount, plan.MaxRules);

        // Resolve lookup values
        var operatorId = await _lookupRepository.ResolveLookupIdAsync(
            LookupTypeCodes.RuleOperator, request.OperatorCode, cancellationToken);

        var severityId = await _lookupRepository.ResolveLookupIdAsync(
            LookupTypeCodes.Severity, request.SeverityCode, cancellationToken);

        var evaluationFrequencyId = await _lookupRepository.ResolveLookupIdAsync(
            LookupTypeCodes.RuleEvaluationFrequency, request.EvaluationFrequencyCode, cancellationToken);

        // Create rule
        var rule = new Rule(
            tenantId,
            request.AssetId,
            request.Name,
            request.MetricName,
            operatorId,
            request.Threshold,
            severityId,
            evaluationFrequencyId,
            request.ConsecutiveBreachesRequired,
            request.Description);

        await _ruleRepository.AddAsync(rule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return rule.Id;
    }
}
