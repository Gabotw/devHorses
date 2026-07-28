using GymFlow.Application.Abstractions.Persistence;
using GymFlow.Application.Abstractions.Time;
using GymFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.Features.Platform;

/// <summary>
/// Marca morosas (PastDue) las suscripciones vigentes (activas o trial) cuyo período de
/// facturación ya venció, evaluando "hoy" en la zona horaria de cada tenant. Sincroniza el
/// estado cacheado en el tenant. Corre sin tenant resuelto: Subscriptions y Tenants son de
/// nivel plataforma (sin global query filter), así que no hace falta IgnoreQueryFilters.
/// </summary>
public sealed class SaasBillingSweepService(IAppDbContext db, IClock clock) : ISaasBillingSweepService
{
    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        var timeZones = await db.Tenants.AsNoTracking()
            .ToDictionaryAsync(t => t.Id, t => t.TimeZoneId, ct);

        var vigentes = await db.Subscriptions
            .Where(s => s.Status == TenantSubscriptionStatus.Active
                     || s.Status == TenantSubscriptionStatus.Trial)
            .ToListAsync(ct);

        var marked = 0;
        foreach (var sub in vigentes)
        {
            var tz = timeZones.GetValueOrDefault(sub.TenantId) ?? "America/Lima";
            var today = clock.TodayIn(tz);
            if (sub.CurrentPeriodEnd >= today)
                continue;

            sub.MarkPastDue();

            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == sub.TenantId, ct);
            tenant?.SetSubscriptionStatus(sub.Status);
            marked++;
        }

        if (marked > 0)
            await db.SaveChangesAsync(ct);

        return marked;
    }
}
