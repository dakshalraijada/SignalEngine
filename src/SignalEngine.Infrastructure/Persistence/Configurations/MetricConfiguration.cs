using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SignalEngine.Domain.Entities;

namespace SignalEngine.Infrastructure.Persistence.Configurations;

public class MetricConfiguration : IEntityTypeConfiguration<Metric>
{
    public void Configure(EntityTypeBuilder<Metric> builder)
    {
        builder.ToTable("Metrics");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .UseIdentityColumn();

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.AssetId)
            .IsRequired();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.MetricTypeId)
            .IsRequired();

        builder.Property(e => e.Value)
            .IsRequired()
            .HasPrecision(18, 6);

        builder.Property(e => e.Timestamp)
            .IsRequired();

        builder.Property(e => e.Unit)
            .HasMaxLength(50);

        builder.Property(e => e.Source)
            .HasMaxLength(200);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.AssetId, e.Name });
        builder.HasIndex(e => e.Timestamp);
    }
}
