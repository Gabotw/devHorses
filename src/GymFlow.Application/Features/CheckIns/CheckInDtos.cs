using GymFlow.Domain.Enums;

namespace GymFlow.Application.Features.CheckIns;

/// <summary>Solicita registrar un ingreso. Method opcional: por defecto recepción (web).</summary>
public sealed record RegisterCheckInRequest(Guid MemberId, CheckInMethod? Method);

/// <summary>Registra un ingreso por el código de acceso de 4 dígitos del miembro.</summary>
public sealed record RegisterByCodeRequest(string Code);

public sealed record CheckInDto(
    Guid Id,
    Guid MemberId,
    string MemberName,
    CheckInMethod Method,
    DateTime OccurredAtUtc,
    bool IsValid,
    string? Reason);

/// <summary>Resultado de registrar un check-in: el registro más el aforo resultante.</summary>
public sealed record CheckInResultDto(CheckInDto CheckIn, int Occupancy);
