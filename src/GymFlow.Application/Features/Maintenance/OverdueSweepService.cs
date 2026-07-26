using GymFlow.Application.Abstractions.Persistence;
using GymFlow.Application.Abstractions.Time;
using GymFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.Features.Maintenance;

/// <summary>
/// Marca como morosas las membresías activas cuyo vencimiento ya pasó, evaluando "hoy"
/// en la zona horaria de cada tenant. Corre sin tenant resuelto (job de background):
/// por eso ignora los global query filters y acota explícitamente por TenantId.
/// </summary>
public sealed class OverdueSweepService(IAppDbContext db, IClock clock) : IOverdueSweepService
{
    public async Task<int> RunAsync(CancellationToken ct = default)
    {
        var tenants = await db.Tenants.AsNoTracking()
            .Select(t => new { t.Id, t.TimeZoneId })
            .ToListAsync(ct);

        var marked = 0;
        foreach (var t in tenants)
        {
            var today = clock.TodayIn(t.TimeZoneId);

            var due = await db.Memberships
                .IgnoreQueryFilters()
                .Where(m => m.TenantId == t.Id
                            && m.Status == MembershipStatus.Active
                            && m.EndDate < today)
                .ToListAsync(ct);

            foreach (var membership in due)
            {
                membership.MarkOverdue();
                marked++;
            }
        }

        if (marked > 0)
            await db.SaveChangesAsync(ct);

        return marked;
    }
}
