namespace GymFlow.Domain.Enums;

/// <summary>
/// Roles de staff dentro de un tenant (RBAC por tenant).
/// El owner nunca puede ver otros tenants: el aislamiento lo garantiza el
/// filtro global por TenantId, no el rol.
/// </summary>
public enum StaffRole
{
    /// <summary>Dueño del gimnasio. Acceso total dentro de su tenant.</summary>
    Owner = 1,

    /// <summary>Administrador. Gestiona miembros, planes, pagos, reportes.</summary>
    Admin = 2,

    /// <summary>Recepción. Check-in y consulta; sin acceso a facturación ni reportes sensibles.</summary>
    Reception = 3,
}
