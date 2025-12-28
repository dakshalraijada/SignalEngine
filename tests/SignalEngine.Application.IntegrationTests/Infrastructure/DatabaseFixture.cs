using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Infrastructure.Persistence;
using Testcontainers.MsSql;
using Xunit;

namespace SignalEngine.Application.IntegrationTests.Infrastructure;

/// <summary>
/// Shared database fixture using Testcontainers for SQL Server.
/// This fixture is shared across all tests in the collection for performance.
/// 
/// Each test gets a fresh, isolated database context with seeded lookup data.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;
    
    public string ConnectionString { get; private set; } = null!;
    
    /// <summary>
    /// Cached lookup IDs for test data creation.
    /// These are seeded once and remain constant.
    /// </summary>
    public LookupIds Lookups { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Start SQL Server container
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("Strong_Password_123!")
            .Build();

        await _container.StartAsync();
        
        ConnectionString = _container.GetConnectionString();

        // Create the database schema and seed lookup data
        await InitializeDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container != null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates a scoped DbContext with a test tenant accessor that bypasses filtering.
    /// </summary>
    public ApplicationDbContext CreateDbContext(TestTenantAccessor? tenantAccessor = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new ApplicationDbContext(options, tenantAccessor ?? TestTenantAccessor.SystemLevel());
    }

    /// <summary>
    /// Resets the database to a clean state between tests.
    /// Preserves lookup data but clears all test entities.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await using var context = CreateDbContext();
        
        // Clear test data in reverse order of dependencies
        context.Notifications.RemoveRange(context.Notifications);
        context.SignalResolutions.RemoveRange(context.SignalResolutions);
        context.Signals.RemoveRange(context.Signals);
        context.SignalStates.RemoveRange(context.SignalStates);
        context.Rules.RemoveRange(context.Rules);
        context.MetricData.RemoveRange(context.MetricData);
        context.Metrics.RemoveRange(context.Metrics);
        context.Assets.RemoveRange(context.Assets);
        context.Tenants.RemoveRange(context.Tenants);
        // Note: Plans are seeded and should not be cleared
        // Note: Lookups are seeded and should not be cleared

        await context.SaveChangesAsync();
    }

    private async Task InitializeDatabaseAsync()
    {
        await using var context = CreateDbContext();
        
        // Ensure database is created with schema
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        // Seed lookup data
        await SeedLookupDataAsync(context);
        await SeedPlanDataAsync(context);
    }

    private async Task SeedLookupDataAsync(ApplicationDbContext context)
    {
        // Create lookup types and values needed for testing
        var lookupTypes = new Dictionary<string, Domain.Entities.LookupType>();
        
        // TENANT_TYPE
        var tenantType = new Domain.Entities.LookupType("TENANT_TYPE", "Tenant Types");
        context.LookupTypes.Add(tenantType);
        lookupTypes["TENANT_TYPE"] = tenantType;
        
        // PLAN_CODE
        var planCode = new Domain.Entities.LookupType("PLAN_CODE", "Plan Codes");
        context.LookupTypes.Add(planCode);
        lookupTypes["PLAN_CODE"] = planCode;
        
        // RULE_OPERATOR
        var ruleOperator = new Domain.Entities.LookupType("RULE_OPERATOR", "Rule Operators");
        context.LookupTypes.Add(ruleOperator);
        lookupTypes["RULE_OPERATOR"] = ruleOperator;
        
        // SEVERITY
        var severity = new Domain.Entities.LookupType("SEVERITY", "Severity Levels");
        context.LookupTypes.Add(severity);
        lookupTypes["SEVERITY"] = severity;
        
        // ASSET_TYPE
        var assetType = new Domain.Entities.LookupType("ASSET_TYPE", "Asset Types");
        context.LookupTypes.Add(assetType);
        lookupTypes["ASSET_TYPE"] = assetType;
        
        // METRIC_TYPE
        var metricType = new Domain.Entities.LookupType("METRIC_TYPE", "Metric Types");
        context.LookupTypes.Add(metricType);
        lookupTypes["METRIC_TYPE"] = metricType;
        
        // SIGNAL_STATUS
        var signalStatus = new Domain.Entities.LookupType("SIGNAL_STATUS", "Signal Statuses");
        context.LookupTypes.Add(signalStatus);
        lookupTypes["SIGNAL_STATUS"] = signalStatus;
        
        // RULE_EVALUATION_FREQUENCY
        var evalFrequency = new Domain.Entities.LookupType("RULE_EVALUATION_FREQUENCY", "Evaluation Frequencies");
        context.LookupTypes.Add(evalFrequency);
        lookupTypes["RULE_EVALUATION_FREQUENCY"] = evalFrequency;
        
        // DATA_SOURCE
        var dataSource = new Domain.Entities.LookupType("DATA_SOURCE", "Data Sources");
        context.LookupTypes.Add(dataSource);
        lookupTypes["DATA_SOURCE"] = dataSource;

        await context.SaveChangesAsync();

        // Create lookup values - we need their IDs
        var lookupIds = new LookupIds();

        // TENANT_TYPE values
        var b2c = new Domain.Entities.LookupValue(lookupTypes["TENANT_TYPE"].Id, "B2C", "B2C Tenant", 1);
        context.LookupValues.Add(b2c);

        // PLAN_CODE values
        var freePlan = new Domain.Entities.LookupValue(lookupTypes["PLAN_CODE"].Id, "FREE", "Free Plan", 1);
        context.LookupValues.Add(freePlan);

        // RULE_OPERATOR values
        var gt = new Domain.Entities.LookupValue(lookupTypes["RULE_OPERATOR"].Id, "GT", "Greater Than", 1);
        var gte = new Domain.Entities.LookupValue(lookupTypes["RULE_OPERATOR"].Id, "GTE", "Greater Than or Equal", 2);
        var lt = new Domain.Entities.LookupValue(lookupTypes["RULE_OPERATOR"].Id, "LT", "Less Than", 3);
        var lte = new Domain.Entities.LookupValue(lookupTypes["RULE_OPERATOR"].Id, "LTE", "Less Than or Equal", 4);
        var eq = new Domain.Entities.LookupValue(lookupTypes["RULE_OPERATOR"].Id, "EQ", "Equal", 5);
        context.LookupValues.AddRange(gt, gte, lt, lte, eq);

        // SEVERITY values
        var info = new Domain.Entities.LookupValue(lookupTypes["SEVERITY"].Id, "INFO", "Informational", 1);
        var warning = new Domain.Entities.LookupValue(lookupTypes["SEVERITY"].Id, "WARNING", "Warning", 2);
        var critical = new Domain.Entities.LookupValue(lookupTypes["SEVERITY"].Id, "CRITICAL", "Critical", 3);
        context.LookupValues.AddRange(info, warning, critical);

        // ASSET_TYPE values
        var crypto = new Domain.Entities.LookupValue(lookupTypes["ASSET_TYPE"].Id, "CRYPTO", "Cryptocurrency", 1);
        context.LookupValues.Add(crypto);

        // METRIC_TYPE values
        var numeric = new Domain.Entities.LookupValue(lookupTypes["METRIC_TYPE"].Id, "NUMERIC", "Numeric", 1);
        context.LookupValues.Add(numeric);

        // SIGNAL_STATUS values
        var open = new Domain.Entities.LookupValue(lookupTypes["SIGNAL_STATUS"].Id, "OPEN", "Open", 1);
        var resolved = new Domain.Entities.LookupValue(lookupTypes["SIGNAL_STATUS"].Id, "RESOLVED", "Resolved", 2);
        context.LookupValues.AddRange(open, resolved);

        // RULE_EVALUATION_FREQUENCY values
        var fiveMin = new Domain.Entities.LookupValue(lookupTypes["RULE_EVALUATION_FREQUENCY"].Id, "5_MIN", "Every 5 Minutes", 1);
        context.LookupValues.Add(fiveMin);

        // DATA_SOURCE values
        var binance = new Domain.Entities.LookupValue(lookupTypes["DATA_SOURCE"].Id, "BINANCE", "Binance", 1);
        var customApi = new Domain.Entities.LookupValue(lookupTypes["DATA_SOURCE"].Id, "CUSTOM_API", "Custom API", 2);
        context.LookupValues.AddRange(binance, customApi);

        await context.SaveChangesAsync();

        // Store IDs for test usage
        lookupIds.TenantTypeB2C = b2c.Id;
        lookupIds.PlanCodeFree = freePlan.Id;
        lookupIds.OperatorGT = gt.Id;
        lookupIds.OperatorGTE = gte.Id;
        lookupIds.OperatorLT = lt.Id;
        lookupIds.OperatorLTE = lte.Id;
        lookupIds.OperatorEQ = eq.Id;
        lookupIds.SeverityInfo = info.Id;
        lookupIds.SeverityWarning = warning.Id;
        lookupIds.SeverityCritical = critical.Id;
        lookupIds.AssetTypeCrypto = crypto.Id;
        lookupIds.MetricTypeNumeric = numeric.Id;
        lookupIds.SignalStatusOpen = open.Id;
        lookupIds.SignalStatusResolved = resolved.Id;
        lookupIds.EvaluationFrequency5Min = fiveMin.Id;
        lookupIds.DataSourceBinance = binance.Id;
        lookupIds.DataSourceCustomApi = customApi.Id;

        Lookups = lookupIds;
    }

    private async Task SeedPlanDataAsync(ApplicationDbContext context)
    {
        // Create a free plan for testing
        var freePlan = new Domain.Entities.Plan(
            name: "Free",
            planCodeId: Lookups.PlanCodeFree,
            maxRules: 10,
            maxAssets: 5,
            maxNotificationsPerDay: 100,
            allowWebhook: false,
            allowSlack: false,
            monthlyPrice: 0);

        context.Plans.Add(freePlan);
        await context.SaveChangesAsync();

        Lookups.FreePlanId = freePlan.Id;
    }
}

/// <summary>
/// Cached lookup IDs for test data creation.
/// </summary>
public class LookupIds
{
    public int TenantTypeB2C { get; set; }
    public int PlanCodeFree { get; set; }
    public int FreePlanId { get; set; }
    public int OperatorGT { get; set; }
    public int OperatorGTE { get; set; }
    public int OperatorLT { get; set; }
    public int OperatorLTE { get; set; }
    public int OperatorEQ { get; set; }
    public int SeverityInfo { get; set; }
    public int SeverityWarning { get; set; }
    public int SeverityCritical { get; set; }
    public int AssetTypeCrypto { get; set; }
    public int MetricTypeNumeric { get; set; }
    public int SignalStatusOpen { get; set; }
    public int SignalStatusResolved { get; set; }
    public int EvaluationFrequency5Min { get; set; }
    public int DataSourceBinance { get; set; }
    public int DataSourceCustomApi { get; set; }
}
