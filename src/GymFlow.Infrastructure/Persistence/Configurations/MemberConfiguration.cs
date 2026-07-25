using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GymFlow.Infrastructure.Persistence.Configurations;

public sealed class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("members");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.TenantId).IsRequired();
        builder.Property(m => m.FullName).HasMaxLength(160).IsRequired();
        builder.Property(m => m.DocumentId).HasMaxLength(32).IsRequired();
        builder.Property(m => m.Phone).HasMaxLength(32);
        builder.Property(m => m.Email).HasMaxLength(256);
        builder.Property(m => m.PhotoUrl).HasMaxLength(512);
        builder.Property(m => m.Status).HasConversion<int>().IsRequired();

        builder.HasIndex(m => m.TenantId);

        // Documento único por tenant.
        builder.HasIndex(m => new { m.TenantId, m.DocumentId }).IsUnique();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(m => m.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
