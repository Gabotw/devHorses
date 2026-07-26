using GymFlow.Application.Abstractions.Persistence;
using GymFlow.Application.Abstractions.Tenancy;
using GymFlow.Domain.Common;
using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Persistence;

/// <summary>
/// DbContext raíz. Aplica multi-tenancy con global query filters por TenantId:
/// toda entidad ITenantScoped queda automáticamente acotada al tenant de la request.
/// Además, en SaveChanges asigna el TenantId a las entidades nuevas y bloquea
/// cualquier intento de escribir/leer fuera del tenant resuelto.
/// </summary>
public sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ITenantProvider tenantProvider) : DbContext(options), IAppDbContext
{
    private readonly ITenantProvider _tenantProvider = tenantProvider;

    /// <summary>
    /// TenantId vivo de la request. Se referencia dentro del query filter para que
    /// EF Core lo re-evalúe en cada consulta (no se congela al construir el modelo).
    /// </summary>
    private Guid CurrentTenantId => _tenantProvider.TenantId ?? Guid.Empty;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<StaffUser> StaffUsers => Set<StaffUser>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<MembershipPlan> MembershipPlans => Set<MembershipPlan>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica configuraciones IEntityTypeConfiguration de este ensamblado.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Filtro global por tenant para toda entidad ITenantScoped.
        modelBuilder.Entity<StaffUser>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        modelBuilder.Entity<Member>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        modelBuilder.Entity<MembershipPlan>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        modelBuilder.Entity<Membership>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        modelBuilder.Entity<Payment>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
    }

    public override int SaveChanges()
    {
        ApplyTenantOnSave();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTenantOnSave();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Defensa en profundidad: al guardar, asigna el TenantId de la request a las
    /// entidades nuevas y rechaza modificar entidades de otro tenant. El filtro global
    /// ya evita leerlas, pero esto cierra el flanco de escritura.
    /// </summary>
    private void ApplyTenantOnSave()
    {
        if (!_tenantProvider.HasTenant)
            return;

        var tenantId = _tenantProvider.GetRequiredTenantId();

        foreach (var entry in ChangeTracker.Entries<ITenantScoped>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    // TenantId es privado en la entidad; se fija vía la propiedad sombreada
                    // solo si viene vacío (respeta el asignado por el constructor de dominio).
                    if (entry.Entity.TenantId == Guid.Empty)
                        entry.Property(nameof(ITenantScoped.TenantId)).CurrentValue = tenantId;
                    else if (entry.Entity.TenantId != tenantId)
                        throw new InvalidOperationException(
                            "Intento de crear una entidad para un tenant distinto al de la request.");
                    break;

                case EntityState.Modified:
                case EntityState.Deleted:
                    if (entry.Entity.TenantId != tenantId)
                        throw new InvalidOperationException(
                            "Intento de modificar una entidad fuera del tenant de la request.");
                    break;
            }
        }
    }
}
