using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SignalEngine.Domain.Entities;

namespace SignalEngine.Infrastructure.Persistence.Configurations;

public class RuleConfiguration : IEntityTypeConfiguration<Rule>
{
    public void Configure(EntityTypeBuilder<Rule> builder)
    {
        builder.ToTable("Rules");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .UseIdentityColumn();

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.AssetId)
            .IsRequired();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.MetricName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.OperatorId)
            .IsRequired();

        builder.Property(e => e.Threshold)
            .IsRequired()
            .HasPrecision(18, 6);

        builder.Property(e => e.SeverityId)
            .IsRequired();

        builder.Property(e => e.EvaluationFrequencyId)
            .IsRequired();

        builder.Property(e => e.ConsecutiveBreachesRequired)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.AssetId);
        builder.HasIndex(e => e.EvaluationFrequencyId);
        builder.HasIndex(e => e.IsActive);

        // Foreign keys to LookupValues
        builder.HasOne(e => e.Operator)
            .WithMany()
            .HasForeignKey(e => e.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Severity)
            .WithMany()
            .HasForeignKey(e => e.SeverityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.EvaluationFrequency)
            .WithMany()
            .HasForeignKey(e => e.EvaluationFrequencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Signals)
            .WithOne(s => s.Rule)
            .HasForeignKey(e => e.RuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
