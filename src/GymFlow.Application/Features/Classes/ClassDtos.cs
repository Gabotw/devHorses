using GymFlow.Domain.Enums;

namespace GymFlow.Application.Features.Classes;

/// <summary>Crea una sesión de clase. <paramref name="StartsAtUtc"/> es el instante UTC de inicio.</summary>
public sealed record CreateClassSessionRequest(
    string Name,
    string? InstructorName,
    DateTime StartsAtUtc,
    int DurationMinutes,
    int Capacity);

/// <summary>Vista de una sesión para el staff, con el estado de ocupación (cupos y espera).</summary>
public sealed record ClassSessionDto(
    Guid Id,
    string Name,
    string? InstructorName,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    DateOnly LocalDate,
    int DurationMinutes,
    int Capacity,
    ClassSessionStatus Status,
    int BookedCount,
    int WaitlistCount,
    int AvailableSpots);

/// <summary>Vista de una sesión para el miembro (app): ocupación + su propio estado de reserva.</summary>
public sealed record MemberClassSessionDto(
    Guid Id,
    string Name,
    string? InstructorName,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    int DurationMinutes,
    int Capacity,
    int BookedCount,
    int AvailableSpots,
    ClassReservationStatus? MyStatus);

/// <summary>Una reserva en el roster de una sesión (para el staff).</summary>
public sealed record ClassReservationDto(
    Guid Id,
    Guid ClassSessionId,
    Guid MemberId,
    string MemberName,
    ClassReservationStatus Status,
    DateTime CreatedAtUtc);

/// <summary>Reserva del miembro con los datos de la clase (para el historial/agenda de la app).</summary>
public sealed record MyReservationDto(
    Guid Id,
    Guid ClassSessionId,
    string ClassName,
    DateTime StartsAtUtc,
    ClassSessionStatus SessionStatus,
    ClassReservationStatus Status);

/// <summary>Resultado de reservar: el estado obtenido (Booked o Waitlisted) y la sesión actualizada.</summary>
public sealed record ReserveResultDto(ClassReservationStatus Status, MemberClassSessionDto Session);
