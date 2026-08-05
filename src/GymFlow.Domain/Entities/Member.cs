using GymFlow.Domain.Common;
using GymFlow.Domain.Enums;

namespace GymFlow.Domain.Entities;

/// <summary>
/// Persona inscrita en el gimnasio. Su documento (DNI) es único por tenant.
/// El estado activo/inactivo es de ciclo de vida; el detalle de su membresía
/// vigente (activa/congelada/vencida) vive en <see cref="Membership"/>.
/// </summary>
public class Member : Entity, ITenantScoped
{
    // EF Core
    private Member() { }

    public Member(
        Guid tenantId,
        string fullName,
        string documentId,
        string? phone = null,
        string? email = null,
        string? photoUrl = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId es obligatorio.", nameof(tenantId));

        TenantId = tenantId;
        SetFullName(fullName);
        SetDocumentId(documentId);
        SetContact(phone, email);
        PhotoUrl = string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl.Trim();
        Status = MemberStatus.Active;
    }

    public Guid TenantId { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    /// <summary>Documento de identidad (DNI/CE). Único por tenant.</summary>
    public string DocumentId { get; private set; } = string.Empty;

    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    public string? PhotoUrl { get; private set; }

    public MemberStatus Status { get; private set; }

    /// <summary>Código de acceso de 4 dígitos que el miembro teclea en recepción para el check-in.
    /// Único por tenant. Null hasta que se le asigne uno.</summary>
    public string? AccessCode { get; private set; }

    public void SetFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("El nombre del miembro es obligatorio.");
        FullName = fullName.Trim();
        Touch();
    }

    public void SetDocumentId(string documentId)
    {
        if (string.IsNullOrWhiteSpace(documentId))
            throw new DomainException("El documento del miembro es obligatorio.");
        DocumentId = documentId.Trim();
        Touch();
    }

    public void SetContact(string? phone, string? email)
    {
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        Touch();
    }

    public void SetPhoto(string? photoUrl)
    {
        PhotoUrl = string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl.Trim();
        Touch();
    }

    public void Activate()
    {
        Status = MemberStatus.Active;
        Touch();
    }

    public void Deactivate()
    {
        Status = MemberStatus.Inactive;
        Touch();
    }

    /// <summary>Asigna el código de acceso de 4 dígitos. La unicidad por tenant la garantiza
    /// quien lo genera (índice único parcial en DB); aquí solo se valida el formato.</summary>
    public void SetAccessCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 4 || !code.All(char.IsDigit))
            throw new DomainException("El código de acceso debe tener exactamente 4 dígitos.");
        AccessCode = code;
        Touch();
    }
}
