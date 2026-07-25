using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).HasMaxLength(160).IsRequired();
        builder.Property(t => t.Subdomain).HasMaxLength(63).IsRequired();
        builder.Property(t => t.TimeZoneId).HasMaxLength(64).IsRequired();
        builder.Property(t => t.SubscriptionStatus).HasConversion<int>().IsRequired();

        builder.Property(t => t.CreatedAtUtc).IsRequired();

        // Subdominio único a nivel plataforma: es la llave de resolución de tenant.
        builder.HasIndex(t => t.Subdomain).IsUnique();
    }
}
