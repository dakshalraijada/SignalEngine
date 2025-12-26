namespace SignalEngine.Worker.Options;

/// <summary>
/// Configuration options for rule evaluation.
/// </summary>
public class RuleEvaluationOptions
{
    public const string SectionName = "RuleEvaluation";

    /// <summary>
    /// Interval in seconds between rule evaluation cycles.
    /// Default is 300 seconds (5 minutes).
    /// </summary>
    public int IntervalSeconds { get; set; } = 300;

    /// <summary>
    /// Gets the interval as a TimeSpan.
    /// </summary>
    public TimeSpan Interval => TimeSpan.FromSeconds(IntervalSeconds);
}
