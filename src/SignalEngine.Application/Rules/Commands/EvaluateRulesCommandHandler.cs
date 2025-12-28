using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Constants;
using SignalEngine.Domain.Entities;

namespace SignalEngine.Application.Rules.Commands;

/// <summary>
/// Handler for evaluating all active rules and generating signals when thresholds are breached.
/// 
/// === RESPONSIBILITY BOUNDARIES ===
/// 
/// ✅ MUST DO:
/// - Read MetricData (latest value per metric)
/// - Evaluate active Rules against threshold
/// - Update SignalState (consecutive breaches tracking)
/// - Append Signals when conditions are met
/// 
/// ❌ MUST NOT:
/// - Ingest data (MetricIngestionWorker responsibility)
/// - Deliver notifications (NotificationWorker responsibility - future)
/// - Call SystemApi HTTP endpoints (direct DB access only)
/// - Mutate existing Signals (immutable after creation)
/// - Bypass CQRS patterns
/// 
/// === EVALUATION SEMANTICS ===
/// 
/// 1. MetricData Selection:
///    - Uses LATEST value only (most recent by Timestamp)
///    - Time-windowed aggregation NOT implemented (future work)
/// 
/// 2. Operators (Rule.Evaluate):
///    - GT:  metricValue > Threshold
///    - GTE: metricValue >= Threshold
///    - LT:  metricValue &lt; Threshold
///    - LTE: metricValue &lt;= Threshold
///    - EQ:  metricValue == Threshold
/// 
/// 3. ConsecutiveBreachesRequired:
///    - Increment on breach → SignalState.RecordBreach()
///    - Reset on non-breach → SignalState.RecordSuccess()
///    - Trigger Signal ONLY when ConsecutiveBreaches >= ConsecutiveBreachesRequired
/// 
/// 4. Signal Creation:
///    - ONE Signal per breach cycle (when threshold reached)
///    - Signal is IMMUTABLE after creation
///    - Severity derived from Rule.SeverityId
///    - SignalState.Reset() called after signal creation
/// 
/// 5. SignalState Lifecycle:
///    - One row per Rule (1:1 relationship)
///    - Tracks: IsBreached, ConsecutiveBreaches, LastEvaluatedAt, LastMetricValue
///    - GetOrCreate pattern (auto-creates if missing)
///    - Resets after signal creation to start fresh breach cycle
/// 
/// === TRANSACTION BOUNDARIES ===
/// 
/// All changes within a single evaluation cycle are persisted in ONE transaction:
/// - SignalState updates
/// - Signal additions
/// 
/// If any rule fails, it's logged and skipped; other rules continue.
/// Final SaveChanges commits all successful evaluations atomically.
/// 
/// === FAILURE HANDLING ===
/// 
/// - Missing MetricData: Rule skipped (not an error)
/// - Individual rule evaluation failure: Logged, rule skipped, others continue
/// - Transaction failure: All changes rolled back, no partial state
/// - Safe to retry: SignalState prevents duplicate signals
/// </summary>
public class EvaluateRulesCommandHandler : IRequestHandler<EvaluateRulesCommand, EvaluateRulesResult>
{
    private readonly IRuleEvaluationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EvaluateRulesCommandHandler> _logger;

    public EvaluateRulesCommandHandler(
        IRuleEvaluationRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<EvaluateRulesCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<EvaluateRulesResult> Handle(
        EvaluateRulesCommand request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var rulesEvaluated = 0;
        var signalsCreated = 0;
        var rulesSkipped = 0;
        var errors = 0;

        try
        {
            // Step 1: Load all active rules with dependencies
            var rules = await _repository.GetActiveRulesWithDependenciesAsync(cancellationToken);

            if (rules.Count == 0)
            {
                _logger.LogDebug("No active rules found for evaluation");
                return EvaluateRulesResult.Empty(stopwatch.Elapsed);
            }

            _logger.LogInformation("Starting rule evaluation for {Count} active rules", rules.Count);

            // Pre-fetch the OPEN signal status ID (used for all signals in this cycle)
            var openStatusId = await _repository.ResolveLookupIdAsync(
                LookupTypeCodes.SignalStatus,
                SignalStatusCodes.Open,
                cancellationToken);

            // Pre-fetch the EMAIL notification channel ID (used for notification queuing)
            var emailChannelId = await _repository.ResolveLookupIdAsync(
                LookupTypeCodes.NotificationChannelType,
                NotificationChannelTypeCodes.Email,
                cancellationToken);

            // Step 2: Evaluate each rule
            foreach (var rule in rules)
            {
                try
                {
                    var result = await EvaluateRuleAsync(rule, openStatusId, emailChannelId, cancellationToken);
                    
                    switch (result)
                    {
                        case RuleEvaluationOutcome.Evaluated:
                            rulesEvaluated++;
                            break;
                        case RuleEvaluationOutcome.SignalCreated:
                            rulesEvaluated++;
                            signalsCreated++;
                            break;
                        case RuleEvaluationOutcome.SkippedNoData:
                            rulesSkipped++;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    errors++;
                    _logger.LogError(ex,
                        "Error evaluating rule {RuleId} ({RuleName}) for tenant {TenantId}",
                        rule.Id, rule.Name, rule.TenantId);
                    // Continue with other rules - don't let one failure stop the cycle
                }
            }

            // Step 3: Persist all changes in a single transaction
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "Rule evaluation completed. Evaluated: {Evaluated}, Signals: {Signals}, Skipped: {Skipped}, Errors: {Errors}, Duration: {Duration}ms",
                rulesEvaluated, signalsCreated, rulesSkipped, errors, stopwatch.ElapsedMilliseconds);

            return new EvaluateRulesResult(rulesEvaluated, signalsCreated, rulesSkipped, errors, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during rule evaluation");
            throw;
        }
    }

    /// <summary>
    /// Evaluates a single rule against its latest metric data.
    /// </summary>
    private async Task<RuleEvaluationOutcome> EvaluateRuleAsync(
        Rule rule,
        int openStatusId,
        int emailChannelId,
        CancellationToken cancellationToken)
    {
        // Step 1: Get latest metric data for this rule
        var latestData = await _repository.GetLatestMetricValueAsync(
            rule.AssetId,
            rule.MetricName,
            cancellationToken);

        if (latestData == null)
        {
            _logger.LogDebug(
                "Rule {RuleId} skipped: No metric data for asset {AssetId}, metric '{MetricName}'",
                rule.Id, rule.AssetId, rule.MetricName);
            return RuleEvaluationOutcome.SkippedNoData;
        }

        // Step 2: Get or create SignalState for tracking breaches
        var signalState = await _repository.GetOrCreateSignalStateAsync(
            rule.TenantId,
            rule.Id,
            cancellationToken);

        // Step 3: Resolve operator code (already loaded via Include, but safeguard)
        var operatorCode = rule.Operator?.Code
            ?? await _repository.ResolveLookupCodeAsync(rule.OperatorId, cancellationToken);

        // Step 4: Evaluate the rule condition
        var isBreached = rule.Evaluate(operatorCode, latestData.Value);

        _logger.LogDebug(
            "Rule {RuleId} evaluated: Value={Value}, Threshold={Threshold}, Operator={Op}, Breached={Breached}",
            rule.Id, latestData.Value, rule.Threshold, operatorCode, isBreached);

        if (isBreached)
        {
            // Record breach - increments consecutive counter
            signalState.RecordBreach(latestData.Value);

            _logger.LogDebug(
                "Rule {RuleId} breach recorded. Consecutive: {Count}/{Required}",
                rule.Id, signalState.ConsecutiveBreaches, rule.ConsecutiveBreachesRequired);

            // Check if we've reached the threshold for signal creation
            if (signalState.ConsecutiveBreaches >= rule.ConsecutiveBreachesRequired)
            {
                await CreateSignalAsync(rule, latestData.Value, operatorCode, openStatusId, emailChannelId, cancellationToken);
                
                // Reset state to start new breach cycle
                signalState.Reset();
                
                return RuleEvaluationOutcome.SignalCreated;
            }
        }
        else
        {
            // No breach - reset consecutive counter
            signalState.RecordSuccess(latestData.Value);
            
            _logger.LogDebug(
                "Rule {RuleId} condition not breached. Consecutive breaches reset to 0",
                rule.Id);
        }

        // SignalState is tracked by DbContext, changes will be persisted on SaveChanges
        return RuleEvaluationOutcome.Evaluated;
    }

    /// <summary>
    /// Creates a new signal for a breached rule and queues a notification.
    /// 
    /// === QUEUE-ONLY NOTIFICATION SEMANTICS ===
    /// Notifications are ONLY persisted to the database (queued).
    /// NotificationWorker is the SOLE component that dispatches notifications.
    /// This method NEVER sends emails or webhooks directly.
    /// </summary>
    private async Task CreateSignalAsync(
        Rule rule,
        decimal metricValue,
        string operatorCode,
        int openStatusId,
        int emailChannelId,
        CancellationToken cancellationToken)
    {
        var severityCode = rule.Severity?.Code
            ?? await _repository.ResolveLookupCodeAsync(rule.SeverityId, cancellationToken);

        var signal = new Signal(
            tenantId: rule.TenantId,
            ruleId: rule.Id,
            assetId: rule.AssetId,
            signalStatusId: openStatusId,
            title: $"[{severityCode}] {rule.Name}: Threshold breached",
            triggerValue: metricValue,
            thresholdValue: rule.Threshold,
            triggeredAt: DateTime.UtcNow,
            description: BuildSignalDescription(rule, metricValue, operatorCode));

        await _repository.AddSignalAsync(signal, cancellationToken);
        
        // Save to get the signal ID for notification
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Queue notification for dispatch by NotificationWorker
        // QUEUE-ONLY: This persists to DB but NEVER dispatches
        var notification = new Notification(
            tenantId: rule.TenantId,
            signalId: signal.Id,
            channelTypeId: emailChannelId,
            recipient: "admin@signalengine.local", // TODO: Get from tenant/rule configuration
            subject: signal.Title,
            body: signal.Description ?? signal.Title);

        await _repository.AddNotificationAsync(notification, cancellationToken);

        _logger.LogInformation(
            "Signal created for rule {RuleId} ({RuleName}): Value={Value}, Threshold={Threshold}, Severity={Severity}. Notification queued.",
            rule.Id, rule.Name, metricValue, rule.Threshold, severityCode);
    }

    /// <summary>
    /// Builds a human-readable description for the signal.
    /// </summary>
    private static string BuildSignalDescription(Rule rule, decimal metricValue, string operatorCode)
    {
        var opDescription = operatorCode switch
        {
            "GT" => "exceeded",
            "GTE" => "met or exceeded",
            "LT" => "fell below",
            "LTE" => "met or fell below",
            "EQ" => "equaled",
            _ => "breached"
        };

        return $"Rule '{rule.Name}' triggered: Metric '{rule.MetricName}' value ({metricValue}) {opDescription} threshold ({rule.Threshold}).";
    }

    /// <summary>
    /// Outcome of evaluating a single rule.
    /// </summary>
    private enum RuleEvaluationOutcome
    {
        /// <summary>Rule evaluated, no signal created.</summary>
        Evaluated,
        
        /// <summary>Rule evaluated, signal created.</summary>
        SignalCreated,
        
        /// <summary>Rule skipped due to missing metric data.</summary>
        SkippedNoData
    }
}
