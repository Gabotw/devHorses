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
public sealed class MemberService(
    IAppDbContext db, ITenantProvider tenant) : IMemberService
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

        member.SetAccessCode(await GenerateUniqueAccessCodeAsync(ct));

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

    public async Task<MemberDto> RegenerateAccessCodeAsync(Guid id, CancellationToken ct = default)
    {
        var member = await db.Members.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new NotFoundException("Miembro no encontrado.");

        member.SetAccessCode(await GenerateUniqueAccessCodeAsync(ct));
        await db.SaveChangesAsync(ct);
        return MemberDto.From(member);
    }

    private async Task EnsureDocumentUniqueAsync(string documentId, Guid? excludeId, CancellationToken ct)
    {
        var exists = await db.Members
            .AnyAsync(m => m.DocumentId == documentId && (excludeId == null || m.Id != excludeId), ct);
        if (exists)
            throw new ConflictException($"Ya existe un miembro con el documento {documentId}.");
    }

    /// <summary>Busca un código de 4 dígitos libre dentro del tenant. Intenta al azar unas veces
    /// (rápido cuando hay pocos miembros) y, si no encuentra, recorre el rango en orden.</summary>
    private async Task<string> GenerateUniqueAccessCodeAsync(CancellationToken ct)
    {
        var used = await db.Members
            .Where(m => m.AccessCode != null)
            .Select(m => m.AccessCode!)
            .ToListAsync(ct);
        var taken = new HashSet<string>(used);

        if (taken.Count >= 10_000)
            throw new ConflictException("No hay códigos de acceso de 4 dígitos disponibles en este gimnasio.");

        for (var i = 0; i < 20; i++)
        {
            var candidate = Random.Shared.Next(0, 10_000).ToString("D4");
            if (!taken.Contains(candidate))
                return candidate;
        }

        for (var n = 0; n < 10_000; n++)
        {
            var candidate = n.ToString("D4");
            if (!taken.Contains(candidate))
                return candidate;
        }

        throw new ConflictException("No hay códigos de acceso de 4 dígitos disponibles en este gimnasio.");
    }
}
