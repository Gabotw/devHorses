using GymFlow.Domain.Common;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

/// <summary>
/// Sesión de clase programada del gimnasio (p.ej. "Yoga 7:00pm"), Fase 7. Es un evento con
/// fecha/hora concreta y un cupo (<see cref="Capacity"/>); los miembros reservan un lugar y,
/// si se llena, entran a lista de espera (ver <see cref="ClassReservation"/>).
///
/// Guarda el instante en UTC (<see cref="StartsAtUtc"/>) y el día de calendario en la zona del
/// tenant (<see cref="LocalDate"/>) para listar/agrupar por día sin recalcular rangos horarios,
/// igual que <see cref="CheckIn"/>.
/// </summary>
public class ClassSession : Entity, ITenantScoped
{
    // EF Core
    private ClassSession() { }

    public ClassSession(
        Guid tenantId,
        string name,
        string? instructorName,
        DateTime startsAtUtc,
        DateOnly localDate,
        int durationMinutes,
        int capacity)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId es obligatorio.", nameof(tenantId));

        TenantId = tenantId;
        SetName(name);
        SetInstructor(instructorName);
        StartsAtUtc = DateTime.SpecifyKind(startsAtUtc, DateTimeKind.Utc);
        LocalDate = localDate;
        SetDuration(durationMinutes);
        SetCapacity(capacity);
        Status = ClassSessionStatus.Scheduled;
    }

    public Guid TenantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? InstructorName { get; private set; }

    /// <summary>Inicio de la clase, en UTC.</summary>
    public DateTime StartsAtUtc { get; private set; }

    /// <summary>Día de calendario de la clase en la zona del tenant (para agrupar por día).</summary>
    public DateOnly LocalDate { get; private set; }

    public int DurationMinutes { get; private set; }

    /// <summary>Cupos totales de la clase. Los que exceden entran a lista de espera.</summary>
    public int Capacity { get; private set; }

    public ClassSessionStatus Status { get; private set; }

    public DateTime EndsAtUtc => StartsAtUtc.AddMinutes(DurationMinutes);

    public bool IsCancelled => Status == ClassSessionStatus.Cancelled;

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre de la clase es obligatorio.");
        Name = name.Trim();
        Touch();
    }

    public void SetInstructor(string? instructorName)
    {
        InstructorName = string.IsNullOrWhiteSpace(instructorName) ? null : instructorName.Trim();
        Touch();
    }

    public void SetDuration(int durationMinutes)
    {
        if (durationMinutes <= 0)
            throw new DomainException("La duración debe ser de al menos 1 minuto.");
        DurationMinutes = durationMinutes;
        Touch();
    }

    public void SetCapacity(int capacity)
    {
        if (capacity <= 0)
            throw new DomainException("El cupo debe ser de al menos 1.");
        Capacity = capacity;
        Touch();
    }

    /// <summary>Cancela la sesión (las reservas se liberan en la capa de aplicación).</summary>
    public void Cancel()
    {
        if (Status == ClassSessionStatus.Cancelled)
            return;
        Status = ClassSessionStatus.Cancelled;
        Touch();
    }
}
