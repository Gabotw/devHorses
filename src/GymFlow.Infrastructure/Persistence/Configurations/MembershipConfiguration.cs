using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public sealed class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("memberships");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.TenantId).IsRequired();
        builder.Property(m => m.MemberId).IsRequired();
        builder.Property(m => m.PlanId).IsRequired();
        builder.Property(m => m.PriceAtPurchase).HasPrecision(12, 2).IsRequired();
        builder.Property(m => m.DurationDaysAtPurchase).IsRequired();
        builder.Property(m => m.StartDate).IsRequired();
        builder.Property(m => m.EndDate).IsRequired();
        builder.Property(m => m.Status).HasConversion<int>().IsRequired();
        builder.Property(m => m.FrozenFrom);
        builder.Property(m => m.FrozenUntil);

        builder.HasIndex(m => m.TenantId);
        builder.HasIndex(m => new { m.TenantId, m.MemberId });
        // Acelera la consulta de morosidad (Fase 2) y de vencimientos.
        builder.HasIndex(m => new { m.TenantId, m.Status, m.EndDate });

        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(m => m.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<MembershipPlan>()
            .WithMany()
            .HasForeignKey(m => m.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
