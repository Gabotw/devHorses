using GymFlow.Domain.Enums;

namespace GymFlow.Api.Security;

/// <summary>
/// Políticas de autorización por rol (RBAC por tenant). El aislamiento entre tenants
/// lo garantiza el filtro global; estas políticas gradúan qué puede hacer cada rol
/// dentro de su propio gimnasio.
/// </summary>
public static class Policies
{
    /// <summary>Owner o Admin: gestión (planes, alta/baja, acciones sensibles).</summary>
    public const string Manager = "Manager";

    /// <summary>Cualquier staff autenticado (incluye Reception): operación diaria.</summary>
    public const string Staff = "Staff";

    /// <summary>Miembro autenticado desde la app (token con actor=member). Solo endpoints /me.</summary>
    public const string Member = "Member";

    /// <summary>Super-admin de la plataforma (token con actor=platform). Solo endpoints /api/platform.</summary>
    public const string Platform = "Platform";

    public static readonly string[] ManagerRoles =
        [nameof(StaffRole.Owner), nameof(StaffRole.Admin)];

    public static readonly string[] StaffRoles =
        [nameof(StaffRole.Owner), nameof(StaffRole.Admin), nameof(StaffRole.Reception)];
}
