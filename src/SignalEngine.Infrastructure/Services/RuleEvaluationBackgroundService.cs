using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Constants;
using SignalEngine.Domain.Entities;
using SignalEngine.Infrastructure.Persistence;

namespace SignalEngine.Infrastructure.Services;

/// <summary>
/// Background service that evaluates rules and generates signals.
/// </summary>
public class RuleEvaluationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RuleEvaluationBackgroundService> _logger;
    private readonly Dictionary<string, TimeSpan> _frequencyIntervals;

    public RuleEvaluationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<RuleEvaluationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        _frequencyIntervals = new Dictionary<string, TimeSpan>
        {
            { RuleEvaluationFrequencyCodes.OneMinute, TimeSpan.FromMinutes(1) },
            { RuleEvaluationFrequencyCodes.FiveMinutes, TimeSpan.FromMinutes(5) },
            { RuleEvaluationFrequencyCodes.FifteenMinutes, TimeSpan.FromMinutes(15) }
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Rule Evaluation Background Service started");

        // Track last execution time for each frequency
        var lastExecutionTimes = new Dictionary<string, DateTime>();
        foreach (var freq in _frequencyIntervals.Keys)
        {
            lastExecutionTimes[freq] = DateTime.MinValue;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;

                foreach (var (frequencyCode, interval) in _frequencyIntervals)
                {
                    if (now - lastExecutionTimes[frequencyCode] >= interval)
                    {
                        await EvaluateRulesForFrequencyAsync(frequencyCode, stoppingToken);
                        lastExecutionTimes[frequencyCode] = now;
                    }
                }

                // Check every 30 seconds
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in rule evaluation loop");
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }

        _logger.LogInformation("Rule Evaluation Background Service stopped");
    }

    private async Task EvaluateRulesForFrequencyAsync(string frequencyCode, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var lookupRepository = scope.ServiceProvider.GetRequiredService<ILookupRepository>();
        var notificationDispatcher = scope.ServiceProvider.GetRequiredService<INotificationDispatcher>();

        try
        {
            var frequencyId = await lookupRepository.ResolveLookupIdAsync(
                LookupTypeCodes.RuleEvaluationFrequency, frequencyCode, cancellationToken);

            var rules = await context.Rules
                .Where(r => r.EvaluationFrequencyId == frequencyId && r.IsActive)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Evaluating {Count} rules for frequency {Frequency}", rules.Count, frequencyCode);

            foreach (var rule in rules)
            {
                await EvaluateRuleAsync(rule, context, lookupRepository, notificationDispatcher, cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating rules for frequency {Frequency}", frequencyCode);
        }
    }

    private async Task EvaluateRuleAsync(
        Rule rule,
        ApplicationDbContext context,
        ILookupRepository lookupRepository,
        INotificationDispatcher notificationDispatcher,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get latest metric
            var metric = await context.Metrics
                .Where(m => m.AssetId == rule.AssetId && m.Name == rule.MetricName)
                .OrderByDescending(m => m.Timestamp)
                .FirstOrDefaultAsync(cancellationToken);

            if (metric == null)
            {
                _logger.LogDebug("No metric found for rule {RuleId}, asset {AssetId}, metric {MetricName}",
                    rule.Id, rule.AssetId, rule.MetricName);
                return;
            }

            // Get or create signal state
            var signalState = await context.SignalStates
                .FirstOrDefaultAsync(s => s.RuleId == rule.Id, cancellationToken);

            if (signalState == null)
            {
                signalState = new SignalState(rule.TenantId, rule.Id);
                await context.SignalStates.AddAsync(signalState, cancellationToken);
            }

            // Get operator code
            var operatorCode = await lookupRepository.ResolveLookupCodeAsync(rule.OperatorId, cancellationToken);

            // Evaluate rule
            var isBreached = rule.Evaluate(operatorCode, metric.Value);

            if (isBreached)
            {
                signalState.RecordBreach(metric.Value);

                if (signalState.ConsecutiveBreaches >= rule.ConsecutiveBreachesRequired)
                {
                    // Create signal
                    var openStatusId = await lookupRepository.ResolveLookupIdAsync(
                        LookupTypeCodes.SignalStatus, SignalStatusCodes.Open, cancellationToken);

                    var severityCode = await lookupRepository.ResolveLookupCodeAsync(rule.SeverityId, cancellationToken);

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

                    await context.Signals.AddAsync(signal, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken); // Save to get signal ID

                    // Create notification
                    var emailChannelId = await lookupRepository.ResolveLookupIdAsync(
                        LookupTypeCodes.NotificationChannelType, NotificationChannelTypeCodes.Email, cancellationToken);

                    var notification = new Notification(
                        rule.TenantId,
                        signal.Id,
                        emailChannelId,
                        "admin@signalengine.local",
                        signal.Title,
                        signal.Description ?? signal.Title);

                    await context.Notifications.AddAsync(notification, cancellationToken);

                    // Dispatch notification
                    var sent = await notificationDispatcher.DispatchAsync(notification, cancellationToken);
                    if (sent)
                    {
                        notification.MarkAsSent();
                    }
                    else
                    {
                        notification.MarkAsFailed("Failed to send notification");
                    }

                    // Reset consecutive breaches after signal creation
                    signalState.Reset();

                    _logger.LogInformation(
                        "Signal created for rule {RuleId}: Value={Value}, Threshold={Threshold}",
                        rule.Id, metric.Value, rule.Threshold);
                }
            }
            else
            {
                signalState.RecordSuccess(metric.Value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating rule {RuleId}", rule.Id);
        }
    }
}
