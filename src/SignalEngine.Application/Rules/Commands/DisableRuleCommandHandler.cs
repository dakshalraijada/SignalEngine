using MediatR;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Exceptions;

namespace SignalEngine.Application.Rules.Commands;

/// <summary>
/// Handler for DisableRuleCommand.
/// </summary>
public class DisableRuleCommandHandler : IRequestHandler<DisableRuleCommand, bool>
{
    private readonly IRuleRepository _ruleRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;

    public DisableRuleCommandHandler(
        IRuleRepository ruleRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork)
    {
        _ruleRepository = ruleRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DisableRuleCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _currentUserService.TenantId
            ?? throw new InvalidOperationException("User must be associated with a tenant.");

        var rule = await _ruleRepository.GetByIdAsync(request.RuleId, cancellationToken)
            ?? throw new EntityNotFoundException("Rule", request.RuleId);

        if (rule.TenantId != tenantId)
            throw new TenantAccessDeniedException(tenantId, rule.TenantId);

        rule.Deactivate();
        await _ruleRepository.UpdateAsync(rule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
