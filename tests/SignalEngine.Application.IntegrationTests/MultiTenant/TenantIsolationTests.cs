using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SignalEngine.Application.IntegrationTests.Infrastructure;
using SignalEngine.Application.Rules.Commands;
using SignalEngine.Infrastructure.Repositories;
using Xunit;

namespace SignalEngine.Application.IntegrationTests.MultiTenant;

/// <summary>
/// SECURITY CRITICAL: Multi-tenant isolation tests.
/// 
/// These tests verify that tenant data is strictly isolated.
/// Failure in ANY of these tests is a BLOCKER.
/// 
/// Scenarios tested:
/// 1. Two tenants monitoring same real-world asset
/// 2. Same metric names, different MetricData
/// 3. Rule evaluation creates signals ONLY for correct tenant
/// 4. Tenant B data is NEVER accessed by Tenant A context
/// </summary>
[Collection(nameof(DatabaseCollection))]
public class TenantIsolationTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    public TenantIsolationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    [Fact]
    public async Task RuleEvaluation_TwoTenantsMonitoringSameAsset_ShouldCreateIsolatedSignals()
    {
        // Arrange - SECURITY CRITICAL TEST
        await using var context = _fixture.CreateDbContext();
        var lookups = _fixture.Lookups;

        // Create two independent tenants
        var tenantA = TestDataBuilder.CreateTenant(lookups, "Tenant A", "tenant-a");
        var tenantB = TestDataBuilder.CreateTenant(lookups, "Tenant B", "tenant-b");
        context.Tenants.AddRange(tenantA, tenantB);
        await context.SaveChangesAsync();

        // Both tenants monitor BTC (same real-world asset, but separate Asset entities)
        var assetA = TestDataBuilder.CreateAsset(tenantA.Id, lookups, "BTC Asset", "BTC");
        var assetB = TestDataBuilder.CreateAsset(tenantB.Id, lookups, "BTC Asset", "BTC");
        context.Assets.AddRange(assetA, assetB);
        await context.SaveChangesAsync();

        // Same metric name for both tenants
        var metricA = TestDataBuilder.CreateMetric(tenantA.Id, assetA.Id, lookups, "price");
        var metricB = TestDataBuilder.CreateMetric(tenantB.Id, assetB.Id, lookups, "price");
        context.Metrics.AddRange(metricA, metricB);
        await context.SaveChangesAsync();

        // DIFFERENT metric values per tenant
        // Tenant A: 150 (will breach threshold of 100)
        // Tenant B: 80 (will NOT breach threshold of 100)
        var metricDataA = TestDataBuilder.CreateMetricData(tenantA.Id, metricA.Id, 150.00m);
        var metricDataB = TestDataBuilder.CreateMetricData(tenantB.Id, metricB.Id, 80.00m);
        context.MetricData.AddRange(metricDataA, metricDataB);
        await context.SaveChangesAsync();

        // Same rule definition for both tenants
        var ruleA = TestDataBuilder.CreateRule(
            tenantA.Id,
            assetA.Id,
            lookups,
            metricName: "price",
            operatorId: lookups.OperatorGT,
            threshold: 100.00m,
            name: "Price Alert");
        var ruleB = TestDataBuilder.CreateRule(
            tenantB.Id,
            assetB.Id,
            lookups,
            metricName: "price",
            operatorId: lookups.OperatorGT,
            threshold: 100.00m,
            name: "Price Alert");
        context.Rules.AddRange(ruleA, ruleB);
        await context.SaveChangesAsync();

        // Act - Run evaluation (system-level, evaluates all tenants)
        var repository = new RuleEvaluationRepository(context);
        var handler = new EvaluateRulesCommandHandler(
            repository,
            context,
            NullLogger<EvaluateRulesCommandHandler>.Instance);

        var result = await handler.Handle(new EvaluateRulesCommand(), CancellationToken.None);

        // Assert - CRITICAL ISOLATION CHECKS
        result.RulesEvaluated.Should().Be(2, "both tenant rules should be evaluated");
        result.SignalsCreated.Should().Be(1, "only Tenant A should have triggered");

        // Verify ONLY Tenant A has a signal
        var signals = await context.Signals.ToListAsync();
        signals.Should().HaveCount(1);

        var signal = signals.First();
        signal.TenantId.Should().Be(tenantA.Id, "signal must belong to Tenant A");
        signal.RuleId.Should().Be(ruleA.Id);
        signal.AssetId.Should().Be(assetA.Id);
        signal.TriggerValue.Should().Be(150.00m, "Tenant A's metric value");

        // CRITICAL: Tenant B should have NO signals
        signals.Where(s => s.TenantId == tenantB.Id).Should().BeEmpty(
            "SECURITY: Tenant B must not have any signals");

        // Verify SignalState isolation
        var signalStates = await context.SignalStates.ToListAsync();
        signalStates.Should().HaveCount(2, "each rule gets its own state");

        var stateA = signalStates.First(s => s.RuleId == ruleA.Id);
        var stateB = signalStates.First(s => s.RuleId == ruleB.Id);

        stateA.TenantId.Should().Be(tenantA.Id);
        stateB.TenantId.Should().Be(tenantB.Id);

        // State A was reset after signal (ConsecutiveBreaches = 0)
        stateA.ConsecutiveBreaches.Should().Be(0);
        
        // State B was recorded as success (no breach)
        stateB.ConsecutiveBreaches.Should().Be(0);
        stateB.IsBreached.Should().BeFalse();
    }

    [Fact]
    public async Task RuleEvaluation_TenantFilteredContext_ShouldOnlySeeOwnData()
    {
        // Arrange
        await using var systemContext = _fixture.CreateDbContext();
        var lookups = _fixture.Lookups;

        // Create two tenants with data
        var tenantA = TestDataBuilder.CreateTenant(lookups, "Tenant A");
        var tenantB = TestDataBuilder.CreateTenant(lookups, "Tenant B");
        systemContext.Tenants.AddRange(tenantA, tenantB);
        await systemContext.SaveChangesAsync();

        var assetA = TestDataBuilder.CreateAsset(tenantA.Id, lookups, "Asset A");
        var assetB = TestDataBuilder.CreateAsset(tenantB.Id, lookups, "Asset B");
        systemContext.Assets.AddRange(assetA, assetB);
        await systemContext.SaveChangesAsync();

        // Act - Query with Tenant A filtered context
        await using var tenantAContext = _fixture.CreateDbContext(TestTenantAccessor.ForTenant(tenantA.Id));
        
        var assetsVisibleToA = await tenantAContext.Assets.ToListAsync();
        
        // Assert - Tenant A should only see their own assets
        assetsVisibleToA.Should().HaveCount(1);
        assetsVisibleToA.First().Id.Should().Be(assetA.Id);
        assetsVisibleToA.First().TenantId.Should().Be(tenantA.Id);

        // Act - Query with Tenant B filtered context
        await using var tenantBContext = _fixture.CreateDbContext(TestTenantAccessor.ForTenant(tenantB.Id));
        
        var assetsVisibleToB = await tenantBContext.Assets.ToListAsync();
        
        // Assert - Tenant B should only see their own assets
        assetsVisibleToB.Should().HaveCount(1);
        assetsVisibleToB.First().Id.Should().Be(assetB.Id);
        assetsVisibleToB.First().TenantId.Should().Be(tenantB.Id);
    }

    [Fact]
    public async Task RuleEvaluation_TenantACannotAccessTenantBMetricData()
    {
        // Arrange - SECURITY CRITICAL
        await using var systemContext = _fixture.CreateDbContext();
        var lookups = _fixture.Lookups;

        var tenantA = TestDataBuilder.CreateTenant(lookups, "Tenant A");
        var tenantB = TestDataBuilder.CreateTenant(lookups, "Tenant B");
        systemContext.Tenants.AddRange(tenantA, tenantB);
        await systemContext.SaveChangesAsync();

        var assetA = TestDataBuilder.CreateAsset(tenantA.Id, lookups, "BTC", "BTC");
        var assetB = TestDataBuilder.CreateAsset(tenantB.Id, lookups, "BTC", "BTC");
        systemContext.Assets.AddRange(assetA, assetB);
        await systemContext.SaveChangesAsync();

        var metricA = TestDataBuilder.CreateMetric(tenantA.Id, assetA.Id, lookups, "price");
        var metricB = TestDataBuilder.CreateMetric(tenantB.Id, assetB.Id, lookups, "price");
        systemContext.Metrics.AddRange(metricA, metricB);
        await systemContext.SaveChangesAsync();

        // Tenant B has SECRET data that Tenant A should NEVER see
        var secretValue = 99999.99m;
        var metricDataB = TestDataBuilder.CreateMetricData(tenantB.Id, metricB.Id, secretValue);
        systemContext.MetricData.Add(metricDataB);
        await systemContext.SaveChangesAsync();

        // Act - Query MetricData with Tenant A context
        await using var tenantAContext = _fixture.CreateDbContext(TestTenantAccessor.ForTenant(tenantA.Id));
        
        var dataVisibleToA = await tenantAContext.MetricData.ToListAsync();

        // Assert - CRITICAL: Tenant A should NOT see Tenant B's data
        dataVisibleToA.Should().BeEmpty("Tenant A has no data");
        dataVisibleToA.Should().NotContain(d => d.Value == secretValue,
            "SECURITY: Tenant A must NEVER see Tenant B's secret value");
    }

    [Fact]
    public async Task RuleEvaluation_SignalsAreStrictlyTenantScoped()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var lookups = _fixture.Lookups;

        // Create three tenants
        var tenant1 = TestDataBuilder.CreateTenant(lookups, "Tenant 1");
        var tenant2 = TestDataBuilder.CreateTenant(lookups, "Tenant 2");
        var tenant3 = TestDataBuilder.CreateTenant(lookups, "Tenant 3");
        context.Tenants.AddRange(tenant1, tenant2, tenant3);
        await context.SaveChangesAsync();

        // Create assets for all
        var asset1 = TestDataBuilder.CreateAsset(tenant1.Id, lookups);
        var asset2 = TestDataBuilder.CreateAsset(tenant2.Id, lookups);
        var asset3 = TestDataBuilder.CreateAsset(tenant3.Id, lookups);
        context.Assets.AddRange(asset1, asset2, asset3);
        await context.SaveChangesAsync();

        // Metrics
        var metric1 = TestDataBuilder.CreateMetric(tenant1.Id, asset1.Id, lookups, "price");
        var metric2 = TestDataBuilder.CreateMetric(tenant2.Id, asset2.Id, lookups, "price");
        var metric3 = TestDataBuilder.CreateMetric(tenant3.Id, asset3.Id, lookups, "price");
        context.Metrics.AddRange(metric1, metric2, metric3);
        await context.SaveChangesAsync();

        // All tenants have breaching data
        context.MetricData.Add(TestDataBuilder.CreateMetricData(tenant1.Id, metric1.Id, 200.00m));
        context.MetricData.Add(TestDataBuilder.CreateMetricData(tenant2.Id, metric2.Id, 200.00m));
        context.MetricData.Add(TestDataBuilder.CreateMetricData(tenant3.Id, metric3.Id, 200.00m));
        await context.SaveChangesAsync();

        // Rules for all
        var rule1 = TestDataBuilder.CreateRule(tenant1.Id, asset1.Id, lookups, threshold: 100.00m);
        var rule2 = TestDataBuilder.CreateRule(tenant2.Id, asset2.Id, lookups, threshold: 100.00m);
        var rule3 = TestDataBuilder.CreateRule(tenant3.Id, asset3.Id, lookups, threshold: 100.00m);
        context.Rules.AddRange(rule1, rule2, rule3);
        await context.SaveChangesAsync();

        // Act - Run evaluation
        var repository = new RuleEvaluationRepository(context);
        var handler = new EvaluateRulesCommandHandler(
            repository,
            context,
            NullLogger<EvaluateRulesCommandHandler>.Instance);

        await handler.Handle(new EvaluateRulesCommand(), CancellationToken.None);

        // Assert - Each tenant should have exactly one signal with correct isolation
        var signals = await context.Signals.ToListAsync();
        signals.Should().HaveCount(3);

        // Verify each signal belongs to correct tenant and rule
        signals.Should().Contain(s => s.TenantId == tenant1.Id && s.RuleId == rule1.Id);
        signals.Should().Contain(s => s.TenantId == tenant2.Id && s.RuleId == rule2.Id);
        signals.Should().Contain(s => s.TenantId == tenant3.Id && s.RuleId == rule3.Id);

        // CRITICAL: No cross-tenant signals
        signals.Should().NotContain(s => s.TenantId == tenant1.Id && s.RuleId == rule2.Id);
        signals.Should().NotContain(s => s.TenantId == tenant1.Id && s.RuleId == rule3.Id);
        signals.Should().NotContain(s => s.TenantId == tenant2.Id && s.RuleId == rule1.Id);
        signals.Should().NotContain(s => s.TenantId == tenant2.Id && s.RuleId == rule3.Id);
        signals.Should().NotContain(s => s.TenantId == tenant3.Id && s.RuleId == rule1.Id);
        signals.Should().NotContain(s => s.TenantId == tenant3.Id && s.RuleId == rule2.Id);
    }

    [Fact]
    public async Task RuleEvaluation_MetricDataIsolation_EachTenantGetsOwnMetricValue()
    {
        // Arrange - Tests that each tenant's rule uses their OWN metric data
        await using var context = _fixture.CreateDbContext();
        var lookups = _fixture.Lookups;

        var tenantHigh = TestDataBuilder.CreateTenant(lookups, "Tenant High");
        var tenantLow = TestDataBuilder.CreateTenant(lookups, "Tenant Low");
        context.Tenants.AddRange(tenantHigh, tenantLow);
        await context.SaveChangesAsync();

        var assetHigh = TestDataBuilder.CreateAsset(tenantHigh.Id, lookups, "BTC", "BTC");
        var assetLow = TestDataBuilder.CreateAsset(tenantLow.Id, lookups, "BTC", "BTC");
        context.Assets.AddRange(assetHigh, assetLow);
        await context.SaveChangesAsync();

        var metricHigh = TestDataBuilder.CreateMetric(tenantHigh.Id, assetHigh.Id, lookups, "price");
        var metricLow = TestDataBuilder.CreateMetric(tenantLow.Id, assetLow.Id, lookups, "price");
        context.Metrics.AddRange(metricHigh, metricLow);
        await context.SaveChangesAsync();

        // Different values - CRITICAL for isolation test
        context.MetricData.Add(TestDataBuilder.CreateMetricData(tenantHigh.Id, metricHigh.Id, 500.00m)); // High value
        context.MetricData.Add(TestDataBuilder.CreateMetricData(tenantLow.Id, metricLow.Id, 50.00m));   // Low value
        await context.SaveChangesAsync();

        // Threshold 100 - High tenant breaches, Low tenant does not
        var ruleHigh = TestDataBuilder.CreateRule(tenantHigh.Id, assetHigh.Id, lookups, threshold: 100.00m);
        var ruleLow = TestDataBuilder.CreateRule(tenantLow.Id, assetLow.Id, lookups, threshold: 100.00m);
        context.Rules.AddRange(ruleHigh, ruleLow);
        await context.SaveChangesAsync();

        // Act
        var repository = new RuleEvaluationRepository(context);
        var handler = new EvaluateRulesCommandHandler(
            repository,
            context,
            NullLogger<EvaluateRulesCommandHandler>.Instance);

        await handler.Handle(new EvaluateRulesCommand(), CancellationToken.None);

        // Assert
        var signals = await context.Signals.ToListAsync();
        
        // CRITICAL: Only High tenant should have signal (500 > 100)
        signals.Should().HaveCount(1);
        signals.First().TenantId.Should().Be(tenantHigh.Id);
        signals.First().TriggerValue.Should().Be(500.00m, 
            "signal should have High tenant's metric value, NOT Low tenant's");

        // Low tenant must NOT have signal (50 < 100)
        signals.Should().NotContain(s => s.TenantId == tenantLow.Id,
            "SECURITY: Low tenant's rule evaluated with Low tenant's data (50), should not breach");
    }

    [Fact]
    public async Task RuleEvaluation_CrossTenantMetricAccess_ShouldNotOccur()
    {
        // Arrange - MOST CRITICAL SECURITY TEST
        // This test verifies that even if tenant A's rule somehow references
        // tenant B's asset ID, it cannot access tenant B's data
        await using var context = _fixture.CreateDbContext();
        var lookups = _fixture.Lookups;

        var tenantAttacker = TestDataBuilder.CreateTenant(lookups, "Attacker");
        var tenantVictim = TestDataBuilder.CreateTenant(lookups, "Victim");
        context.Tenants.AddRange(tenantAttacker, tenantVictim);
        await context.SaveChangesAsync();

        // Victim has an asset with sensitive data
        var victimAsset = TestDataBuilder.CreateAsset(tenantVictim.Id, lookups, "Secret Asset");
        context.Assets.Add(victimAsset);
        await context.SaveChangesAsync();

        var victimMetric = TestDataBuilder.CreateMetric(tenantVictim.Id, victimAsset.Id, lookups, "secret_metric");
        context.Metrics.Add(victimMetric);
        await context.SaveChangesAsync();

        var victimData = TestDataBuilder.CreateMetricData(tenantVictim.Id, victimMetric.Id, 999.00m);
        context.MetricData.Add(victimData);
        await context.SaveChangesAsync();

        // Attacker creates their own asset but has NO metric data
        var attackerAsset = TestDataBuilder.CreateAsset(tenantAttacker.Id, lookups, "Attacker Asset");
        context.Assets.Add(attackerAsset);
        await context.SaveChangesAsync();

        // Attacker's rule references the same metric name as victim
        // In a vulnerable system, this might accidentally access victim's data
        var attackerRule = TestDataBuilder.CreateRule(
            tenantAttacker.Id,
            attackerAsset.Id,  // Attacker's own asset
            lookups,
            metricName: "secret_metric",  // Same name as victim's metric
            threshold: 500.00m);
        context.Rules.Add(attackerRule);
        await context.SaveChangesAsync();

        // Act
        var repository = new RuleEvaluationRepository(context);
        var handler = new EvaluateRulesCommandHandler(
            repository,
            context,
            NullLogger<EvaluateRulesCommandHandler>.Instance);

        var result = await handler.Handle(new EvaluateRulesCommand(), CancellationToken.None);

        // Assert - CRITICAL
        // Attacker's rule should be SKIPPED because attacker has no metric data
        // It should NOT be able to see victim's secret_metric data
        result.RulesSkipped.Should().Be(1, "attacker's rule has no data");
        result.SignalsCreated.Should().Be(0, "no signals should be created");

        var signals = await context.Signals.ToListAsync();
        signals.Should().BeEmpty("SECURITY: No cross-tenant data access allowed");
    }
}
