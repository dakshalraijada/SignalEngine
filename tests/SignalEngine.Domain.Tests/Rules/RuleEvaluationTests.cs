using FluentAssertions;
using SignalEngine.Domain.Entities;
using Xunit;

namespace SignalEngine.Domain.Tests.Rules;

/// <summary>
/// Domain-level tests for Rule evaluation logic.
/// Tests the Rule.Evaluate method with all operators and boundary conditions.
/// 
/// TESTING PRINCIPLES:
/// - No EF Core, no database, no mocks
/// - Pure domain logic verification
/// - Focus on correctness over coverage
/// </summary>
public class RuleEvaluationTests
{
    #region Greater Than (GT) Operator Tests

    [Theory]
    [InlineData(100.01, 100.00, true)]   // Just above threshold
    [InlineData(150.00, 100.00, true)]   // Well above threshold
    [InlineData(100.00, 100.00, false)]  // Exactly at threshold (GT should be false)
    [InlineData(99.99, 100.00, false)]   // Just below threshold
    [InlineData(0.00, 100.00, false)]    // Zero value
    public void Evaluate_GreaterThan_ReturnsExpectedResult(decimal metricValue, decimal threshold, bool expectedResult)
    {
        // Arrange
        var rule = CreateRule(threshold);

        // Act
        var result = rule.Evaluate("GT", metricValue);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("gt")]   // lowercase
    [InlineData("Gt")]   // mixed case
    [InlineData("gT")]   // mixed case reverse
    public void Evaluate_GreaterThan_IsCaseInsensitive(string operatorCode)
    {
        // Arrange
        var rule = CreateRule(threshold: 100.00m);

        // Act
        var result = rule.Evaluate(operatorCode, metricValue: 150.00m);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Greater Than or Equal (GTE) Operator Tests

    [Theory]
    [InlineData(100.01, 100.00, true)]   // Just above threshold
    [InlineData(100.00, 100.00, true)]   // Exactly at threshold (GTE should be true)
    [InlineData(99.99, 100.00, false)]   // Just below threshold
    [InlineData(0.00, 100.00, false)]    // Zero value
    public void Evaluate_GreaterThanOrEqual_ReturnsExpectedResult(decimal metricValue, decimal threshold, bool expectedResult)
    {
        // Arrange
        var rule = CreateRule(threshold);

        // Act
        var result = rule.Evaluate("GTE", metricValue);

        // Assert
        result.Should().Be(expectedResult);
    }

    #endregion

    #region Less Than (LT) Operator Tests

    [Theory]
    [InlineData(99.99, 100.00, true)]    // Just below threshold
    [InlineData(50.00, 100.00, true)]    // Well below threshold
    [InlineData(100.00, 100.00, false)]  // Exactly at threshold (LT should be false)
    [InlineData(100.01, 100.00, false)]  // Just above threshold
    [InlineData(0.00, 100.00, true)]     // Zero value
    public void Evaluate_LessThan_ReturnsExpectedResult(decimal metricValue, decimal threshold, bool expectedResult)
    {
        // Arrange
        var rule = CreateRule(threshold);

        // Act
        var result = rule.Evaluate("LT", metricValue);

        // Assert
        result.Should().Be(expectedResult);
    }

    #endregion

    #region Less Than or Equal (LTE) Operator Tests

    [Theory]
    [InlineData(99.99, 100.00, true)]    // Just below threshold
    [InlineData(100.00, 100.00, true)]   // Exactly at threshold (LTE should be true)
    [InlineData(100.01, 100.00, false)]  // Just above threshold
    [InlineData(0.00, 100.00, true)]     // Zero value
    public void Evaluate_LessThanOrEqual_ReturnsExpectedResult(decimal metricValue, decimal threshold, bool expectedResult)
    {
        // Arrange
        var rule = CreateRule(threshold);

        // Act
        var result = rule.Evaluate("LTE", metricValue);

        // Assert
        result.Should().Be(expectedResult);
    }

    #endregion

    #region Equal (EQ) Operator Tests

    [Theory]
    [InlineData(100.00, 100.00, true)]   // Exactly at threshold
    [InlineData(100.01, 100.00, false)]  // Just above threshold
    [InlineData(99.99, 100.00, false)]   // Just below threshold
    [InlineData(0.00, 0.00, true)]       // Zero equals zero
    public void Evaluate_Equal_ReturnsExpectedResult(decimal metricValue, decimal threshold, bool expectedResult)
    {
        // Arrange
        var rule = CreateRule(threshold);

        // Act
        var result = rule.Evaluate("EQ", metricValue);

        // Assert
        result.Should().Be(expectedResult);
    }

    #endregion

    #region Unknown Operator Tests

    [Theory]
    [InlineData("UNKNOWN")]
    [InlineData("NE")]     // Not Equal - not supported
    [InlineData("")]
    [InlineData(" ")]
    public void Evaluate_UnknownOperator_ReturnsFalse(string operatorCode)
    {
        // Arrange
        var rule = CreateRule(threshold: 100.00m);

        // Act
        var result = rule.Evaluate(operatorCode, metricValue: 100.00m);

        // Assert
        result.Should().BeFalse("unknown operators should not trigger breaches");
    }

    #endregion

    #region Decimal Precision Tests

    [Theory]
    [InlineData("0.0000000001", "0.0000000000", "GT", true)]   // Very small positive
    [InlineData("0.0000000000", "0.0000000001", "LT", true)]   // Very small negative
    [InlineData("999999999999.999999", "999999999999.999999", "EQ", true)]  // Large numbers
    [InlineData("0.123456789012345678", "0.123456789012345678", "EQ", true)] // High precision
    public void Evaluate_DecimalPrecision_HandledCorrectly(string metricValueStr, string thresholdStr, string operatorCode, bool expected)
    {
        // Arrange
        var metricValue = decimal.Parse(metricValueStr);
        var threshold = decimal.Parse(thresholdStr);
        var rule = CreateRule(threshold);

        // Act
        var result = rule.Evaluate(operatorCode, metricValue);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Evaluate_NegativeValues_HandledCorrectly()
    {
        // Arrange
        var rule = CreateRule(threshold: -50.00m);

        // Act & Assert
        rule.Evaluate("GT", -49.00m).Should().BeTrue("−49 > −50");
        rule.Evaluate("GT", -51.00m).Should().BeFalse("−51 < −50");
        rule.Evaluate("LT", -51.00m).Should().BeTrue("−51 < −50");
        rule.Evaluate("EQ", -50.00m).Should().BeTrue("−50 = −50");
    }

    #endregion

    #region Boundary Condition Tests

    [Fact]
    public void Evaluate_DecimalMinMax_HandledCorrectly()
    {
        // Arrange - MaxValue threshold
        var ruleMax = CreateRule(threshold: decimal.MaxValue);

        // Only decimal.MaxValue should breach GTE
        ruleMax.Evaluate("GTE", decimal.MaxValue).Should().BeTrue();
        ruleMax.Evaluate("GT", decimal.MaxValue).Should().BeFalse();
        ruleMax.Evaluate("LT", decimal.MaxValue).Should().BeFalse();

        // Arrange - MinValue threshold
        var ruleMin = CreateRule(threshold: decimal.MinValue);

        // Only decimal.MinValue should breach LTE
        ruleMin.Evaluate("LTE", decimal.MinValue).Should().BeTrue();
        ruleMin.Evaluate("LT", decimal.MinValue).Should().BeFalse();
        ruleMin.Evaluate("GT", decimal.MinValue).Should().BeFalse();
    }

    [Fact]
    public void Evaluate_ZeroThreshold_HandledCorrectly()
    {
        // Arrange
        var rule = CreateRule(threshold: 0.00m);

        // Act & Assert
        rule.Evaluate("GT", 0.01m).Should().BeTrue();
        rule.Evaluate("GT", 0.00m).Should().BeFalse();
        rule.Evaluate("GT", -0.01m).Should().BeFalse();
        rule.Evaluate("LT", -0.01m).Should().BeTrue();
        rule.Evaluate("EQ", 0.00m).Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a minimal valid Rule for testing.
    /// Uses placeholder IDs since we're only testing the Evaluate method.
    /// </summary>
    private static Rule CreateRule(decimal threshold)
    {
        return new Rule(
            tenantId: 1,
            assetId: 1,
            name: "Test Rule",
            metricName: "test_metric",
            operatorId: 1,      // Not used by Evaluate - code is passed separately
            threshold: threshold,
            severityId: 1,
            evaluationFrequencyId: 1,
            consecutiveBreachesRequired: 1);
    }

    #endregion
}
