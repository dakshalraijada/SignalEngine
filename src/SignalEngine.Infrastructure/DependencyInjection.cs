using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Infrastructure.Identity;
using SignalEngine.Infrastructure.Persistence;
using SignalEngine.Infrastructure.Repositories;
using SignalEngine.Infrastructure.Services;
using SignalEngine.Infrastructure.Services.DataSources;
using SignalEngine.Infrastructure.Services.Email;

namespace SignalEngine.Infrastructure;

/// <summary>
/// Dependency injection configuration for the Infrastructure layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        bool includeDataSeeder = true)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // Configure DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
            
            // Suppress pending model changes warning for OpenIddict model differences between preview versions
            // This is safe because we manually manage migrations and the schema is production-compatible
            options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
            
            // Only configure OpenIddict stores if we're including data seeder (i.e., IdentityServer)
            if (includeDataSeeder)
            {
                options.UseOpenIddict();
            }
        });

        // Configure Identity
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Register repositories
        services.AddScoped<ILookupRepository, LookupRepository>();
        services.AddScoped<IRuleRepository, RuleRepository>();
        services.AddScoped<ISignalRepository, SignalRepository>();
        services.AddScoped<ISignalStateRepository, SignalStateRepository>();
        services.AddScoped<ISignalResolutionRepository, SignalResolutionRepository>();
        services.AddScoped<IMetricRepository, MetricRepository>();
        services.AddScoped<IMetricDataRepository, MetricDataRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IIngestionRepository, IngestionRepository>();
        services.AddScoped<IRuleEvaluationRepository, RuleEvaluationRepository>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Register tenant accessor for multi-tenant query filtering
        services.AddScoped<ITenantAccessor, TenantAccessor>();

        // Register services
        
        // Configure Email options and sender
        services.Configure<EmailOptions>(configuration.GetSection("Email"));
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        
        // Register HttpClient for NotificationDispatcher (webhook HTTP POST)
        services.AddHttpClient<NotificationDispatcher>(client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        
        // Register HttpClient for Binance API
        services.AddHttpClient<BinanceDataSourceProvider>(client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        
        // Register data source providers for metric ingestion
        services.AddScoped<IDataSourceProvider, BinanceDataSourceProvider>();
        services.AddScoped<IDataSourceProvider, CustomApiDataSourceProvider>();
        services.AddScoped<IDataSourceProviderFactory, DataSourceProviderFactory>();
        
        // Only register DataSeeder for IdentityServer (needs OpenIddict managers)
        if (includeDataSeeder)
        {
            services.AddScoped<DataSeeder>();
        }

        return services;
    }
}
