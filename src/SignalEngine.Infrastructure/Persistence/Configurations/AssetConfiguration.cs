using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SignalEngine.Domain.Entities;

namespace SignalEngine.Infrastructure.Persistence.Configurations;

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("Assets");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .UseIdentityColumn();

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Identifier)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.AssetTypeId)
            .IsRequired();

        builder.Property(e => e.DataSourceId)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.Metadata)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        // Ingestion scheduling properties
        builder.Property(e => e.IngestionIntervalSeconds)
            .IsRequired()
            .HasDefaultValue(60);

        builder.Property(e => e.LastIngestedAtUtc)
            .IsRequired(false);

        builder.Property(e => e.NextIngestionAtUtc)
            .IsRequired(false);

        builder.HasIndex(e => e.TenantId);

        builder.HasIndex(e => new { e.TenantId, e.Identifier })
            .IsUnique();

        // Index for ingestion worker: find assets due for ingestion
        // Covers: WHERE IsActive = 1 AND (NextIngestionAtUtc IS NULL OR NextIngestionAtUtc <= @now)
        builder.HasIndex(e => new { e.IsActive, e.NextIngestionAtUtc })
            .HasDatabaseName("IX_Assets_Ingestion_Due");

        // Index for grouping assets by DataSource during ingestion
        builder.HasIndex(e => new { e.DataSourceId, e.Identifier })
            .HasDatabaseName("IX_Assets_DataSource_Identifier");

        // Foreign key to LookupValues for AssetType
        builder.HasOne(e => e.AssetType)
            .WithMany()
            .HasForeignKey(e => e.AssetTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to LookupValues for DataSource (where data comes from)
        builder.HasOne(e => e.DataSource)
            .WithMany()
            .HasForeignKey(e => e.DataSourceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index for ingestion workers querying by tenant and data source
        builder.HasIndex(e => new { e.TenantId, e.DataSourceId });

        builder.HasMany(e => e.Metrics)
            .WithOne(m => m.Asset)
            .HasForeignKey(e => e.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Rules)
            .WithOne(r => r.Asset)
            .HasForeignKey(e => e.AssetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
