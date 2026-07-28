using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.TenantId).IsRequired();
        builder.Property(s => s.PlatformPlanId).IsRequired();
        builder.Property(s => s.PlanName).HasMaxLength(120).IsRequired();
        builder.Property(s => s.PriceAtSubscription).HasPrecision(12, 2).IsRequired();
        builder.Property(s => s.BillingPeriodDays).IsRequired();
        builder.Property(s => s.Status).HasConversion<int>().IsRequired();
        builder.Property(s => s.CurrentPeriodStart).IsRequired();
        builder.Property(s => s.CurrentPeriodEnd).IsRequired();
        builder.Property(s => s.CreatedAtUtc).IsRequired();

        // A lo sumo una suscripción por tenant (la vigente).
        builder.HasIndex(s => s.TenantId).IsUnique();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<PlatformPlan>()
            .WithMany()
            .HasForeignKey(s => s.PlatformPlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
