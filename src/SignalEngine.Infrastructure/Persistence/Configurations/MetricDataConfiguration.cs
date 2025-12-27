using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SignalEngine.Domain.Entities;

namespace SignalEngine.Infrastructure.Persistence.Configurations;

public class MetricDataConfiguration : IEntityTypeConfiguration<MetricData>
{
    public void Configure(EntityTypeBuilder<MetricData> builder)
    {
        builder.ToTable("MetricData");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .UseIdentityColumn();

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.MetricId)
            .IsRequired();

        builder.Property(e => e.Value)
            .IsRequired()
            .HasPrecision(18, 6);

        builder.Property(e => e.Timestamp)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        // Performance indexes for time-series queries
        builder.HasIndex(e => new { e.MetricId, e.Timestamp })
            .IsDescending(false, true)
            .HasDatabaseName("IX_MetricData_MetricId_Timestamp");

        builder.HasIndex(e => e.TenantId);

        builder.HasIndex(e => e.Timestamp)
            .IsDescending(true);

        // Foreign key to Metrics table (with navigation)
        builder.HasOne(e => e.Metric)
            .WithMany(m => m.DataPoints)
            .HasForeignKey(e => e.MetricId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to Tenants table
        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
