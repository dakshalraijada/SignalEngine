using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SignalEngine.Domain.Entities;

namespace SignalEngine.Infrastructure.Persistence.Configurations;

public class SignalStateConfiguration : IEntityTypeConfiguration<SignalState>
{
    public void Configure(EntityTypeBuilder<SignalState> builder)
    {
        builder.ToTable("SignalStates");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .UseIdentityColumn();

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.RuleId)
            .IsRequired();

        builder.Property(e => e.ConsecutiveBreaches)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.LastEvaluatedAt)
            .IsRequired();

        builder.Property(e => e.LastMetricValue)
            .HasPrecision(18, 6);

        builder.Property(e => e.IsBreached)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.RuleId)
            .IsUnique();

        builder.HasIndex(e => e.TenantId);

        // Foreign key to Rules (one state per rule)
        builder.HasOne<Rule>()
            .WithMany()
            .HasForeignKey(e => e.RuleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to Tenants
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
