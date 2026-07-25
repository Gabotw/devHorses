using GymFlow.Application.Abstractions.Persistence;
using GymFlow.Application.Abstractions.Tenancy;
using GymFlow.Application.Common;
using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.Features.Members;

/// <summary>
/// CRUD de miembros. Todas las consultas van acotadas al tenant por el global query
/// filter; el TenantId de creación viene de ITenantProvider, nunca del cliente.
/// </summary>
public sealed class MemberService(IAppDbContext db, ITenantProvider tenant) : IMemberService
{
    private const int MaxPageSize = 100;

    public async Task<PagedResult<MemberDto>> ListAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > MaxPageSize ? 20 : pageSize;

        var query = db.Members.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(m =>
                m.FullName.ToLower().Contains(term) ||
                m.DocumentId.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(m => m.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => MemberDto.From(m))
            .ToListAsync(ct);

        return new PagedResult<MemberDto>(items, total, page, pageSize);
    }

    public async Task<MemberDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var member = await db.Members.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new NotFoundException("Miembro no encontrado.");
        return MemberDto.From(member);
    }

    public async Task<MemberDto> CreateAsync(CreateMemberRequest request, CancellationToken ct = default)
    {
        var doc = (request.DocumentId ?? string.Empty).Trim();
        await EnsureDocumentUniqueAsync(doc, excludeId: null, ct);

        var member = new Member(
            tenant.GetRequiredTenantId(),
            request.FullName,
            doc,
            request.Phone,
            request.Email,
            request.PhotoUrl);

        db.Members.Add(member);
        await db.SaveChangesAsync(ct);
        return MemberDto.From(member);
    }

    public async Task<MemberDto> UpdateAsync(Guid id, UpdateMemberRequest request, CancellationToken ct = default)
    {
        var member = await db.Members.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new NotFoundException("Miembro no encontrado.");

        var doc = (request.DocumentId ?? string.Empty).Trim();
        await EnsureDocumentUniqueAsync(doc, excludeId: id, ct);

        member.SetFullName(request.FullName);
        member.SetDocumentId(doc);
        member.SetContact(request.Phone, request.Email);
        member.SetPhoto(request.PhotoUrl);

        await db.SaveChangesAsync(ct);
        return MemberDto.From(member);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var member = await db.Members.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new NotFoundException("Miembro no encontrado.");
        member.Deactivate();
        await db.SaveChangesAsync(ct);
    }

    public async Task ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var member = await db.Members.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new NotFoundException("Miembro no encontrado.");
        member.Activate();
        await db.SaveChangesAsync(ct);
    }

    private async Task EnsureDocumentUniqueAsync(string documentId, Guid? excludeId, CancellationToken ct)
    {
        var exists = await db.Members
            .AnyAsync(m => m.DocumentId == documentId && (excludeId == null || m.Id != excludeId), ct);
        if (exists)
            throw new ConflictException($"Ya existe un miembro con el documento {documentId}.");
    }
}
