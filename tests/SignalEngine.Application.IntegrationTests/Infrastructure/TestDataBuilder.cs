using SignalEngine.Domain.Entities;

namespace SignalEngine.Application.IntegrationTests.Infrastructure;

/// <summary>
/// Builder for creating test data with fluent syntax.
/// Simplifies test setup by providing sensible defaults.
/// </summary>
public static class TestDataBuilder
{
    /// <summary>
    /// Creates a tenant with default values.
    /// </summary>
    public static Tenant CreateTenant(
        LookupIds lookups,
        string name = "Test Tenant",
        string? subdomain = null)
    {
        subdomain ??= $"test-{Guid.NewGuid():N}".Substring(0, 20);
        return new Tenant(name, subdomain, lookups.TenantTypeB2C, lookups.FreePlanId);
    }

    /// <summary>
    /// Creates an asset for the specified tenant.
    /// </summary>
    public static Asset CreateAsset(
        int tenantId,
        LookupIds lookups,
        string name = "Test Asset",
        string identifier = "TEST")
    {
        return new Asset(
            tenantId: tenantId,
            name: name,
            identifier: identifier,
            assetTypeId: lookups.AssetTypeCrypto,
            dataSourceId: lookups.DataSourceBinance);
    }

    /// <summary>
    /// Creates a metric for the specified tenant and asset.
    /// </summary>
    public static Metric CreateMetric(
        int tenantId,
        int assetId,
        LookupIds lookups,
        string name = "price")
    {
        return new Metric(
            tenantId: tenantId,
            assetId: assetId,
            name: name,
            metricTypeId: lookups.MetricTypeNumeric,
            unit: "USD");
    }

    /// <summary>
    /// Creates a rule for the specified tenant and asset.
    /// </summary>
    public static Rule CreateRule(
        int tenantId,
        int assetId,
        LookupIds lookups,
        string metricName = "price",
        int? operatorId = null,
        decimal threshold = 100.00m,
        int? severityId = null,
        int consecutiveBreachesRequired = 1,
        string name = "Test Rule")
    {
        return new Rule(
            tenantId: tenantId,
            assetId: assetId,
            name: name,
            metricName: metricName,
            operatorId: operatorId ?? lookups.OperatorGT,
            threshold: threshold,
            severityId: severityId ?? lookups.SeverityWarning,
            evaluationFrequencyId: lookups.EvaluationFrequency5Min,
            consecutiveBreachesRequired: consecutiveBreachesRequired);
    }

    /// <summary>
    /// Creates a metric data point.
    /// </summary>
    public static MetricData CreateMetricData(
        int tenantId,
        int metricId,
        decimal value,
        DateTime? timestamp = null)
    {
        return new MetricData(
            tenantId: tenantId,
            metricId: metricId,
            value: value,
            timestamp: timestamp ?? DateTime.UtcNow);
    }
}
