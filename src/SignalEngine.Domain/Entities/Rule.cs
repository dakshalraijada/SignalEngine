using SignalEngine.Domain.Common;

namespace SignalEngine.Domain.Entities;

/// <summary>
/// Represents a rule that evaluates metrics and generates signals.
/// OperatorId references LookupValues (RULE_OPERATOR: GT, LT, EQ, GTE, LTE).
/// SeverityId references LookupValues (SEVERITY: INFO, WARNING, CRITICAL).
/// EvaluationFrequencyId references LookupValues (RULE_EVALUATION_FREQUENCY: 1_MIN, 5_MIN, 15_MIN).
/// </summary>
public class Rule : AuditableEntity, ITenantScoped
{
    public int TenantId { get; private set; }
    public int AssetId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string MetricName { get; private set; } = null!;
    public int OperatorId { get; private set; }
    public decimal Threshold { get; private set; }
    public int SeverityId { get; private set; }
    public int EvaluationFrequencyId { get; private set; }
    public int ConsecutiveBreachesRequired { get; private set; }
    public bool IsActive { get; private set; }

    // Navigation properties
    public LookupValue? Operator { get; private set; }
    public LookupValue? Severity { get; private set; }
    public LookupValue? EvaluationFrequency { get; private set; }
    public Asset? Asset { get; private set; }
    public Tenant? Tenant { get; private set; }

    private readonly List<Signal> _signals = new();
    public IReadOnlyCollection<Signal> Signals => _signals.AsReadOnly();

    private Rule() { } // EF Core

    public Rule(
        int tenantId,
        int assetId,
        string name,
        string metricName,
        int operatorId,
        decimal threshold,
        int severityId,
        int evaluationFrequencyId,
        int consecutiveBreachesRequired = 1,
        string? description = null)
    {
        if (tenantId <= 0)
            throw new ArgumentException("Tenant ID must be positive.", nameof(tenantId));

        if (assetId <= 0)
            throw new ArgumentException("Asset ID must be positive.", nameof(assetId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Rule name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentException("Metric name is required.", nameof(metricName));

        if (operatorId <= 0)
            throw new ArgumentException("Operator ID must be positive.", nameof(operatorId));

        if (severityId <= 0)
            throw new ArgumentException("Severity ID must be positive.", nameof(severityId));

        if (evaluationFrequencyId <= 0)
            throw new ArgumentException("Evaluation frequency ID must be positive.", nameof(evaluationFrequencyId));

        if (consecutiveBreachesRequired < 1)
            throw new ArgumentException("Consecutive breaches required must be at least 1.", nameof(consecutiveBreachesRequired));

        TenantId = tenantId;
        AssetId = assetId;
        Name = name;
        MetricName = metricName;
        OperatorId = operatorId;
        Threshold = threshold;
        SeverityId = severityId;
        EvaluationFrequencyId = evaluationFrequencyId;
        ConsecutiveBreachesRequired = consecutiveBreachesRequired;
        Description = description;
        IsActive = true;
    }

    public void Update(
        string name,
        string metricName,
        int operatorId,
        decimal threshold,
        int severityId,
        int evaluationFrequencyId,
        int consecutiveBreachesRequired,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Rule name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(metricName))
            throw new ArgumentException("Metric name is required.", nameof(metricName));

        Name = name;
        MetricName = metricName;
        OperatorId = operatorId;
        Threshold = threshold;
        SeverityId = severityId;
        EvaluationFrequencyId = evaluationFrequencyId;
        ConsecutiveBreachesRequired = consecutiveBreachesRequired;
        Description = description;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Evaluates a metric value against the rule threshold.
    /// </summary>
    /// <param name="operatorCode">The operator code (GT, LT, EQ, GTE, LTE).</param>
    /// <param name="metricValue">The metric value to evaluate.</param>
    /// <returns>True if the rule condition is breached.</returns>
    public bool Evaluate(string operatorCode, decimal metricValue)
    {
        return operatorCode.ToUpperInvariant() switch
        {
            "GT" => metricValue > Threshold,
            "LT" => metricValue < Threshold,
            "EQ" => metricValue == Threshold,
            "GTE" => metricValue >= Threshold,
            "LTE" => metricValue <= Threshold,
            _ => false
        };
    }
}
