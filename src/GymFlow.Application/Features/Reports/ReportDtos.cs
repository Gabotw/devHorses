using GymFlow.Domain.Enums;

namespace GymFlow.Application.Features.Reports;

/// <summary>Rango de fechas (locales, en la zona del tenant) que cubre el dashboard.</summary>
public sealed record ReportRangeDto(DateOnly From, DateOnly To);

/// <summary>Ingresos de un día: monto cobrado y número de pagos completados.</summary>
public sealed record RevenuePointDto(DateOnly Date, decimal Amount, int Count);

/// <summary>Ingresos agrupados por medio de pago (efectivo / pasarela).</summary>
public sealed record RevenueByMethodDto(PaymentMethod Method, decimal Amount, int Count);

/// <summary>Cantidad de membresías en un estado dado.</summary>
public sealed record MembershipStatusCountDto(MembershipStatus Status, int Count);

/// <summary>Ocupación agregada por hora del día (0–23): ingresos válidos en el rango.</summary>
public sealed record OccupancyByHourDto(int Hour, int Count);

/// <summary>
/// Foto del negocio para el panel (Fase 5). Todo en la zona del tenant: los ingresos se
/// agrupan por día/medio, la morosidad y el churn salen del estado de las membresías, y la
/// ocupación por hora agrega los ingresos válidos del rango. El dinero es siempre decimal.
/// </summary>
public sealed record DashboardDto(
    ReportRangeDto Range,
    decimal RevenueTotal,
    int PaymentsCount,
    decimal AverageTicket,
    int OverdueMemberships,
    decimal OverdueAmount,
    int TotalMembers,
    int ActiveMembers,
    int NewMembers,
    int ActiveMemberships,
    decimal ChurnRate,
    decimal RetentionRate,
    IReadOnlyList<RevenuePointDto> RevenueByDay,
    IReadOnlyList<RevenueByMethodDto> RevenueByMethod,
    IReadOnlyList<MembershipStatusCountDto> MembershipsByStatus,
    IReadOnlyList<OccupancyByHourDto> OccupancyByHour);
