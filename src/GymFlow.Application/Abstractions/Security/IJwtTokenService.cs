using GymFlow.Domain.Entities;

namespace GymFlow.Application.Abstractions.Security;

/// <summary>
/// Puerto de emisión de tokens JWT. El adaptador vive en la API (conoce la config
/// de firma). Los claims incluyen sub, tenant_id y role para el RBAC por tenant.
/// </summary>
public interface IJwtTokenService
{
    AccessToken Issue(StaffUser user);

    /// <summary>Emite un token para un miembro (app móvil). Lleva sub=memberId, tenant_id y actor=member.</summary>
    AccessToken IssueForMember(Member member);

    /// <summary>Emite un token para un super-admin de plataforma. Lleva sub=adminId y actor=platform, SIN tenant_id.</summary>
    AccessToken IssueForPlatformAdmin(PlatformAdmin admin);
}

/// <summary>Token emitido y su expiración (UTC).</summary>
public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);
