using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GymFlow.Application.Abstractions.Security;
using GymFlow.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GymFlow.Api.Security;

/// <summary>
/// Emite JWT firmados HMAC-SHA256 con los claims necesarios para el RBAC por tenant:
/// sub (userId), tenant_id, role, name y email.
/// </summary>
public sealed class JwtTokenService(IOptions<JwtSettings> options) : IJwtTokenService
{
    private readonly JwtSettings _settings = options.Value;

    public AccessToken Issue(StaffUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(GymFlowClaims.TenantId, user.TenantId.ToString()),
            new(GymFlowClaims.ActorType, GymFlowClaims.ActorStaff),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(JwtRegisteredClaimNames.Name, user.FullName),
            new(JwtRegisteredClaimNames.Email, user.Email),
        };
        return Build(claims);
    }

    private AccessToken Build(IEnumerable<Claim> claims)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessToken(jwt, expiresAt);
    }
}
