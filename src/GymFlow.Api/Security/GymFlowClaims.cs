namespace GymFlow.Api.Security;

/// <summary>Tipos de claim propios de GymFlow.</summary>
public static class GymFlowClaims
{
    public const string TenantId = "tenant_id";

    /// <summary>Distingue el tipo de actor del token. Hoy solo "staff" (panel de recepción/admin).</summary>
    public const string ActorType = "actor";

    public const string ActorStaff = "staff";
}
