namespace GymFlow.Application.Features.Memberships;

public interface IMembershipService
{
    Task<IReadOnlyList<MembershipDto>> ListByMemberAsync(Guid memberId, CancellationToken ct = default);

    /// <summary>Membresía vigente (activa o congelada) del miembro, o null si no tiene.</summary>
    Task<MembershipDto?> GetCurrentAsync(Guid memberId, CancellationToken ct = default);

    /// <summary>Membresías que vencen dentro de <paramref name="withinDays"/> días (o ya vencidas/morosas),
    /// de miembros activos, para la lista de avisos de recepción.</summary>
    Task<IReadOnlyList<ExpiringMembershipDto>> ListExpiringAsync(int withinDays, CancellationToken ct = default);

    Task<MembershipDto> CreateAsync(CreateMembershipRequest request, CancellationToken ct = default);

    Task<MembershipDto> FreezeAsync(Guid membershipId, FreezeMembershipRequest request, CancellationToken ct = default);

    Task<MembershipDto> UnfreezeAsync(Guid membershipId, UnfreezeMembershipRequest request, CancellationToken ct = default);
}
