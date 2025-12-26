using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SignalEngine.Application.Common.Interfaces;
using SignalEngine.Domain.Entities;
using SignalEngine.Infrastructure.Identity;

namespace SignalEngine.Infrastructure.Persistence;

/// <summary>
/// Application database context containing Identity, OpenIddict, and SignalEngine tables.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>, IUnitOfWork
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // Lookup tables
    public DbSet<LookupType> LookupTypes => Set<LookupType>();
    public DbSet<LookupValue> LookupValues => Set<LookupValue>();

    // SignalEngine tables
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Metric> Metrics => Set<Metric>();
    public DbSet<Rule> Rules => Set<Rule>();
    public DbSet<Signal> Signals => Set<Signal>();
    public DbSet<SignalState> SignalStates => Set<SignalState>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply configurations
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

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
}
