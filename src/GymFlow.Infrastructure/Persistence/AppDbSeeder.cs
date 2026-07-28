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
    private const string MemberDocument = "12345678";
    private const string MemberPassword = "Miembro123!";
    private const string DemoPlanName = "Mensual";

    // Billing SaaS (Fase 6): super-admin de plataforma y catálogo de planes.
    private const string PlatformAdminEmail = "admin@gymflow.pe";
    private const string PlatformAdminPassword = "Superadmin123!";
    private const string StarterPlanName = "Starter";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // La migración se aplica en el arranque (Program), antes de sembrar.
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

        await SeedDemoMemberAsync(tenant.Id, cancellationToken);
        await SeedPlatformBillingAsync(tenant, cancellationToken);
    }

    /// <summary>
    /// Billing SaaS (Fase 6): super-admin de plataforma, un par de planes y la suscripción del
    /// gimnasio demo. Todo a nivel plataforma (sin tenant en contexto). Idempotente.
    /// </summary>
    private async Task SeedPlatformBillingAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        var adminExists = await db.PlatformAdmins
            .AnyAsync(a => a.Email == PlatformAdminEmail, cancellationToken);

        if (!adminExists)
        {
            var admin = new PlatformAdmin(
                "Super Admin", PlatformAdminEmail, passwordHasher.Hash(PlatformAdminPassword));
            db.PlatformAdmins.Add(admin);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Seed: super-admin de plataforma '{Email}' creado (password {Pass}).",
                PlatformAdminEmail, PlatformAdminPassword);
        }

        var starter = await db.PlatformPlans
            .FirstOrDefaultAsync(p => p.Name == StarterPlanName, cancellationToken);

        if (starter is null)
        {
            starter = new PlatformPlan(StarterPlanName, 99m, billingPeriodDays: 30, maxMembers: 200);
            db.PlatformPlans.Add(starter);
            db.PlatformPlans.Add(new PlatformPlan("Pro", 199m, billingPeriodDays: 30, maxMembers: null));
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seed: planes de plataforma 'Starter' y 'Pro' creados.");
        }

        var hasSubscription = await db.Subscriptions
            .AnyAsync(s => s.TenantId == tenant.Id, cancellationToken);

        if (!hasSubscription)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var subscription = Subscription.Start(tenant.Id, starter, today);
            db.Subscriptions.Add(subscription);
            tenant.SetSubscriptionStatus(subscription.Status);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seed: gimnasio demo suscrito al plan '{Plan}'.", StarterPlanName);
        }
    }

    /// <summary>
    /// Miembro de prueba con acceso a la app (contraseña) y una membresía activa, para
    /// probar la app móvil de punta a punta en Development. Idempotente.
    /// </summary>
    private async Task SeedDemoMemberAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var plan = await db.MembershipPlans
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Name == DemoPlanName, cancellationToken);

        if (plan is null)
        {
            plan = new MembershipPlan(tenantId, DemoPlanName, 100m, 30);
            db.MembershipPlans.Add(plan);
            await db.SaveChangesAsync(cancellationToken);
        }

        var member = await db.Members
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.DocumentId == MemberDocument, cancellationToken);

        if (member is null)
        {
            member = new Member(tenantId, "Juan Pérez", MemberDocument, phone: "999888777");
            member.SetPasswordHash(passwordHasher.Hash(MemberPassword));
            db.Members.Add(member);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seed: miembro demo '{Doc}' creado (password {Pass}).", MemberDocument, MemberPassword);
        }

        var hasMembership = await db.Memberships
            .IgnoreQueryFilters()
            .AnyAsync(m => m.MemberId == member.Id, cancellationToken);

        if (!hasMembership)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            db.Memberships.Add(new Membership(tenantId, member, plan, today));
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
