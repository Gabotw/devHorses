using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public sealed class PlatformPlanConfiguration : IEntityTypeConfiguration<PlatformPlan>
{
    public void Configure(EntityTypeBuilder<PlatformPlan> builder)
    {
        builder.ToTable("platform_plans");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).HasMaxLength(120).IsRequired();
        // Dinero: decimal con precisión fija.
        builder.Property(p => p.MonthlyPrice).HasPrecision(12, 2).IsRequired();
        builder.Property(p => p.BillingPeriodDays).IsRequired();
        builder.Property(p => p.MaxMembers);
        builder.Property(p => p.IsActive).IsRequired();
        builder.Property(p => p.CreatedAtUtc).IsRequired();

        // Nombre único a nivel plataforma (no hay tenant).
        builder.HasIndex(p => p.Name).IsUnique();
    }
}
