using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Application.IntegrationTests.Infrastructure;
using SignalEngine.Application.Metrics.Commands;
using SignalEngine.Infrastructure.Repositories;
using Xunit;

namespace SignalEngine.Application.IntegrationTests.Ingestion;

/// <summary>
/// Integration tests for Metric Ingestion.
/// Mock external data sources, NOT the database.
/// Uses real SQL Server via Testcontainers.
/// 
/// Test Scenarios:
/// 1. Fan-out ingestion - multiple tenants share same asset identifier
/// 2. Cursor behavior - asset only ingested when due
/// 3. Failure handling - provider failure doesn't corrupt data
/// </summary>
[Collection(nameof(DatabaseCollection))]
public class MetricIngestionIntegrationTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    public MetricIngestionIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    #region Fan-Out Ingestion Tests

    [Fact]
    public async Task Ingestion_WhenMultipleTenantsShareAsset_ShouldFanOutCorrectly()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var lookups = _fixture.Lookups;

        // Create two tenants monitoring the same real-world asset (BTC)
        var tenantA = TestDataBuilder.CreateTenant(lookups, "Tenant A");
        var tenantB = TestDataBuilder.CreateTenant(lookups, "Tenant B");
        context.Tenants.AddRange(tenantA, tenantB);
        await context.SaveChangesAsync();

        // Both tenants have assets tracking BTC with same identifier
        var assetA = TestDataBuilder.CreateAsset(tenantA.Id, lookups, "Bitcoin A", "BTC");
        var assetB = TestDataBuilder.CreateAsset(tenantB.Id, lookups, "Bitcoin B", "BTC");
        context.Assets.AddRange(assetA, assetB);
        await context.SaveChangesAsync();

        // Both have price metrics
        var metricA = TestDataBuilder.CreateMetric(tenantA.Id, assetA.Id, lookups, "price");
        var metricB = TestDataBuilder.CreateMetric(tenantB.Id, assetB.Id, lookups, "price");
        context.Metrics.AddRange(metricA, metricB);
        await context.SaveChangesAsync();

        // Setup fake provider
        var fakeFactory = new FakeDataSourceProviderFactory();
        var binanceProvider = fakeFactory.AddProvider("BINANCE");
        
        var fetchTimestamp = DateTime.UtcNow;
        binanceProvider.SetupSuccess("BTC", new List<FetchedMetricValue>
        {
            new("price", 45000.00m, fetchTimestamp)
        });

        // Act
        var repository = new IngestionRepository(context);
        var handler = new IngestMetricsCommandHandler(
            repository,
            fakeFactory,
            context,
            NullLogger<IngestMetricsCommandHandler>.Instance);

        var result = await handler.Handle(new IngestMetricsCommand(), CancellationToken.None);

        // Assert
        result.AssetsProcessed.Should().Be(2, "both tenants' assets processed");
        result.DataPointsCreated.Should().Be(2, "one data point per tenant");
        result.Errors.Should().Be(0);

        // CRITICAL: Provider should be called ONCE for batch, not twice
        binanceProvider.FetchCallCount.Should().Be(1, 
            "API should be called once for batch, not per-tenant");

        // Verify MetricData created for BOTH tenants
        var allData = await context.MetricData.Include(d => d.Metric).ToListAsync();
        allData.Should().HaveCount(2);

        // Tenant A's data
        var dataA = allData.First(d => d.TenantId == tenantA.Id);
        dataA.MetricId.Should().Be(metricA.Id);
        dataA.Value.Should().Be(45000.00m);
        dataA.Timestamp.Should().Be(fetchTimestamp);

        // Tenant B's data - SAME value but different MetricId
        var dataB = allData.First(d => d.TenantId == tenantB.Id);
        dataB.MetricId.Should().Be(metricB.Id);
        dataB.Value.Should().Be(45000.00m);
        dataB.Timestamp.Should().Be(fetchTimestamp);
    }

    [Fact]
    public async Task Ingestion_WhenTenantsHaveDifferentMetrics_ShouldCreateMatchingDataPoints()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var lookups = _fixture.Lookups;

        var tenant = TestDataBuilder.CreateTenant(lookups);
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        var asset = TestDataBuilder.CreateAsset(tenant.Id, lookups, "BTC", "BTC");
        context.Assets.Add(asset);
        await context.SaveChangesAsync();

        // Multiple metrics for same asset
        var priceMetric = TestDataBuilder.CreateMetric(tenant.Id, asset.Id, lookups, "price");
        var volumeMetric = TestDataBuilder.CreateMetric(tenant.Id, asset.Id, lookups, "volume");
        context.Metrics.AddRange(priceMetric, volumeMetric);
        await context.SaveChangesAsync();

        // Provider returns multiple metric values
        var fakeFactory = new FakeDataSourceProviderFactory();
        var provider = fakeFactory.AddProvider("BINANCE");
        provider.SetupSuccess("BTC", new List<FetchedMetricValue>
        {
            new("price", 45000.00m, DateTime.UtcNow),
            new("volume", 1000000.00m, DateTime.UtcNow),
            new("unknown_metric", 999.00m, DateTime.UtcNow)  // This should be ignored
        });

        // Act
        var repository = new IngestionRepository(context);
        var handler = new IngestMetricsCommandHandler(
            repository,
            fakeFactory,
            context,
            NullLogger<IngestMetricsCommandHandler>.Instance);

        var result = await handler.Handle(new IngestMetricsCommand(), CancellationToken.None);

        // Assert
        result.DataPointsCreated.Should().Be(2, "only defined metrics get data points");

        var allData = await context.MetricData.ToListAsync();
        allData.Should().HaveCount(2);
        allData.Should().Contain(d => d.MetricId == priceMetric.Id && d.Value == 45000.00m);
        allData.Should().Contain(d => d.MetricId == volumeMetric.Id && d.Value == 1000000.00m);
    }

    #endregion

    #region Cursor Behavior Tests

    [Fact]
    public async Task Ingestion_WhenAssetDue_ShouldIngestAndUpdateCursor()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var lookups = _fixture.Lookups;

        var tenant = TestDataBuilder.CreateTenant(lookups);
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        // Asset is due for ingestion (NextIngestionAtUtc is null = never ingested)
        var asset = TestDataBuilder.CreateAsset(tenant.Id, lookups, "BTC", "BTC");
        context.Assets.Add(asset);
        await context.SaveChangesAsync();

        var metric = TestDataBuilder.CreateMetric(tenant.Id, asset.Id, lookups, "price");
        context.Metrics.Add(metric);
        await context.SaveChangesAsync();

        var fakeFactory = new FakeDataSourceProviderFactory();
        var provider = fakeFactory.AddProvider("BINANCE");
        provider.SetupSuccess("BTC", new List<FetchedMetricValue>
        {
            new("price", 45000.00m, DateTime.UtcNow)
        });

        var beforeIngestion = DateTime.UtcNow;

        // Act
        var repository = new IngestionRepository(context);
        var handler = new IngestMetricsCommandHandler(
            repository,
            fakeFactory,
            context,
            NullLogger<IngestMetricsCommandHandler>.Instance);

        await handler.Handle(new IngestMetricsCommand(), CancellationToken.None);

        // Assert - Cursor updated
        await context.Entry(asset).ReloadAsync();
        
        asset.LastIngestedAtUtc.Should().NotBeNull();
        asset.LastIngestedAtUtc.Should().BeOnOrAfter(beforeIngestion);
        
        asset.NextIngestionAtUtc.Should().NotBeNull();
        asset.NextIngestionAtUtc.Should().BeAfter(asset.LastIngestedAtUtc!.Value);
    }

    [Fact]
    public async Task Ingestion_WhenAssetNotDue_ShouldNotIngest()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var lookups = _fixture.Lookups;

        var tenant = TestDataBuilder.CreateTenant(lookups);
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        var asset = TestDataBuilder.CreateAsset(tenant.Id, lookups, "BTC", "BTC");
        context.Assets.Add(asset);
        await context.SaveChangesAsync();

        var metric = TestDataBuilder.CreateMetric(tenant.Id, asset.Id, lookups, "price");
        context.Metrics.Add(metric);
        await context.SaveChangesAsync();

        // Manually set cursor to FUTURE - asset not due
        var futureTime = DateTime.UtcNow.AddHours(1);
        await context.Assets
            .Where(a => a.Id == asset.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(a => a.NextIngestionAtUtc, futureTime)
                .SetProperty(a => a.LastIngestedAtUtc, DateTime.UtcNow));

        var fakeFactory = new FakeDataSourceProviderFactory();
        var provider = fakeFactory.AddProvider("BINANCE");
        provider.SetupSuccess("BTC", new List<FetchedMetricValue>
        {
            new("price", 45000.00m, DateTime.UtcNow)
        });

        // Act
        var repository = new IngestionRepository(context);
        var handler = new IngestMetricsCommandHandler(
            repository,
            fakeFactory,
            context,
            NullLogger<IngestMetricsCommandHandler>.Instance);

        var result = await handler.Handle(new IngestMetricsCommand(), CancellationToken.None);

        // Assert
        result.AssetsProcessed.Should().Be(0, "asset not due for ingestion");
        provider.FetchCallCount.Should().Be(0, "no API calls made");

        var allData = await context.MetricData.ToListAsync();
        allData.Should().BeEmpty();
    }

    [Fact]
    public async Task Ingestion_SecondRunWithSameData_ShouldOnlyIngestIfDue()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var lookups = _fixture.Lookups;

        var tenant = TestDataBuilder.CreateTenant(lookups);
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        // Asset with 60 second interval
        var asset = TestDataBuilder.CreateAsset(tenant.Id, lookups, "BTC", "BTC");
        context.Assets.Add(asset);
        await context.SaveChangesAsync();

        var metric = TestDataBuilder.CreateMetric(tenant.Id, asset.Id, lookups, "price");
        context.Metrics.Add(metric);
        await context.SaveChangesAsync();

        var fakeFactory = new FakeDataSourceProviderFactory();
        var provider = fakeFactory.AddProvider("BINANCE");
        provider.SetupSuccess("BTC", new List<FetchedMetricValue>
        {
            new("price", 45000.00m, DateTime.UtcNow)
        });

        var repository = new IngestionRepository(context);
        var handler = new IngestMetricsCommandHandler(
            repository,
            fakeFactory,
            context,
            NullLogger<IngestMetricsCommandHandler>.Instance);

        // First run
        var result1 = await handler.Handle(new IngestMetricsCommand(), CancellationToken.None);
        result1.AssetsProcessed.Should().Be(1);

        // Second run immediately after - should NOT ingest
        var result2 = await handler.Handle(new IngestMetricsCommand(), CancellationToken.None);
        result2.AssetsProcessed.Should().Be(0, "cursor updated, asset not due");

        // Only one data point should exist
        var allData = await context.MetricData.ToListAsync();
        allData.Should().HaveCount(1, "second run didn't create duplicate");
    }

    #endregion

    #region Failure Handling Tests

    [Fact]
    public async Task Ingestion_WhenProviderFails_ShouldNotWriteData()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var lookups = _fixture.Lookups;

        var tenant = TestDataBuilder.CreateTenant(lookups);
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        var asset = TestDataBuilder.CreateAsset(tenant.Id, lookups, "BTC", "BTC");
        context.Assets.Add(asset);
        await context.SaveChangesAsync();

        var metric = TestDataBuilder.CreateMetric(tenant.Id, asset.Id, lookups, "price");
        context.Metrics.Add(metric);
        await context.SaveChangesAsync();

        // Setup provider to return failure
        var fakeFactory = new FakeDataSourceProviderFactory();
        var provider = fakeFactory.AddProvider("BINANCE");
        provider.SetupFailure("BTC", "API rate limit exceeded");

        var beforeIngestion = DateTime.UtcNow;

        // Act
        var repository = new IngestionRepository(context);
        var handler = new IngestMetricsCommandHandler(
            repository,
            fakeFactory,
            context,
            NullLogger<IngestMetricsCommandHandler>.Instance);

        var result = await handler.Handle(new IngestMetricsCommand(), CancellationToken.None);

        // Assert
        result.Errors.Should().Be(1);
        result.DataPointsCreated.Should().Be(0);

        // No data written
        var allData = await context.MetricData.ToListAsync();
        allData.Should().BeEmpty();

        // CRITICAL: Cursor should NOT be advanced on failure
        await context.Entry(asset).ReloadAsync();
        asset.LastIngestedAtUtc.Should().BeNull("cursor not advanced on failure");
    }

    [Fact]
    public async Task Ingestion_WhenSomeAssetsFailOthersSucceed_ShouldHandlePartialSuccess()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var lookups = _fixture.Lookups;

        var tenant = TestDataBuilder.CreateTenant(lookups);
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        // Two assets - one will succeed, one will fail
        var successAsset = TestDataBuilder.CreateAsset(tenant.Id, lookups, "BTC Success", "BTC");
        var failAsset = TestDataBuilder.CreateAsset(tenant.Id, lookups, "ETH Fail", "ETH");
        context.Assets.AddRange(successAsset, failAsset);
        await context.SaveChangesAsync();

        var successMetric = TestDataBuilder.CreateMetric(tenant.Id, successAsset.Id, lookups, "price");
        var failMetric = TestDataBuilder.CreateMetric(tenant.Id, failAsset.Id, lookups, "price");
        context.Metrics.AddRange(successMetric, failMetric);
        await context.SaveChangesAsync();

        var fakeFactory = new FakeDataSourceProviderFactory();
        var provider = fakeFactory.AddProvider("BINANCE");
        provider.SetupSuccess("BTC", new List<FetchedMetricValue>
        {
            new("price", 45000.00m, DateTime.UtcNow)
        });
        provider.SetupFailure("ETH", "Connection timeout");

        // Act
        var repository = new IngestionRepository(context);
        var handler = new IngestMetricsCommandHandler(
            repository,
            fakeFactory,
            context,
            NullLogger<IngestMetricsCommandHandler>.Instance);

        var result = await handler.Handle(new IngestMetricsCommand(), CancellationToken.None);

        // Assert
        result.AssetsProcessed.Should().Be(1);
        result.Errors.Should().Be(1);
        result.DataPointsCreated.Should().Be(1);

        // Success asset has data
        var allData = await context.MetricData.ToListAsync();
        allData.Should().HaveCount(1);
        allData.First().MetricId.Should().Be(successMetric.Id);

        // Success asset cursor advanced
        await context.Entry(successAsset).ReloadAsync();
        successAsset.LastIngestedAtUtc.Should().NotBeNull();

        // Fail asset cursor NOT advanced
        await context.Entry(failAsset).ReloadAsync();
        failAsset.LastIngestedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task Ingestion_WhenNoProviderRegistered_ShouldSkipAssets()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var lookups = _fixture.Lookups;

        var tenant = TestDataBuilder.CreateTenant(lookups);
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        // Asset uses BINANCE data source
        var asset = TestDataBuilder.CreateAsset(tenant.Id, lookups, "BTC", "BTC");
        context.Assets.Add(asset);
        await context.SaveChangesAsync();

        var metric = TestDataBuilder.CreateMetric(tenant.Id, asset.Id, lookups, "price");
        context.Metrics.Add(metric);
        await context.SaveChangesAsync();

        // Factory has NO providers registered
        var fakeFactory = new FakeDataSourceProviderFactory();
        // Intentionally NOT adding any provider

        // Act
        var repository = new IngestionRepository(context);
        var handler = new IngestMetricsCommandHandler(
            repository,
            fakeFactory,
            context,
            NullLogger<IngestMetricsCommandHandler>.Instance);

        var result = await handler.Handle(new IngestMetricsCommand(), CancellationToken.None);

        // Assert
        result.Errors.Should().Be(1, "no provider = error");
        result.DataPointsCreated.Should().Be(0);

        var allData = await context.MetricData.ToListAsync();
        allData.Should().BeEmpty();
    }

    #endregion

    #region Multi-Tenant Ingestion Isolation Tests

    [Fact]
    public async Task Ingestion_MultiTenant_ShouldMaintainDataIsolation()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext();
        var lookups = _fixture.Lookups;

        var tenantA = TestDataBuilder.CreateTenant(lookups, "Tenant A");
        var tenantB = TestDataBuilder.CreateTenant(lookups, "Tenant B");
        context.Tenants.AddRange(tenantA, tenantB);
        await context.SaveChangesAsync();

        var assetA = TestDataBuilder.CreateAsset(tenantA.Id, lookups, "BTC A", "BTC");
        var assetB = TestDataBuilder.CreateAsset(tenantB.Id, lookups, "BTC B", "BTC");
        context.Assets.AddRange(assetA, assetB);
        await context.SaveChangesAsync();

        var metricA = TestDataBuilder.CreateMetric(tenantA.Id, assetA.Id, lookups, "price");
        var metricB = TestDataBuilder.CreateMetric(tenantB.Id, assetB.Id, lookups, "price");
        context.Metrics.AddRange(metricA, metricB);
        await context.SaveChangesAsync();

        var fakeFactory = new FakeDataSourceProviderFactory();
        var provider = fakeFactory.AddProvider("BINANCE");
        provider.SetupSuccess("BTC", new List<FetchedMetricValue>
        {
            new("price", 50000.00m, DateTime.UtcNow)
        });

        // Act
        var repository = new IngestionRepository(context);
        var handler = new IngestMetricsCommandHandler(
            repository,
            fakeFactory,
            context,
            NullLogger<IngestMetricsCommandHandler>.Instance);

        await handler.Handle(new IngestMetricsCommand(), CancellationToken.None);

        // Assert - Each tenant's data is isolated
        var dataA = await context.MetricData.Where(d => d.TenantId == tenantA.Id).ToListAsync();
        var dataB = await context.MetricData.Where(d => d.TenantId == tenantB.Id).ToListAsync();

        dataA.Should().HaveCount(1);
        dataA.First().MetricId.Should().Be(metricA.Id);

        dataB.Should().HaveCount(1);
        dataB.First().MetricId.Should().Be(metricB.Id);

        // Same value, but different metric IDs (tenant isolation maintained)
        dataA.First().Value.Should().Be(dataB.First().Value);
        dataA.First().MetricId.Should().NotBe(dataB.First().MetricId);
    }

    #endregion
}
