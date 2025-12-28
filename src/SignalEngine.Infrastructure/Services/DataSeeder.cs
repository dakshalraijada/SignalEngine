using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenIddict.Abstractions;
using SignalEngine.Domain.Constants;
using SignalEngine.Domain.Entities;
using SignalEngine.Infrastructure.Identity;
using SignalEngine.Infrastructure.Persistence;

namespace SignalEngine.Infrastructure.Services;

/// <summary>
/// Service to seed the database with required data.
/// </summary>
public class DataSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictScopeManager scopeManager,
        ILogger<DataSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _applicationManager = applicationManager;
        _scopeManager = scopeManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        _logger.LogInformation("Starting database seeding...");

        await SeedLookupTypesAsync();
        await SeedLookupValuesAsync();
        await SeedPlansAsync();
        await SeedDefaultTenantAsync();
        await SeedRolesAsync();
        await SeedTestUserAsync();
        await SeedOpenIddictClientsAsync();
        await SeedOpenIddictScopesAsync();
        await SeedSampleDataAsync();

        _logger.LogInformation("Database seeding completed.");
    }

    private async Task SeedLookupTypesAsync()
    {
        var lookupTypes = new[]
        {
            new { Code = LookupTypeCodes.TenantType, Description = "Types of tenants (B2C, B2B)" },
            new { Code = LookupTypeCodes.PlanCode, Description = "Subscription plan codes" },
            new { Code = LookupTypeCodes.RuleOperator, Description = "Rule comparison operators" },
            new { Code = LookupTypeCodes.Severity, Description = "Alert severity levels" },
            new { Code = LookupTypeCodes.AssetType, Description = "Types of monitored assets" },
            new { Code = LookupTypeCodes.MetricType, Description = "Types of metrics" },
            new { Code = LookupTypeCodes.SignalStatus, Description = "Signal status values" },
            new { Code = LookupTypeCodes.NotificationChannelType, Description = "Notification delivery channels" },
            new { Code = LookupTypeCodes.RuleEvaluationFrequency, Description = "Rule evaluation frequency intervals" },
            new { Code = LookupTypeCodes.DataSource, Description = "Data sources for asset ingestion" }
        };

        foreach (var lt in lookupTypes)
        {
            if (!await _context.LookupTypes.AnyAsync(x => x.Code == lt.Code))
            {
                var lookupType = new LookupType(lt.Code, lt.Description);
                await _context.LookupTypes.AddAsync(lookupType);
                _logger.LogDebug("Created lookup type: {Code}", lt.Code);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedLookupValuesAsync()
    {
        var lookupValues = new[]
        {
            // TENANT_TYPE
            new { TypeCode = LookupTypeCodes.TenantType, Code = TenantTypeCodes.B2C, Name = "Business to Consumer", SortOrder = 1 },
            new { TypeCode = LookupTypeCodes.TenantType, Code = TenantTypeCodes.B2B, Name = "Business to Business", SortOrder = 2 },

            // PLAN_CODE
            new { TypeCode = LookupTypeCodes.PlanCode, Code = PlanCodes.Free, Name = "Free Plan", SortOrder = 1 },
            new { TypeCode = LookupTypeCodes.PlanCode, Code = PlanCodes.Pro, Name = "Professional Plan", SortOrder = 2 },
            new { TypeCode = LookupTypeCodes.PlanCode, Code = PlanCodes.Business, Name = "Business Plan", SortOrder = 3 },

            // RULE_OPERATOR
            new { TypeCode = LookupTypeCodes.RuleOperator, Code = RuleOperatorCodes.GreaterThan, Name = "Greater Than", SortOrder = 1 },
            new { TypeCode = LookupTypeCodes.RuleOperator, Code = RuleOperatorCodes.LessThan, Name = "Less Than", SortOrder = 2 },
            new { TypeCode = LookupTypeCodes.RuleOperator, Code = RuleOperatorCodes.Equal, Name = "Equal To", SortOrder = 3 },
            new { TypeCode = LookupTypeCodes.RuleOperator, Code = RuleOperatorCodes.GreaterThanOrEqual, Name = "Greater Than or Equal", SortOrder = 4 },
            new { TypeCode = LookupTypeCodes.RuleOperator, Code = RuleOperatorCodes.LessThanOrEqual, Name = "Less Than or Equal", SortOrder = 5 },

            // SEVERITY
            new { TypeCode = LookupTypeCodes.Severity, Code = SeverityCodes.Info, Name = "Information", SortOrder = 1 },
            new { TypeCode = LookupTypeCodes.Severity, Code = SeverityCodes.Warning, Name = "Warning", SortOrder = 2 },
            new { TypeCode = LookupTypeCodes.Severity, Code = SeverityCodes.Critical, Name = "Critical", SortOrder = 3 },

            // ASSET_TYPE
            new { TypeCode = LookupTypeCodes.AssetType, Code = AssetTypeCodes.Crypto, Name = "Cryptocurrency", SortOrder = 1 },
            new { TypeCode = LookupTypeCodes.AssetType, Code = AssetTypeCodes.Website, Name = "Website", SortOrder = 2 },
            new { TypeCode = LookupTypeCodes.AssetType, Code = AssetTypeCodes.Service, Name = "Service", SortOrder = 3 },

            // METRIC_TYPE
            new { TypeCode = LookupTypeCodes.MetricType, Code = MetricTypeCodes.Numeric, Name = "Numeric Value", SortOrder = 1 },
            new { TypeCode = LookupTypeCodes.MetricType, Code = MetricTypeCodes.Percentage, Name = "Percentage", SortOrder = 2 },
            new { TypeCode = LookupTypeCodes.MetricType, Code = MetricTypeCodes.Rate, Name = "Rate", SortOrder = 3 },

            // SIGNAL_STATUS
            new { TypeCode = LookupTypeCodes.SignalStatus, Code = SignalStatusCodes.Open, Name = "Open", SortOrder = 1 },
            new { TypeCode = LookupTypeCodes.SignalStatus, Code = SignalStatusCodes.Resolved, Name = "Resolved", SortOrder = 2 },

            // NOTIFICATION_CHANNEL_TYPE
            new { TypeCode = LookupTypeCodes.NotificationChannelType, Code = NotificationChannelTypeCodes.Email, Name = "Email", SortOrder = 1 },
            new { TypeCode = LookupTypeCodes.NotificationChannelType, Code = NotificationChannelTypeCodes.Webhook, Name = "Webhook", SortOrder = 2 },
            new { TypeCode = LookupTypeCodes.NotificationChannelType, Code = NotificationChannelTypeCodes.Slack, Name = "Slack", SortOrder = 3 },

            // RULE_EVALUATION_FREQUENCY
            new { TypeCode = LookupTypeCodes.RuleEvaluationFrequency, Code = RuleEvaluationFrequencyCodes.OneMinute, Name = "Every 1 Minute", SortOrder = 1 },
            new { TypeCode = LookupTypeCodes.RuleEvaluationFrequency, Code = RuleEvaluationFrequencyCodes.FiveMinutes, Name = "Every 5 Minutes", SortOrder = 2 },
            new { TypeCode = LookupTypeCodes.RuleEvaluationFrequency, Code = RuleEvaluationFrequencyCodes.FifteenMinutes, Name = "Every 15 Minutes", SortOrder = 3 },

            // DATA_SOURCE - where asset data comes from
            new { TypeCode = LookupTypeCodes.DataSource, Code = DataSourceCodes.Binance, Name = "Binance Exchange", SortOrder = 1 },
            new { TypeCode = LookupTypeCodes.DataSource, Code = DataSourceCodes.Coinbase, Name = "Coinbase Exchange", SortOrder = 2 },
            new { TypeCode = LookupTypeCodes.DataSource, Code = DataSourceCodes.Kraken, Name = "Kraken Exchange", SortOrder = 3 },
            new { TypeCode = LookupTypeCodes.DataSource, Code = DataSourceCodes.CustomApi, Name = "Custom API", SortOrder = 4 }
        };

        foreach (var lv in lookupValues)
        {
            var lookupType = await _context.LookupTypes.FirstOrDefaultAsync(x => x.Code == lv.TypeCode);
            if (lookupType == null) continue;

            if (!await _context.LookupValues.AnyAsync(x => x.LookupTypeId == lookupType.Id && x.Code == lv.Code))
            {
                var lookupValue = new LookupValue(lookupType.Id, lv.Code, lv.Name, lv.SortOrder);
                await _context.LookupValues.AddAsync(lookupValue);
                _logger.LogDebug("Created lookup value: {TypeCode}.{Code}", lv.TypeCode, lv.Code);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedPlansAsync()
    {
        var freePlanCodeId = await GetLookupValueIdAsync(LookupTypeCodes.PlanCode, PlanCodes.Free);
        var proPlanCodeId = await GetLookupValueIdAsync(LookupTypeCodes.PlanCode, PlanCodes.Pro);
        var businessPlanCodeId = await GetLookupValueIdAsync(LookupTypeCodes.PlanCode, PlanCodes.Business);

        var plans = new[]
        {
            new { Name = "Free", PlanCodeId = freePlanCodeId, MaxRules = 3, MaxAssets = 2, MaxNotifications = 10, AllowWebhook = false, AllowSlack = false, Price = 0m },
            new { Name = "Pro", PlanCodeId = proPlanCodeId, MaxRules = 20, MaxAssets = 10, MaxNotifications = 100, AllowWebhook = true, AllowSlack = false, Price = 29m },
            new { Name = "Business", PlanCodeId = businessPlanCodeId, MaxRules = 100, MaxAssets = 50, MaxNotifications = 1000, AllowWebhook = true, AllowSlack = true, Price = 99m }
        };

        foreach (var p in plans)
        {
            if (!await _context.Plans.AnyAsync(x => x.PlanCodeId == p.PlanCodeId))
            {
                var plan = new Plan(p.Name, p.PlanCodeId, p.MaxRules, p.MaxAssets, p.MaxNotifications, p.AllowWebhook, p.AllowSlack, p.Price);
                await _context.Plans.AddAsync(plan);
                _logger.LogDebug("Created plan: {Name}", p.Name);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedDefaultTenantAsync()
    {
        if (await _context.Tenants.AnyAsync(x => x.Subdomain == "default"))
            return;

        var tenantTypeId = await GetLookupValueIdAsync(LookupTypeCodes.TenantType, TenantTypeCodes.B2C);
        var freePlanCodeId = await GetLookupValueIdAsync(LookupTypeCodes.PlanCode, PlanCodes.Free);
        var plan = await _context.Plans.FirstOrDefaultAsync(x => x.PlanCodeId == freePlanCodeId);

        if (plan == null)
        {
            _logger.LogError("Free plan not found. Cannot create default tenant.");
            return;
        }

        var tenant = new Tenant("Default Tenant", "default", tenantTypeId, plan.Id);
        await _context.Tenants.AddAsync(tenant);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Created default tenant with ID: {TenantId}", tenant.Id);
    }

    private async Task SeedRolesAsync()
    {
        var roles = new[] { "Admin", "User" };

        foreach (var roleName in roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var role = new ApplicationRole
                {
                    Name = roleName,
                    Description = $"{roleName} role"
                };
                await _roleManager.CreateAsync(role);
                _logger.LogDebug("Created role: {RoleName}", roleName);
            }
        }
    }

    private async Task SeedTestUserAsync()
    {
        const string email = "admin@signalengine.local";
        const string password = "P@ssword123";

        if (await _userManager.FindByEmailAsync(email) != null)
            return;

        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Subdomain == "default");
        if (tenant == null)
        {
            _logger.LogError("Default tenant not found. Cannot create test user.");
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            TenantId = tenant.Id,
            FirstName = "Admin",
            LastName = "User",
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Admin");
            _logger.LogInformation("Created test user: {Email}", email);
        }
        else
        {
            _logger.LogError("Failed to create test user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private async Task SeedOpenIddictClientsAsync()
    {
        // Angular SPA client (PKCE) - for user authentication
        // Users login via browser, SPA gets access token, then calls SystemAPI
        if (await _applicationManager.FindByClientIdAsync("signalengine-spa") == null)
        {
            await _applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "signalengine-spa",
                DisplayName = "SignalEngine Angular SPA",
                ClientType = OpenIddictConstants.ClientTypes.Public,
                RedirectUris =
                {
                    new Uri("https://localhost:4200/callback"),
                    new Uri("https://localhost:4200/silent-refresh"),
                    new Uri("http://localhost:4200/callback"),
                    new Uri("http://localhost:4200/silent-refresh")
                },
                PostLogoutRedirectUris =
                {
                    new Uri("https://localhost:4200"),
                    new Uri("http://localhost:4200")
                },
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.Endpoints.EndSession,
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    OpenIddictConstants.Permissions.ResponseTypes.Code,
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "system-api"
                },
                Requirements =
                {
                    OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
                }
            });
            _logger.LogInformation("Created OpenIddict SPA client");
        }

        // ROPC client - for testing/Postman (get tokens via username/password)
        if (await _applicationManager.FindByClientIdAsync("signalengine-ropc") == null)
        {
            await _applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "signalengine-ropc",
                ClientSecret = "RopcSecret123!",
                DisplayName = "SignalEngine ROPC Client (Testing)",
                ClientType = OpenIddictConstants.ClientTypes.Confidential,
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.Password,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                    OpenIddictConstants.Permissions.Prefixes.Scope + "offline_access",
                    OpenIddictConstants.Permissions.Prefixes.Scope + "system-api"
                }
            });
            _logger.LogInformation("Created OpenIddict ROPC client");
        }
    }

    private async Task SeedOpenIddictScopesAsync()
    {
        // Create the system-api scope - this sets the audience in the token
        if (await _scopeManager.FindByNameAsync("system-api") == null)
        {
            await _scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "system-api",
                DisplayName = "SignalEngine System API",
                Resources =
                {
                    "signalengine-api"  // This becomes the 'aud' claim in the token
                }
            });
            _logger.LogInformation("Created OpenIddict scope: system-api");
        }
    }

    private async Task SeedSampleDataAsync()
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Subdomain == "default");
        if (tenant == null) return;

        // Check if sample data already exists
        if (await _context.Assets.AnyAsync(a => a.TenantId == tenant.Id))
            return;

        var cryptoTypeId = await GetLookupValueIdAsync(LookupTypeCodes.AssetType, AssetTypeCodes.Crypto);
        var websiteTypeId = await GetLookupValueIdAsync(LookupTypeCodes.AssetType, AssetTypeCodes.Website);
        var binanceSourceId = await GetLookupValueIdAsync(LookupTypeCodes.DataSource, DataSourceCodes.Binance);
        var customApiSourceId = await GetLookupValueIdAsync(LookupTypeCodes.DataSource, DataSourceCodes.CustomApi);

        // Create sample assets (now with DataSourceId)
        var btcAsset = new Asset(tenant.Id, "Bitcoin", "BTC", cryptoTypeId, binanceSourceId, "Bitcoin cryptocurrency");
        var webAsset = new Asset(tenant.Id, "Main Website", "https://www.example.com", websiteTypeId, customApiSourceId, "Company main website");

        await _context.Assets.AddRangeAsync(btcAsset, webAsset);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created sample assets");
    }

    private async Task<int> GetLookupValueIdAsync(string typeCode, string valueCode)
    {
        var lookupType = await _context.LookupTypes.FirstOrDefaultAsync(x => x.Code == typeCode);
        if (lookupType == null)
            throw new InvalidOperationException($"Lookup type '{typeCode}' not found");

        var lookupValue = await _context.LookupValues
            .FirstOrDefaultAsync(x => x.LookupTypeId == lookupType.Id && x.Code == valueCode);

        if (lookupValue == null)
            throw new InvalidOperationException($"Lookup value '{valueCode}' not found for type '{typeCode}'");

        return lookupValue.Id;
    }
}
