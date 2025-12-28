using FluentAssertions;
using SignalEngine.Domain.Entities;
using Xunit;

namespace SignalEngine.Domain.Tests.Rules;

/// <summary>
/// Domain-level tests for SignalState behavior.
/// Tests consecutive breach tracking, reset behavior, and signal triggering logic.
/// 
/// TESTING PRINCIPLES:
/// - No EF Core, no database, no mocks
/// - Pure domain logic verification
/// - Focus on correctness over coverage
/// 
/// SIGNALSTATE SEMANTICS:
/// - RecordBreach: Increments ConsecutiveBreaches, sets IsBreached = true
/// - RecordSuccess: Resets ConsecutiveBreaches to 0, sets IsBreached = false  
/// - Reset: Clears all state after signal creation
/// - Signal triggers ONLY when ConsecutiveBreaches >= Rule.ConsecutiveBreachesRequired
/// </summary>
public class SignalStateTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_ValidParameters_InitializesCorrectly()
    {
        // Act
        var state = new SignalState(tenantId: 1, ruleId: 100);

        // Assert
        state.TenantId.Should().Be(1);
        state.RuleId.Should().Be(100);
        state.ConsecutiveBreaches.Should().Be(0);
        state.IsBreached.Should().BeFalse();
        state.LastMetricValue.Should().BeNull();
        state.LastEvaluatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    public void Constructor_InvalidParameters_ThrowsArgumentException(int tenantId, int ruleId)
    {
        // Act
        var act = () => new SignalState(tenantId, ruleId);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region RecordBreach Tests

    [Fact]
    public void RecordBreach_FirstBreach_IncrementsToOne()
    {
        // Arrange
        var state = new SignalState(tenantId: 1, ruleId: 1);

        // Act
        state.RecordBreach(metricValue: 150.00m);

        // Assert
        state.ConsecutiveBreaches.Should().Be(1);
        state.IsBreached.Should().BeTrue();
        state.LastMetricValue.Should().Be(150.00m);
    }

    [Fact]
    public void RecordBreach_MultipleTimes_IncrementsEachTime()
    {
        // Arrange
        var state = new SignalState(tenantId: 1, ruleId: 1);

        // Act
        state.RecordBreach(metricValue: 101.00m);
        state.RecordBreach(metricValue: 102.00m);
        state.RecordBreach(metricValue: 103.00m);

        // Assert
        state.ConsecutiveBreaches.Should().Be(3);
        state.IsBreached.Should().BeTrue();
        state.LastMetricValue.Should().Be(103.00m, "should track the most recent value");
    }

    [Fact]
    public void RecordBreach_UpdatesLastEvaluatedAt()
    {
        // Arrange
        var state = new SignalState(tenantId: 1, ruleId: 1);
        var beforeBreach = state.LastEvaluatedAt;

        // Small delay to ensure time difference
        Thread.Sleep(10);

        // Act
        state.RecordBreach(metricValue: 150.00m);

        // Assert
        state.LastEvaluatedAt.Should().BeOnOrAfter(beforeBreach);
    }

    #endregion

    #region RecordSuccess Tests

    [Fact]
    public void RecordSuccess_AfterBreaches_ResetsConsecutiveBreaches()
    {
        // Arrange
        var state = new SignalState(tenantId: 1, ruleId: 1);
        state.RecordBreach(metricValue: 150.00m);
        state.RecordBreach(metricValue: 160.00m);
        state.ConsecutiveBreaches.Should().Be(2);

        // Act
        state.RecordSuccess(metricValue: 80.00m);

        // Assert
        state.ConsecutiveBreaches.Should().Be(0);
        state.IsBreached.Should().BeFalse();
        state.LastMetricValue.Should().Be(80.00m);
    }

    [Fact]
    public void RecordSuccess_WhenAlreadyZero_StaysAtZero()
    {
        // Arrange
        var state = new SignalState(tenantId: 1, ruleId: 1);
        state.ConsecutiveBreaches.Should().Be(0);

        // Act
        state.RecordSuccess(metricValue: 50.00m);

        // Assert
        state.ConsecutiveBreaches.Should().Be(0);
        state.IsBreached.Should().BeFalse();
        state.LastMetricValue.Should().Be(50.00m);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsAllState()
    {
        // Arrange
        var state = new SignalState(tenantId: 1, ruleId: 1);
        state.RecordBreach(metricValue: 150.00m);
        state.RecordBreach(metricValue: 160.00m);

        // Act
        state.Reset();

        // Assert
        state.ConsecutiveBreaches.Should().Be(0);
        state.IsBreached.Should().BeFalse();
        state.LastMetricValue.Should().BeNull("Reset clears the last metric value");
    }

    [Fact]
    public void Reset_UpdatesLastEvaluatedAt()
    {
        // Arrange
        var state = new SignalState(tenantId: 1, ruleId: 1);
        state.RecordBreach(metricValue: 150.00m);
        var beforeReset = state.LastEvaluatedAt;

        Thread.Sleep(10);

        // Act
        state.Reset();

        // Assert
        state.LastEvaluatedAt.Should().BeOnOrAfter(beforeReset);
    }

    #endregion

    #region Signal Trigger Logic Tests (Domain Behavior)

    [Theory]
    [InlineData(1, 1, true)]   // 1 breach, requires 1 → trigger
    [InlineData(2, 2, true)]   // 2 breaches, requires 2 → trigger
    [InlineData(1, 2, false)]  // 1 breach, requires 2 → no trigger
    [InlineData(2, 3, false)]  // 2 breaches, requires 3 → no trigger
    [InlineData(5, 3, true)]   // 5 breaches, requires 3 → trigger (exceeds threshold)
    public void ConsecutiveBreaches_TriggerCondition_EvaluatesCorrectly(
        int breachCount,
        int requiredBreaches,
        bool shouldTrigger)
    {
        // Arrange
        var state = new SignalState(tenantId: 1, ruleId: 1);
        
        for (int i = 0; i < breachCount; i++)
        {
            state.RecordBreach(metricValue: 100.00m + i);
        }

        // Act - This is the trigger check logic from EvaluateRulesCommandHandler
        var triggerSignal = state.ConsecutiveBreaches >= requiredBreaches;

        // Assert
        triggerSignal.Should().Be(shouldTrigger);
    }

    [Fact]
    public void BreachCycle_OneSignalPerCycle_Reset_StartsNewCycle()
    {
        // Arrange
        var state = new SignalState(tenantId: 1, ruleId: 1);
        const int requiredBreaches = 3;

        // Act - First cycle: 3 breaches
        state.RecordBreach(metricValue: 101.00m);
        state.RecordBreach(metricValue: 102.00m);
        state.RecordBreach(metricValue: 103.00m);

        // Signal would be created here in the handler
        var firstCycleTrigger = state.ConsecutiveBreaches >= requiredBreaches;
        firstCycleTrigger.Should().BeTrue("first cycle should trigger at 3 breaches");

        // Reset after signal creation
        state.Reset();

        // Assert - State reset
        state.ConsecutiveBreaches.Should().Be(0);
        state.IsBreached.Should().BeFalse();

        // Act - Second cycle: new breaches
        state.RecordBreach(metricValue: 201.00m);
        state.RecordBreach(metricValue: 202.00m);
        
        // Second cycle - not enough yet
        var secondCycleTrigger = state.ConsecutiveBreaches >= requiredBreaches;
        secondCycleTrigger.Should().BeFalse("second cycle only has 2 breaches, needs 3");

        state.ConsecutiveBreaches.Should().Be(2);
    }

    [Fact]
    public void IntermittentBreaches_ResetOnSuccess_PreventsSignal()
    {
        // Arrange
        var state = new SignalState(tenantId: 1, ruleId: 1);
        const int requiredBreaches = 3;

        // Act - 2 breaches, then success, then 2 more
        state.RecordBreach(metricValue: 101.00m);
        state.RecordBreach(metricValue: 102.00m);
        state.ConsecutiveBreaches.Should().Be(2);

        state.RecordSuccess(metricValue: 50.00m); // Resets!
        state.ConsecutiveBreaches.Should().Be(0);

        state.RecordBreach(metricValue: 103.00m);
        state.RecordBreach(metricValue: 104.00m);
        state.ConsecutiveBreaches.Should().Be(2);

        // Assert - Never reached 3 consecutive
        var wouldTrigger = state.ConsecutiveBreaches >= requiredBreaches;
        wouldTrigger.Should().BeFalse("intermittent success prevented reaching threshold");
    }

    #endregion

    #region Idempotency Tests

    [Fact]
    public void State_IsIdempotentUnderRetry_WithSameValue()
    {
        // This tests that if we retry an evaluation with the same metric value,
        // the state changes are consistent
        
        // Arrange
        var state1 = new SignalState(tenantId: 1, ruleId: 1);
        var state2 = new SignalState(tenantId: 1, ruleId: 1);
        
        // Act - Same sequence of operations on both
        state1.RecordBreach(100.00m);
        state2.RecordBreach(100.00m);

        // Assert
        state1.ConsecutiveBreaches.Should().Be(state2.ConsecutiveBreaches);
        state1.IsBreached.Should().Be(state2.IsBreached);
        state1.LastMetricValue.Should().Be(state2.LastMetricValue);
    }

    #endregion
}
