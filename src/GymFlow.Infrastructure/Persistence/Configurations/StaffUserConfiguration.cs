using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public sealed class StaffUserConfiguration : IEntityTypeConfiguration<StaffUser>
{
    public void Configure(EntityTypeBuilder<StaffUser> builder)
    {
        builder.ToTable("staff_users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.TenantId).IsRequired();
        builder.Property(u => u.FullName).HasMaxLength(160).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
        builder.Property(u => u.Role).HasConversion<int>().IsRequired();
        builder.Property(u => u.IsActive).IsRequired();

        builder.HasIndex(u => u.TenantId);

        // Email único POR tenant: dos gimnasios pueden tener el mismo email.
        builder.HasIndex(u => new { u.TenantId, u.Email }).IsUnique();

        // FK al tenant. Sin propiedad de navegación para mantener el dominio limpio.
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(u => u.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
