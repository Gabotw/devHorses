using GymFlow.Application.Abstractions.Persistence;
using GymFlow.Application.Abstractions.Tenancy;
using GymFlow.Application.Abstractions.Time;
using GymFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.Features.Reports;

/// <summary>
/// Arma el dashboard del tenant (ingresos, morosidad, churn, ocupación por hora). Los
/// timestamps viven en UTC; para agrupar por día/hora se convierten a la zona del tenant en
/// memoria (los rangos son acotados, así que el volumen es chico). El dinero es decimal.
/// </summary>
public sealed class ReportService(
    IAppDbContext db,
    ITenantProvider tenant,
    IClock clock) : IReportService
{
    /// <summary>Ventana por defecto del dashboard (incluye hoy).</summary>
    private const int DefaultWindowDays = 30;

    /// <summary>Tope de días del rango para no barrer históricos enormes en una sola consulta.</summary>
    private const int MaxWindowDays = 366;

    public async Task<DashboardDto> GetDashboardAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var tenantId = tenant.GetRequiredTenantId();
        var timeZoneId = await TenantTimeZoneAsync(tenantId, ct);
        var today = clock.TodayIn(timeZoneId);

        var (rangeFrom, rangeTo) = NormalizeRange(from, to, today);
        var fromUtc = clock.StartOfDayUtc(rangeFrom, timeZoneId);
        var toUtcExclusive = clock.StartOfDayUtc(rangeTo.AddDays(1), timeZoneId);

        var revenueByDay = await BuildRevenueAsync(rangeFrom, rangeTo, fromUtc, toUtcExclusive, timeZoneId, ct);
        var revenueByMethod = await BuildRevenueByMethodAsync(fromUtc, toUtcExclusive, ct);
        var membershipsByStatus = await BuildMembershipsByStatusAsync(ct);
        var occupancyByHour = await BuildOccupancyByHourAsync(rangeFrom, rangeTo, timeZoneId, ct);

        var revenueTotal = revenueByDay.Sum(p => p.Amount);
        var paymentsCount = revenueByDay.Sum(p => p.Count);
        var averageTicket = paymentsCount == 0
            ? 0m
            : decimal.Round(revenueTotal / paymentsCount, 2, MidpointRounding.AwayFromZero);

        var overdue = membershipsByStatus.FirstOrDefault(s => s.Status == MembershipStatus.Overdue)?.Count ?? 0;
        var overdueAmount = await db.Memberships.AsNoTracking()
            .Where(m => m.Status == MembershipStatus.Overdue)
            .SumAsync(m => (decimal?)m.PriceAtPurchase, ct) ?? 0m;

        var totalMembers = await db.Members.AsNoTracking().CountAsync(ct);
        var activeMembers = await db.Members.AsNoTracking()
            .CountAsync(m => m.Status == MemberStatus.Active, ct);
        var newMembers = await db.Members.AsNoTracking()
            .CountAsync(m => m.CreatedAtUtc >= fromUtc && m.CreatedAtUtc < toUtcExclusive, ct);

        var activeMemberships = membershipsByStatus.FirstOrDefault(s => s.Status == MembershipStatus.Active)?.Count ?? 0;
        var lapsed = membershipsByStatus
            .Where(s => s.Status is MembershipStatus.Expired or MembershipStatus.Overdue)
            .Sum(s => s.Count);
        var retentionBase = activeMemberships + lapsed;
        var churnRate = retentionBase == 0
            ? 0m
            : decimal.Round((decimal)lapsed / retentionBase, 4, MidpointRounding.AwayFromZero);
        var retentionRate = retentionBase == 0 ? 0m : decimal.Round(1m - churnRate, 4, MidpointRounding.AwayFromZero);

        return new DashboardDto(
            new ReportRangeDto(rangeFrom, rangeTo),
            RevenueTotal: revenueTotal,
            PaymentsCount: paymentsCount,
            AverageTicket: averageTicket,
            OverdueMemberships: overdue,
            OverdueAmount: overdueAmount,
            TotalMembers: totalMembers,
            ActiveMembers: activeMembers,
            NewMembers: newMembers,
            ActiveMemberships: activeMemberships,
            ChurnRate: churnRate,
            RetentionRate: retentionRate,
            RevenueByDay: revenueByDay,
            RevenueByMethod: revenueByMethod,
            MembershipsByStatus: membershipsByStatus,
            OccupancyByHour: occupancyByHour);
    }

    private async Task<IReadOnlyList<RevenuePointDto>> BuildRevenueAsync(
        DateOnly rangeFrom, DateOnly rangeTo, DateTime fromUtc, DateTime toUtcExclusive,
        string timeZoneId, CancellationToken ct)
    {
        var paid = await db.Payments.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Completed
                && p.PaidAtUtc != null
                && p.PaidAtUtc >= fromUtc && p.PaidAtUtc < toUtcExclusive)
            .Select(p => new { p.PaidAtUtc, p.Amount })
            .ToListAsync(ct);

        var byDay = paid
            .GroupBy(p => DateOnly.FromDateTime(clock.ToLocalTime(p.PaidAtUtc!.Value, timeZoneId)))
            .ToDictionary(g => g.Key, g => (Amount: g.Sum(x => x.Amount), Count: g.Count()));

        // Serie completa con ceros para que la gráfica cubra todo el rango sin huecos.
        var points = new List<RevenuePointDto>();
        for (var day = rangeFrom; day <= rangeTo; day = day.AddDays(1))
        {
            var bucket = byDay.TryGetValue(day, out var v) ? v : (Amount: 0m, Count: 0);
            points.Add(new RevenuePointDto(day, bucket.Amount, bucket.Count));
        }

        return points;
    }

    private async Task<IReadOnlyList<RevenueByMethodDto>> BuildRevenueByMethodAsync(
        DateTime fromUtc, DateTime toUtcExclusive, CancellationToken ct)
    {
        var grouped = await db.Payments.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Completed
                && p.PaidAtUtc != null
                && p.PaidAtUtc >= fromUtc && p.PaidAtUtc < toUtcExclusive)
            .GroupBy(p => p.Method)
            .Select(g => new RevenueByMethodDto(g.Key, g.Sum(x => x.Amount), g.Count()))
            .ToListAsync(ct);

        return grouped.OrderByDescending(g => g.Amount).ToList();
    }

    private async Task<IReadOnlyList<MembershipStatusCountDto>> BuildMembershipsByStatusAsync(CancellationToken ct)
    {
        return await db.Memberships.AsNoTracking()
            .GroupBy(m => m.Status)
            .Select(g => new MembershipStatusCountDto(g.Key, g.Count()))
            .ToListAsync(ct);
    }

    private async Task<IReadOnlyList<OccupancyByHourDto>> BuildOccupancyByHourAsync(
        DateOnly rangeFrom, DateOnly rangeTo, string timeZoneId, CancellationToken ct)
    {
        var occurred = await db.CheckIns.AsNoTracking()
            .Where(c => c.IsValid && c.LocalDate >= rangeFrom && c.LocalDate <= rangeTo)
            .Select(c => c.OccurredAtUtc)
            .ToListAsync(ct);

        var byHour = occurred
            .GroupBy(utc => clock.ToLocalTime(utc, timeZoneId).Hour)
            .ToDictionary(g => g.Key, g => g.Count());

        return Enumerable.Range(0, 24)
            .Select(h => new OccupancyByHourDto(h, byHour.TryGetValue(h, out var c) ? c : 0))
            .ToList();
    }

    private static (DateOnly From, DateOnly To) NormalizeRange(DateOnly? from, DateOnly? to, DateOnly today)
    {
        var rangeTo = to ?? today;
        var rangeFrom = from ?? rangeTo.AddDays(-(DefaultWindowDays - 1));

        if (rangeFrom > rangeTo)
            (rangeFrom, rangeTo) = (rangeTo, rangeFrom);

        // Acota el ancho para no barrer históricos gigantes en una consulta.
        if (rangeTo.DayNumber - rangeFrom.DayNumber > MaxWindowDays)
            rangeFrom = rangeTo.AddDays(-MaxWindowDays);

        return (rangeFrom, rangeTo);
    }

    private async Task<string> TenantTimeZoneAsync(Guid tenantId, CancellationToken ct)
    {
        return await db.Tenants.AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => t.TimeZoneId)
            .FirstOrDefaultAsync(ct) ?? "America/Lima";
    }
}
