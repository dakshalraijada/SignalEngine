using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SignalEngine.Domain.Entities;

namespace SignalEngine.Infrastructure.Persistence.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("Plans");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .UseIdentityColumn();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.PlanCodeId)
            .IsRequired();

        builder.Property(e => e.MaxRules)
            .IsRequired();

        builder.Property(e => e.MaxAssets)
            .IsRequired();

        builder.Property(e => e.MaxNotificationsPerDay)
            .IsRequired();

        builder.Property(e => e.AllowWebhook)
            .IsRequired();

        builder.Property(e => e.AllowSlack)
            .IsRequired();

        builder.Property(e => e.MonthlyPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .IsRequired();
    }
}
