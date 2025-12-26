using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SignalEngine.Domain.Entities;

namespace SignalEngine.Infrastructure.Persistence.Configurations;

public class SignalConfiguration : IEntityTypeConfiguration<Signal>
{
    public void Configure(EntityTypeBuilder<Signal> builder)
    {
        builder.ToTable("Signals");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .UseIdentityColumn();

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.RuleId)
            .IsRequired();

        builder.Property(e => e.AssetId)
            .IsRequired();

        builder.Property(e => e.SignalStatusId)
            .IsRequired();

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.TriggerValue)
            .IsRequired()
            .HasPrecision(18, 6);

        builder.Property(e => e.ThresholdValue)
            .IsRequired()
            .HasPrecision(18, 6);

        builder.Property(e => e.TriggeredAt)
            .IsRequired();

        builder.Property(e => e.ResolutionNotes)
            .HasMaxLength(2000);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.RuleId);
        builder.HasIndex(e => e.SignalStatusId);
        builder.HasIndex(e => e.TriggeredAt);

        builder.HasMany(e => e.Notifications)
            .WithOne()
            .HasForeignKey(e => e.SignalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
