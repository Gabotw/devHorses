using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public sealed class CheckInConfiguration : IEntityTypeConfiguration<CheckIn>
{
    public void Configure(EntityTypeBuilder<CheckIn> builder)
    {
        builder.ToTable("check_ins");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId).IsRequired();
        builder.Property(c => c.MemberId).IsRequired();
        builder.Property(c => c.Method).HasConversion<int>().IsRequired();
        builder.Property(c => c.OccurredAtUtc).IsRequired();
        builder.Property(c => c.LocalDate).IsRequired();
        builder.Property(c => c.IsValid).IsRequired();
        builder.Property(c => c.Reason).HasMaxLength(200);

        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => new { c.TenantId, c.MemberId });
        // Acelera aforo y asistencia del día (conteo por tenant + día + validez).
        builder.HasIndex(c => new { c.TenantId, c.LocalDate, c.IsValid });

        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(c => c.MemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
