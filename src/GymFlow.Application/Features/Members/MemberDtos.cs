using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.Features.Members;

public sealed record CreateMemberRequest(
    string FullName,
    string DocumentId,
    string? Phone,
    string? Email,
    string? PhotoUrl);

public sealed record UpdateMemberRequest(
    string FullName,
    string DocumentId,
    string? Phone,
    string? Email,
    string? PhotoUrl);

public sealed record MemberDto(
    Guid Id,
    string FullName,
    string DocumentId,
    string? Phone,
    string? Email,
    string? PhotoUrl,
    MemberStatus Status,
    DateTime CreatedAtUtc)
{
    public static MemberDto From(Member m) => new(
        m.Id, m.FullName, m.DocumentId, m.Phone, m.Email, m.PhotoUrl, m.Status, m.CreatedAtUtc);
}
