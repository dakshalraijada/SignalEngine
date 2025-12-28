using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SignalEngine.Domain.Entities;

namespace SignalEngine.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .UseIdentityColumn();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Subdomain)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.TenantTypeId)
            .IsRequired();

        builder.Property(e => e.PlanId)
            .IsRequired();

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.DefaultNotificationEmail)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.Subdomain)
            .IsUnique();

        // Foreign key to LookupValues for TenantType
        builder.HasOne(e => e.TenantType)
            .WithMany()
            .HasForeignKey(e => e.TenantTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to Plans
        builder.HasOne(e => e.Plan)
            .WithMany()
            .HasForeignKey(e => e.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Assets)
            .WithOne(a => a.Tenant)
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Rules)
            .WithOne(r => r.Tenant)
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
