using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SignalEngine.Domain.Entities;

namespace SignalEngine.Infrastructure.Persistence.Configurations;

public class SignalResolutionConfiguration : IEntityTypeConfiguration<SignalResolution>
{
    public void Configure(EntityTypeBuilder<SignalResolution> builder)
    {
        builder.ToTable("SignalResolutions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .UseIdentityColumn();

        builder.Property(e => e.TenantId)
            .IsRequired();

        builder.Property(e => e.SignalId)
            .IsRequired();

        builder.Property(e => e.ResolutionStatusId)
            .IsRequired();

        builder.Property(e => e.ResolvedByUserId)
            .IsRequired();

        builder.Property(e => e.Notes)
            .HasMaxLength(2000);

        builder.Property(e => e.ResolvedAt)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => e.SignalId)
            .IsUnique(); // One resolution per signal

        // Foreign keys (with navigation properties)
        builder.HasOne(e => e.Signal)
            .WithOne(s => s.Resolution)
            .HasForeignKey<SignalResolution>(e => e.SignalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ResolutionStatus)
            .WithMany()
            .HasForeignKey(e => e.ResolutionStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
