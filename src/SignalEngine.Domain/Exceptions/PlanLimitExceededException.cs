namespace SignalEngine.Domain.Exceptions;

/// <summary>
/// Exception thrown when plan limits are exceeded.
/// </summary>
public class PlanLimitExceededException : DomainException
{
    public string LimitType { get; }
    public int CurrentCount { get; }
    public int MaxAllowed { get; }

    public PlanLimitExceededException(string limitType, int currentCount, int maxAllowed)
        : base($"Plan limit exceeded for '{limitType}'. Current: {currentCount}, Max: {maxAllowed}.")
    {
        LimitType = limitType;
        CurrentCount = currentCount;
        MaxAllowed = maxAllowed;
    }
}
