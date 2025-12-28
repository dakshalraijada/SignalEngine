using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SignalEngine.Domain.Entities;

namespace SignalEngine.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .UseIdentityColumn();

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.SignalId)
            .IsRequired();

        builder.Property(e => e.ChannelTypeId)
            .IsRequired();

        builder.Property(e => e.Recipient)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Body)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.IsSent)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(e => e.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.SignalId);
        builder.HasIndex(e => new { e.TenantId, e.IsSent, e.CreatedAt })
            .HasDatabaseName("IX_Notifications_TenantId_IsSent_CreatedAt");

        // Foreign key to Tenants
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to LookupValues for ChannelType
        builder.HasOne(e => e.ChannelType)
            .WithMany()
            .HasForeignKey(e => e.ChannelTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
