namespace GymFlow.Application.Features.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResult(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid UserId,
    string FullName,
    string Role);
