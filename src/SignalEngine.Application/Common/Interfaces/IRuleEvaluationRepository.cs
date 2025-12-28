using SignalEngine.Domain.Entities;

namespace SignalEngine.Application.Common.Interfaces;

/// <summary>
/// Specialized repository for Rule Evaluation Worker operations.
/// Provides optimized queries for the evaluation cycle.
/// 
/// Design principles:
/// - Read-optimized for evaluation scenarios
/// - Includes all necessary navigation properties
/// - Bypasses tenant filtering (system-level operation)
/// </summary>
public interface IRuleEvaluationRepository
{
    /// <summary>
    /// Gets all active rules with their associated navigation properties.
    /// Includes: Operator, Severity, Asset.
    /// 
    /// Note: This query bypasses tenant filtering because the worker
    /// evaluates rules across all tenants.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Active rules ready for evaluation.</returns>
    Task<IReadOnlyList<Rule>> GetActiveRulesWithDependenciesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the latest metric data point for a specific asset and metric name.
    /// Used to evaluate rule conditions against the most recent value.
    /// 
    /// Returns null if no data exists (rule should be skipped, not errored).
    /// </summary>
    /// <param name="assetId">The asset ID.</param>
    /// <param name="metricName">The metric name (case-insensitive match).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Latest metric data point, or null if none exists.</returns>
    Task<MetricData?> GetLatestMetricValueAsync(
        int assetId,
        string metricName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates a SignalState for a rule.
    /// Creates a new state if one doesn't exist.
    /// 
    /// The SignalState is attached to the DbContext, so changes
    /// will be persisted on SaveChanges.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <param name="ruleId">The rule ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Existing or new SignalState.</returns>
    Task<SignalState> GetOrCreateSignalStateAsync(
        int tenantId,
        int ruleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new signal to the database.
    /// The signal is attached but not yet persisted.
    /// </summary>
    /// <param name="signal">The signal to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddSignalAsync(
        Signal signal,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a lookup value ID to its code.
    /// Used for operator code resolution during evaluation.
    /// </summary>
    /// <param name="lookupValueId">The lookup value ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The lookup code (e.g., "GT", "LT", "EQ").</returns>
    Task<string> ResolveLookupCodeAsync(
        int lookupValueId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a lookup type code and value code to its ID.
    /// Used for resolving signal status IDs.
    /// </summary>
    /// <param name="typeCode">The lookup type code.</param>
    /// <param name="valueCode">The lookup value code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The lookup value ID.</returns>
    Task<int> ResolveLookupIdAsync(
        string typeCode,
        string valueCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a notification to the queue (database).
    /// QUEUE-ONLY: This method persists the notification but NEVER dispatches it.
    /// Dispatch is handled exclusively by NotificationWorker.
    /// </summary>
    /// <param name="notification">The notification to enqueue.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddNotificationAsync(
        Notification notification,
        CancellationToken cancellationToken = default);
}
