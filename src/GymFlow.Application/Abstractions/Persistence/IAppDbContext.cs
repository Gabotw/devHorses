using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.Abstractions.Persistence;

/// <summary>
/// Puerto de acceso a datos que expone la Application sin conocer EF Core concreto.
/// El adaptador (AppDbContext) aplica los global query filters por TenantId.
/// Se irán agregando DbSets por bounded context conforme avanzan las fases.
/// </summary>
public interface IAppDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<StaffUser> StaffUsers { get; }
    DbSet<Member> Members { get; }
    DbSet<MembershipPlan> MembershipPlans { get; }
    DbSet<Membership> Memberships { get; }
    DbSet<Payment> Payments { get; }
    DbSet<CheckIn> CheckIns { get; }

    // Billing del SaaS (Fase 6) — entidades a nivel plataforma (sin global query filter).
    DbSet<PlatformPlan> PlatformPlans { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<PlatformAdmin> PlatformAdmins { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
