using GymFlow.Application.Abstractions.Security;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymFlow.Infrastructure.Persistence;

/// <summary>
/// Seed idempotente del gimnasio de validación (Fase 0). Crea un tenant y su owner
/// si aún no existen. Corre fuera de request: no hay tenant en contexto, así que el
/// TenantId del owner es el que fija su constructor de dominio.
/// </summary>
public sealed class AppDbSeeder(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    ILogger<AppDbSeeder> logger)
{
    private const string Subdomain = "demo";
    private const string OwnerEmail = "owner@demo.gymflow.pe";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);

        var tenant = await db.Tenants
            .FirstOrDefaultAsync(t => t.Subdomain == Subdomain, cancellationToken);

        if (tenant is null)
        {
            tenant = new Tenant("Gimnasio Demo", Subdomain, "America/Lima");
            tenant.SetSubscriptionStatus(TenantSubscriptionStatus.Active);
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seed: tenant '{Subdomain}' creado ({TenantId}).", Subdomain, tenant.Id);
        }

        // IgnoreQueryFilters porque el seed corre sin tenant en contexto.
        var ownerExists = await db.StaffUsers
            .IgnoreQueryFilters()
            .AnyAsync(u => u.TenantId == tenant.Id && u.Email == OwnerEmail, cancellationToken);

        if (!ownerExists)
        {
            var owner = new StaffUser(
                tenant.Id,
                "Dueño Demo",
                OwnerEmail,
                passwordHasher.Hash("Cambiar123!"),
                StaffRole.Owner);
            db.StaffUsers.Add(owner);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seed: owner '{Email}' creado para tenant {TenantId}.", OwnerEmail, tenant.Id);
        }
    }
}
