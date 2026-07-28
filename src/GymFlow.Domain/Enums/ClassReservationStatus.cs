namespace GymFlow.Domain.Enums;

/// <summary>
/// Estado de la reserva de un miembro a una sesión de clase (Fase 7).
/// <see cref="Booked"/> = con cupo confirmado; <see cref="Waitlisted"/> = en lista de espera
/// (se promueve a Booked si se libera un cupo). <see cref="Attended"/> = asistió.
/// </summary>
public enum ClassReservationStatus
{
    Booked = 1,
    Waitlisted = 2,
    Cancelled = 3,
    Attended = 4,
}
