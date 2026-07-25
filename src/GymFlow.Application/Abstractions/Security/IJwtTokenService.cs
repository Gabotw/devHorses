using GymFlow.Domain.Entities;

namespace GymFlow.Application.Abstractions.Security;

/// <summary>
/// Puerto de emisión de tokens JWT. El adaptador vive en la API (conoce la config
/// de firma). Los claims incluyen sub, tenant_id y role para el RBAC por tenant.
/// </summary>
public interface IJwtTokenService
{
    AccessToken Issue(StaffUser user);
}

/// <summary>Token emitido y su expiración (UTC).</summary>
public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);
