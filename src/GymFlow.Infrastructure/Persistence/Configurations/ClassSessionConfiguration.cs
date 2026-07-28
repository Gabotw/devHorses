using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public sealed class ClassSessionConfiguration : IEntityTypeConfiguration<ClassSession>
{
    public void Configure(EntityTypeBuilder<ClassSession> builder)
    {
        builder.ToTable("class_sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.TenantId).IsRequired();
        builder.Property(s => s.Name).HasMaxLength(120).IsRequired();
        builder.Property(s => s.InstructorName).HasMaxLength(120);
        builder.Property(s => s.StartsAtUtc).IsRequired();
        builder.Property(s => s.LocalDate).IsRequired();
        builder.Property(s => s.DurationMinutes).IsRequired();
        builder.Property(s => s.Capacity).IsRequired();
        builder.Property(s => s.Status).HasConversion<int>().IsRequired();

        // Listado por día en la zona del tenant.
        builder.HasIndex(s => new { s.TenantId, s.LocalDate });

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
