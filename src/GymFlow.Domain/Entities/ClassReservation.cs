using GymFlow.Domain.Common;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

/// <summary>
/// Reserva de un miembro a una <see cref="ClassSession"/> (Fase 7). Nace <see cref="ClassReservationStatus.Booked"/>
/// si hay cupo, o <see cref="ClassReservationStatus.Waitlisted"/> si la clase está llena. Al
/// liberarse un cupo, la reserva en espera más antigua se promueve a Booked. El orden de la lista
/// de espera se resuelve por <see cref="Entity.CreatedAtUtc"/> (FIFO).
/// </summary>
public class ClassReservation : Entity, ITenantScoped
{
    // EF Core
    private ClassReservation() { }

    private ClassReservation(Guid tenantId, Guid classSessionId, Guid memberId, ClassReservationStatus status)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId es obligatorio.", nameof(tenantId));
        if (classSessionId == Guid.Empty)
            throw new ArgumentException("ClassSessionId es obligatorio.", nameof(classSessionId));
        if (memberId == Guid.Empty)
            throw new ArgumentException("MemberId es obligatorio.", nameof(memberId));

        TenantId = tenantId;
        ClassSessionId = classSessionId;
        MemberId = memberId;
        Status = status;
    }

    public Guid TenantId { get; private set; }

    public Guid ClassSessionId { get; private set; }

    public Guid MemberId { get; private set; }

    public ClassReservationStatus Status { get; private set; }

    public DateTime? CanceledAtUtc { get; private set; }

    public DateTime? AttendedAtUtc { get; private set; }

    /// <summary>La reserva ocupa un lugar activo (con cupo o en espera).</summary>
    public bool IsActive => Status is ClassReservationStatus.Booked or ClassReservationStatus.Waitlisted;

    public static ClassReservation Booked(Guid tenantId, Guid classSessionId, Guid memberId) =>
        new(tenantId, classSessionId, memberId, ClassReservationStatus.Booked);

    public static ClassReservation Waitlisted(Guid tenantId, Guid classSessionId, Guid memberId) =>
        new(tenantId, classSessionId, memberId, ClassReservationStatus.Waitlisted);

    /// <summary>Promueve una reserva en espera a cupo confirmado (al liberarse un lugar).</summary>
    public void Promote()
    {
        if (Status != ClassReservationStatus.Waitlisted)
            throw new DomainException("Solo se puede promover una reserva en lista de espera.");
        Status = ClassReservationStatus.Booked;
        Touch();
    }

    /// <summary>Cancela la reserva (libera el cupo/lugar en espera).</summary>
    public void Cancel(DateTime nowUtc)
    {
        if (!IsActive)
            throw new DomainException("La reserva ya no está activa.");
        Status = ClassReservationStatus.Cancelled;
        CanceledAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        Touch();
    }

    /// <summary>Marca asistencia del miembro a la clase (debía tener cupo confirmado).</summary>
    public void MarkAttended(DateTime nowUtc)
    {
        if (Status != ClassReservationStatus.Booked)
            throw new DomainException("Solo un miembro con cupo confirmado puede marcar asistencia.");
        Status = ClassReservationStatus.Attended;
        AttendedAtUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        Touch();
    }
}
