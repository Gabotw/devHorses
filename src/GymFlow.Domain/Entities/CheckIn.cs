using GymFlow.Domain.Common;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

/// <summary>
/// Registro de asistencia de un miembro. Un check-in es <see cref="IsValid"/> cuando el
/// miembro tenía una membresía vigente al ingresar; si no, se guarda igual pero marcado
/// como no válido con el <see cref="Reason"/> (queda traza de intentos sin membresía).
///
/// Guarda el instante en UTC (<see cref="OccurredAtUtc"/>) y además el día de calendario
/// en la zona del tenant (<see cref="LocalDate"/>) para agrupar la asistencia/aforo del día
/// sin recalcular rangos horarios en cada consulta.
/// </summary>
public class CheckIn : Entity, ITenantScoped
{
    // EF Core
    private CheckIn() { }

    private CheckIn(
        Guid tenantId,
        Guid memberId,
        CheckInMethod method,
        DateTime occurredAtUtc,
        DateOnly localDate,
        bool isValid,
        string? reason)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId es obligatorio.", nameof(tenantId));
        if (memberId == Guid.Empty)
            throw new ArgumentException("MemberId es obligatorio.", nameof(memberId));

        TenantId = tenantId;
        MemberId = memberId;
        Method = method;
        OccurredAtUtc = DateTime.SpecifyKind(occurredAtUtc, DateTimeKind.Utc);
        LocalDate = localDate;
        IsValid = isValid;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    public Guid TenantId { get; private set; }

    public Guid MemberId { get; private set; }

    public CheckInMethod Method { get; private set; }

    /// <summary>Instante del ingreso, en UTC.</summary>
    public DateTime OccurredAtUtc { get; private set; }

    /// <summary>Día de calendario del ingreso en la zona del tenant (para agrupar el día).</summary>
    public DateOnly LocalDate { get; private set; }

    public bool IsValid { get; private set; }

    /// <summary>Motivo cuando el ingreso no es válido (p.ej. "sin membresía vigente").</summary>
    public string? Reason { get; private set; }

    /// <summary>Ingreso válido: el miembro tenía membresía vigente.</summary>
    public static CheckIn Valid(
        Guid tenantId, Guid memberId, CheckInMethod method, DateTime occurredAtUtc, DateOnly localDate) =>
        new(tenantId, memberId, method, occurredAtUtc, localDate, isValid: true, reason: null);

    /// <summary>Ingreso rechazado: se registra la traza con el motivo.</summary>
    public static CheckIn Rejected(
        Guid tenantId, Guid memberId, CheckInMethod method, DateTime occurredAtUtc, DateOnly localDate, string reason) =>
        new(tenantId, memberId, method, occurredAtUtc, localDate, isValid: false,
            reason: string.IsNullOrWhiteSpace(reason) ? "Ingreso no válido." : reason);
}
