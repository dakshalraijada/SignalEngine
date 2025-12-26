using MediatR;
using Microsoft.Extensions.Logging;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Constants;
using SignalEngine.Domain.Entities;

namespace SignalEngine.Application.Rules.Commands;

/// <summary>
/// Handler for evaluating all active rules and generating signals when thresholds are breached.
/// </summary>
public class EvaluateRulesCommandHandler : IRequestHandler<EvaluateRulesCommand, EvaluateRulesResult>
{
    private readonly IRuleRepository _ruleRepository;
    private readonly IMetricRepository _metricRepository;
    private readonly ISignalRepository _signalRepository;
    private readonly ISignalStateRepository _signalStateRepository;
    private readonly ILookupRepository _lookupRepository;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EvaluateRulesCommandHandler> _logger;

    public EvaluateRulesCommandHandler(
        IRuleRepository ruleRepository,
        IMetricRepository metricRepository,
        ISignalRepository signalRepository,
        ISignalStateRepository signalStateRepository,
        ILookupRepository lookupRepository,
        INotificationDispatcher notificationDispatcher,
        INotificationRepository notificationRepository,
        IUnitOfWork unitOfWork,
        ILogger<EvaluateRulesCommandHandler> logger)
    {
        _ruleRepository = ruleRepository;
        _metricRepository = metricRepository;
        _signalRepository = signalRepository;
        _signalStateRepository = signalStateRepository;
        _lookupRepository = lookupRepository;
        _notificationDispatcher = notificationDispatcher;
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<EvaluateRulesResult> Handle(EvaluateRulesCommand request, CancellationToken cancellationToken)
    {
        var rulesEvaluated = 0;
        var signalsCreated = 0;
        var errors = 0;

        try
        {
            // Get rules to evaluate based on request parameters
            var rules = await GetRulesToEvaluateAsync(request, cancellationToken);
            
            _logger.LogInformation("Starting rule evaluation for {Count} rules", rules.Count);

            foreach (var rule in rules)
            {
                try
                {
                    var signalCreated = await EvaluateRuleAsync(rule, cancellationToken);
                    rulesEvaluated++;
                    if (signalCreated) signalsCreated++;
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogError(ex, "Error evaluating rule {RuleId}: {RuleName}", rule.Id, rule.Name);
                }
            }

            // Save all changes at once
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Rule evaluation completed. Rules: {RulesEvaluated}, Signals: {SignalsCreated}, Errors: {Errors}",
                rulesEvaluated, signalsCreated, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during rule evaluation");
            throw;
        }

        return new EvaluateRulesResult(rulesEvaluated, signalsCreated, errors);
    }

    private async Task<IReadOnlyList<Rule>> GetRulesToEvaluateAsync(
        EvaluateRulesCommand request, 
        CancellationToken cancellationToken)
    {
        // If specific frequency is provided, get rules for that frequency
        if (!string.IsNullOrEmpty(request.EvaluationFrequencyCode))
        {
            var frequencyId = await _lookupRepository.ResolveLookupIdAsync(
                LookupTypeCodes.RuleEvaluationFrequency, 
                request.EvaluationFrequencyCode, 
                cancellationToken);
            
            return await _ruleRepository.GetByEvaluationFrequencyIdAsync(frequencyId, cancellationToken);
        }

        // If specific tenant is provided, get active rules for that tenant
        if (request.TenantId.HasValue)
        {
            return await _ruleRepository.GetActiveByTenantIdAsync(request.TenantId.Value, cancellationToken);
        }

        // Otherwise, get all active rules
        return await _ruleRepository.GetAllActiveAsync(cancellationToken);
    }

    private async Task<bool> EvaluateRuleAsync(Rule rule, CancellationToken cancellationToken)
    {
        // Get latest metric for this rule
        var metric = await _metricRepository.GetLatestByAssetAndNameAsync(
            rule.AssetId, 
            rule.MetricName, 
            cancellationToken);

        if (metric == null)
        {
            _logger.LogDebug(
                "No metric found for rule {RuleId}, asset {AssetId}, metric {MetricName}",
                rule.Id, rule.AssetId, rule.MetricName);
            return false;
        }

        // Get or create signal state for tracking consecutive breaches
        var signalState = await _signalStateRepository.GetOrCreateAsync(
            rule.TenantId, 
            rule.Id, 
            cancellationToken);

        // Get operator code for evaluation
        var operatorCode = await _lookupRepository.ResolveLookupCodeAsync(rule.OperatorId, cancellationToken);

        // Evaluate the rule
        var isBreached = rule.Evaluate(operatorCode, metric.Value);

        if (isBreached)
        {
            signalState.RecordBreach(metric.Value);

            // Check if we've reached the required consecutive breaches
            if (signalState.ConsecutiveBreaches >= rule.ConsecutiveBreachesRequired)
            {
                await CreateSignalAsync(rule, metric, operatorCode, cancellationToken);
                signalState.Reset();
                return true;
            }
        }
        else
        {
            signalState.RecordSuccess(metric.Value);
        }

        await _signalStateRepository.UpdateAsync(signalState, cancellationToken);
        return false;
    }

    private async Task CreateSignalAsync(
        Rule rule, 
        Metric metric, 
        string operatorCode, 
        CancellationToken cancellationToken)
    {
        // Get lookup values for signal creation
        var openStatusId = await _lookupRepository.ResolveLookupIdAsync(
            LookupTypeCodes.SignalStatus, 
            SignalStatusCodes.Open, 
            cancellationToken);

        var severityCode = await _lookupRepository.ResolveLookupCodeAsync(rule.SeverityId, cancellationToken);

        // Create the signal
        var signal = new Signal(
            rule.TenantId,
            rule.Id,
            rule.AssetId,
            openStatusId,
            $"[{severityCode}] {rule.Name}: Threshold breached",
            metric.Value,
            rule.Threshold,
            DateTime.UtcNow,
            $"Rule '{rule.Name}' triggered. Value: {metric.Value}, Threshold: {rule.Threshold}, Operator: {operatorCode}");

        await _signalRepository.AddAsync(signal, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken); // Save to get signal ID

        _logger.LogInformation(
            "Signal created for rule {RuleId}: Value={Value}, Threshold={Threshold}",
            rule.Id, metric.Value, rule.Threshold);

        // Create and dispatch notification
        await CreateAndDispatchNotificationAsync(rule, signal, cancellationToken);
    }

    private async Task CreateAndDispatchNotificationAsync(
        Rule rule, 
        Signal signal, 
        CancellationToken cancellationToken)
    {
        try
        {
            var emailChannelId = await _lookupRepository.ResolveLookupIdAsync(
                LookupTypeCodes.NotificationChannelType, 
                NotificationChannelTypeCodes.Email, 
                cancellationToken);

            var notification = new Notification(
                rule.TenantId,
                signal.Id,
                emailChannelId,
                "admin@signalengine.local", // TODO: Get from tenant settings
                signal.Title,
                signal.Description ?? signal.Title);

            await _notificationRepository.AddAsync(notification, cancellationToken);

            // Dispatch notification
            var sent = await _notificationDispatcher.DispatchAsync(notification, cancellationToken);
            if (sent)
            {
                notification.MarkAsSent();
            }
            else
            {
                notification.MarkAsFailed("Failed to send notification");
            }

            await _notificationRepository.UpdateAsync(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create/dispatch notification for signal {SignalId}", signal.Id);
        }
    }
}
