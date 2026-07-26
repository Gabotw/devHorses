namespace GymFlow.Application.Features.MemberAuth;

/// <summary>Login del miembro en la app: documento (DNI) + contraseña, dentro del tenant resuelto.</summary>
public sealed record MemberLoginRequest(string DocumentId, string Password);

public sealed record MemberLoginResult(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid MemberId,
    string FullName);
