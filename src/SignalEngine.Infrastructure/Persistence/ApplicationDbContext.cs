using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Common;
using SignalEngine.Domain.Entities;
using SignalEngine.Infrastructure.Identity;

namespace SignalEngine.Infrastructure.Persistence;

/// <summary>
/// Application database context containing Identity, OpenIddict, and SignalEngine tables.
/// Implements multi-tenant data isolation through global query filters.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>, IUnitOfWork
{
    private readonly ITenantAccessor? _tenantAccessor;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options, 
        ITenantAccessor tenantAccessor) : base(options)
    {
        _tenantAccessor = tenantAccessor;
    }

    /// <summary>
    /// Gets the current tenant ID for query filtering.
    /// Returns 0 when no tenant context is available (bypasses filter).
    /// </summary>
    private int CurrentTenantId => _tenantAccessor?.CurrentTenantId ?? 0;

    /// <summary>
    /// Indicates whether tenant filtering is active.
    /// </summary>
    private bool IsTenantFilteringEnabled => _tenantAccessor?.IsFilteringEnabled ?? false;

    // Lookup tables
    public DbSet<LookupType> LookupTypes => Set<LookupType>();
    public DbSet<LookupValue> LookupValues => Set<LookupValue>();

    // SignalEngine tables
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Metric> Metrics => Set<Metric>();
    public DbSet<MetricData> MetricData => Set<MetricData>();
    public DbSet<Rule> Rules => Set<Rule>();
    public DbSet<Signal> Signals => Set<Signal>();
    public DbSet<SignalResolution> SignalResolutions => Set<SignalResolution>();
    public DbSet<SignalState> SignalStates => Set<SignalState>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply configurations
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Configure global query filters for multi-tenant data isolation
        ConfigureTenantQueryFilters(builder);

        // Configure OpenIddict tables to use 'identity' schema
        builder.Entity<OpenIddict.EntityFrameworkCore.Models.OpenIddictEntityFrameworkCoreApplication>().ToTable("OpenIddictApplications", "identity");
        builder.Entity<OpenIddict.EntityFrameworkCore.Models.OpenIddictEntityFrameworkCoreAuthorization>().ToTable("OpenIddictAuthorizations", "identity");
        builder.Entity<OpenIddict.EntityFrameworkCore.Models.OpenIddictEntityFrameworkCoreScope>().ToTable("OpenIddictScopes", "identity");
        builder.Entity<OpenIddict.EntityFrameworkCore.Models.OpenIddictEntityFrameworkCoreToken>().ToTable("OpenIddictTokens", "identity");

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.TenantId).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.HasIndex(e => e.TenantId);

            // Foreign key to Tenants
            entity.HasOne(e => e.Tenant)
                .WithMany()
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(500);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Configures global query filters to enforce tenant data isolation.
    /// All ITenantScoped entities are automatically filtered by CurrentTenantId.
    /// </summary>
    private void ConfigureTenantQueryFilters(ModelBuilder builder)
    {
        // Apply tenant filter to all tenant-scoped entities
        // The filter is bypassed when IsTenantFilteringEnabled is false (system operations)
        // or when CurrentTenantId matches the entity's TenantId

        builder.Entity<Asset>()
            .HasQueryFilter(e => !IsTenantFilteringEnabled || e.TenantId == CurrentTenantId);

        builder.Entity<Rule>()
            .HasQueryFilter(e => !IsTenantFilteringEnabled || e.TenantId == CurrentTenantId);

        builder.Entity<Signal>()
            .HasQueryFilter(e => !IsTenantFilteringEnabled || e.TenantId == CurrentTenantId);

        builder.Entity<SignalState>()
            .HasQueryFilter(e => !IsTenantFilteringEnabled || e.TenantId == CurrentTenantId);

        builder.Entity<Notification>()
            .HasQueryFilter(e => !IsTenantFilteringEnabled || e.TenantId == CurrentTenantId);

        // Metric is tenant-scoped through Asset relationship
        // Apply filter via Asset navigation (null-forgiving: Asset is required FK)
        builder.Entity<Metric>()
            .HasQueryFilter(e => !IsTenantFilteringEnabled || e.Asset!.TenantId == CurrentTenantId);

        // MetricData is tenant-scoped through Metric->Asset relationship
        // Null-forgiving operators used because these are required FK relationships
        builder.Entity<MetricData>()
            .HasQueryFilter(e => !IsTenantFilteringEnabled || e.Metric!.Asset!.TenantId == CurrentTenantId);

        // SignalResolution inherits tenant from Signal
        builder.Entity<SignalResolution>()
            .HasQueryFilter(e => !IsTenantFilteringEnabled || e.Signal.TenantId == CurrentTenantId);
    }
}
