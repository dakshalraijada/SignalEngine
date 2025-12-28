using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Application.Rules.Commands;
using SignalEngine.Domain.Constants;
using SignalEngine.Domain.Entities;

namespace SignalEngine.Application.Tests.Rules.Commands;

/// <summary>
/// Tests for EvaluateRulesCommandHandler.
/// 
/// === QUEUE-ONLY NOTIFICATION SEMANTICS ===
/// These tests verify the architectural contract:
/// - Notification creation = DB INSERT only (queue)
/// - NotificationDispatcher is NEVER called from this handler
/// - NotificationWorker is the SOLE dispatcher
/// </summary>
public class EvaluateRulesCommandHandlerTests
{
    private readonly Mock<IRuleEvaluationRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<EvaluateRulesCommandHandler>> _loggerMock;
    private readonly EvaluateRulesCommandHandler _handler;

    public EvaluateRulesCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRuleEvaluationRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<EvaluateRulesCommandHandler>>();
        
        _handler = new EvaluateRulesCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_NoActiveRules_ReturnsEmptyResult()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetActiveRulesWithDependenciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rule>());

        // Act
        var result = await _handler.Handle(new EvaluateRulesCommand(), CancellationToken.None);

        // Assert
        result.RulesEvaluated.Should().Be(0);
        result.SignalsCreated.Should().Be(0);
        result.RulesSkipped.Should().Be(0);
        result.Errors.Should().Be(0);
    }

    [Fact]
    public async Task Handle_RuleWithNoMetricData_SkipsRule()
    {
        // Arrange
        var rule = CreateTestRule();
        
        _repositoryMock.Setup(r => r.GetActiveRulesWithDependenciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rule> { rule });
        _repositoryMock.Setup(r => r.ResolveLookupIdAsync(LookupTypeCodes.SignalStatus, SignalStatusCodes.Open, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _repositoryMock.Setup(r => r.ResolveLookupIdAsync(LookupTypeCodes.NotificationChannelType, NotificationChannelTypeCodes.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _repositoryMock.Setup(r => r.GetLatestMetricValueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MetricData?)null);

        // Act
        var result = await _handler.Handle(new EvaluateRulesCommand(), CancellationToken.None);

        // Assert
        result.RulesSkipped.Should().Be(1);
        result.SignalsCreated.Should().Be(0);
    }

    [Fact]
    public async Task Handle_BreachMeetsThreshold_CreatesSignalAndQueuesNotification()
    {
        // Arrange
        var rule = CreateTestRule(threshold: 100, consecutiveBreachesRequired: 1);
        var metric = CreateTestMetric();
        var metricData = CreateTestMetricData(metric, value: 150m); // Above threshold
        var signalState = CreateTestSignalState(rule);

        SetupRepositoryForBreachScenario(rule, metricData, signalState);

        Notification? capturedNotification = null;
        _repositoryMock.Setup(r => r.AddNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback<Notification, CancellationToken>((n, _) => capturedNotification = n)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(new EvaluateRulesCommand(), CancellationToken.None);

        // Assert
        result.SignalsCreated.Should().Be(1);
        
        // CRITICAL: Verify notification was QUEUED (AddNotificationAsync was called)
        _repositoryMock.Verify(r => r.AddNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
        
        // CRITICAL: Verify notification exists with expected properties
        capturedNotification.Should().NotBeNull();
        capturedNotification!.TenantId.Should().Be(rule.TenantId);
        capturedNotification.IsSent.Should().BeFalse(); // NOT sent - just queued
        capturedNotification.RetryCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ConsecutiveBreachesNotMet_DoesNotCreateSignalOrNotification()
    {
        // Arrange
        var rule = CreateTestRule(threshold: 100, consecutiveBreachesRequired: 3);
        var metric = CreateTestMetric();
        var metricData = CreateTestMetricData(metric, value: 150m); // Above threshold
        var signalState = CreateTestSignalState(rule, consecutiveBreaches: 1); // Only 1, needs 3

        SetupRepositoryForBreachScenario(rule, metricData, signalState);

        // Act
        var result = await _handler.Handle(new EvaluateRulesCommand(), CancellationToken.None);

        // Assert
        result.SignalsCreated.Should().Be(0);
        
        // CRITICAL: No notification should be queued
        _repositoryMock.Verify(r => r.AddNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ConditionNotBreached_DoesNotCreateSignalOrNotification()
    {
        // Arrange
        var rule = CreateTestRule(threshold: 100, consecutiveBreachesRequired: 1);
        var metric = CreateTestMetric();
        var metricData = CreateTestMetricData(metric, value: 50m); // Below threshold
        var signalState = CreateTestSignalState(rule);

        SetupRepositoryForBreachScenario(rule, metricData, signalState);

        // Act
        var result = await _handler.Handle(new EvaluateRulesCommand(), CancellationToken.None);

        // Assert
        result.SignalsCreated.Should().Be(0);
        result.RulesEvaluated.Should().Be(1);
        
        // CRITICAL: No notification should be queued
        _repositoryMock.Verify(r => r.AddNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SignalCreated_SavesBeforeNotificationForForeignKey()
    {
        // Arrange
        var rule = CreateTestRule(threshold: 100, consecutiveBreachesRequired: 1);
        var metric = CreateTestMetric();
        var metricData = CreateTestMetricData(metric, value: 150m);
        var signalState = CreateTestSignalState(rule);

        var saveCallOrder = new List<string>();
        
        // Setup basic lookups
        _repositoryMock.Setup(r => r.GetActiveRulesWithDependenciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rule> { rule });
        _repositoryMock.Setup(r => r.ResolveLookupIdAsync(LookupTypeCodes.SignalStatus, SignalStatusCodes.Open, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _repositoryMock.Setup(r => r.ResolveLookupIdAsync(LookupTypeCodes.NotificationChannelType, NotificationChannelTypeCodes.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _repositoryMock.Setup(r => r.GetLatestMetricValueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metricData);
        _repositoryMock.Setup(r => r.GetOrCreateSignalStateAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(signalState);
        _repositoryMock.Setup(r => r.ResolveLookupCodeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("GT");
        
        // Track call order and simulate EF Core ID assignment
        Signal? capturedSignal = null;
        _repositoryMock.Setup(r => r.AddSignalAsync(It.IsAny<Signal>(), It.IsAny<CancellationToken>()))
            .Callback<Signal, CancellationToken>((s, _) =>
            {
                saveCallOrder.Add("AddSignal");
                capturedSignal = s;
            })
            .Returns(Task.CompletedTask);
            
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                saveCallOrder.Add("SaveChanges");
                // Simulate EF Core setting the signal ID on save
                if (capturedSignal != null)
                {
                    SetEntityId(capturedSignal, 100);
                }
            })
            .ReturnsAsync(1);
            
        _repositoryMock.Setup(r => r.AddNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Callback(() => saveCallOrder.Add("AddNotification"))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(new EvaluateRulesCommand(), CancellationToken.None);

        // Assert: Signal is added, then saved (to get ID), then notification is queued
        // Verify order: AddSignal comes before SaveChanges which comes before AddNotification
        var addSignalIndex = saveCallOrder.IndexOf("AddSignal");
        var firstSaveIndex = saveCallOrder.IndexOf("SaveChanges");
        var addNotificationIndex = saveCallOrder.IndexOf("AddNotification");
        
        addSignalIndex.Should().BeGreaterThanOrEqualTo(0, "Signal should be added");
        firstSaveIndex.Should().BeGreaterThan(addSignalIndex, "First save should happen after signal is added");
        addNotificationIndex.Should().BeGreaterThan(firstSaveIndex, "Notification should be added after first save");
    }

    #region Helper Methods

    private static Rule CreateTestRule(decimal threshold = 100m, int consecutiveBreachesRequired = 1)
    {
        // Create lookup values with valid constructor
        var operatorLookup = new LookupValue(1, "GT", "Greater Than");
        var severityLookup = new LookupValue(2, "HIGH", "High Severity");

        // Use the actual constructor
        var rule = new Rule(
            tenantId: 1,
            assetId: 1,
            name: "Test Rule",
            metricName: "price",
            operatorId: 1,
            threshold: threshold,
            severityId: 2,
            evaluationFrequencyId: 1,
            consecutiveBreachesRequired: consecutiveBreachesRequired,
            description: "Test rule description");
        
        // Set navigation properties via reflection (they're set by EF Core normally)
        SetPrivateProperty(rule, "Operator", operatorLookup);
        SetPrivateProperty(rule, "Severity", severityLookup);
        
        // Set Id via base class
        SetEntityId(rule, 1);

        return rule;
    }

    private static Metric CreateTestMetric()
    {
        // Use reflection to create metric as it has private constructor
        var metric = (Metric)Activator.CreateInstance(typeof(Metric), true)!;
        SetEntityId(metric, 1);
        SetPrivateProperty(metric, "AssetId", 1);
        SetPrivateProperty(metric, "Name", "price");
        SetPrivateProperty(metric, "IsActive", true);
        return metric;
    }

    private static MetricData CreateTestMetricData(Metric metric, decimal value)
    {
        // Use reflection to create metric data as it has private constructor
        var metricData = (MetricData)Activator.CreateInstance(typeof(MetricData), true)!;
        SetEntityId(metricData, 1);
        SetPrivateProperty(metricData, "MetricId", metric.Id);
        SetPrivateProperty(metricData, "Metric", metric);
        SetPrivateProperty(metricData, "Value", value);
        SetPrivateProperty(metricData, "Timestamp", DateTime.UtcNow);
        return metricData;
    }

    private static SignalState CreateTestSignalState(Rule rule, int consecutiveBreaches = 0)
    {
        var state = new SignalState(rule.TenantId, rule.Id);
        // Set consecutive breaches if needed
        for (int i = 0; i < consecutiveBreaches; i++)
        {
            state.RecordBreach(100m);
        }
        return state;
    }
    
    private static void SetPrivateProperty<T>(object obj, string propertyName, T value)
    {
        var property = obj.GetType().GetProperty(propertyName);
        if (property != null)
        {
            property.SetValue(obj, value);
        }
    }
    
    private static void SetEntityId(object entity, int id)
    {
        // The Id property is defined in the base Entity class
        var idProperty = entity.GetType().BaseType?.BaseType?.GetProperty("Id")
                      ?? entity.GetType().BaseType?.GetProperty("Id")
                      ?? entity.GetType().GetProperty("Id");
        
        if (idProperty != null)
        {
            // Use reflection to access the backing field since setter might be private
            var backingField = entity.GetType().BaseType?.BaseType?.GetField("<Id>k__BackingField", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?? entity.GetType().BaseType?.GetField("<Id>k__BackingField", 
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            
            if (backingField != null)
            {
                backingField.SetValue(entity, id);
            }
        }
    }

    private void SetupRepositoryForBreachScenario(Rule rule, MetricData metricData, SignalState signalState)
    {
        _repositoryMock.Setup(r => r.GetActiveRulesWithDependenciesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rule> { rule });
        _repositoryMock.Setup(r => r.ResolveLookupIdAsync(LookupTypeCodes.SignalStatus, SignalStatusCodes.Open, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _repositoryMock.Setup(r => r.ResolveLookupIdAsync(LookupTypeCodes.NotificationChannelType, NotificationChannelTypeCodes.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _repositoryMock.Setup(r => r.GetLatestMetricValueAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metricData);
        _repositoryMock.Setup(r => r.GetOrCreateSignalStateAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(signalState);
        _repositoryMock.Setup(r => r.ResolveLookupCodeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("GT");
        
        // Capture the signal so we can set its ID (simulating EF Core behavior)
        Signal? capturedSignal = null;
        _repositoryMock.Setup(r => r.AddSignalAsync(It.IsAny<Signal>(), It.IsAny<CancellationToken>()))
            .Callback<Signal, CancellationToken>((s, _) => capturedSignal = s)
            .Returns(Task.CompletedTask);
        
        // Simulate EF Core setting the signal ID on SaveChanges
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => 
            {
                if (capturedSignal != null)
                {
                    // Use reflection to set the Id property (simulating EF Core)
                    SetEntityId(capturedSignal, 100); // Set to a valid ID
                }
            })
            .ReturnsAsync(1);
            
        _repositoryMock.Setup(r => r.AddNotificationAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    #endregion
}
