using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public sealed class ClassReservationConfiguration : IEntityTypeConfiguration<ClassReservation>
{
    public void Configure(EntityTypeBuilder<ClassReservation> builder)
    {
        builder.ToTable("class_reservations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.TenantId).IsRequired();
        builder.Property(r => r.ClassSessionId).IsRequired();
        builder.Property(r => r.MemberId).IsRequired();
        builder.Property(r => r.Status).HasConversion<int>().IsRequired();

        // Un miembro no puede tener dos reservas ACTIVAS (Booked=1 / Waitlisted=2) en la misma
        // sesión; tras cancelar sí puede volver a reservar (índice parcial, solo Postgres).
        builder.HasIndex(r => new { r.ClassSessionId, r.MemberId })
            .IsUnique()
            .HasFilter("\"Status\" IN (1, 2)");

        builder.HasIndex(r => r.MemberId);

        builder.HasOne<ClassSession>()
            .WithMany()
            .HasForeignKey(r => r.ClassSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Member>()
            .WithMany()
            .HasForeignKey(r => r.MemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
