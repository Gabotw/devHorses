namespace GymFlow.Api.Security;

/// <summary>Tipos de claim propios de GymFlow.</summary>
public static class GymFlowClaims
{
    public const string TenantId = "tenant_id";

    /// <summary>Distingue el tipo de actor del token: "staff" (panel), "member" (app) o "platform" (super-admin SaaS).</summary>
    public const string ActorType = "actor";

    public const string ActorStaff = "staff";
    public const string ActorMember = "member";
    public const string ActorPlatform = "platform";
}
