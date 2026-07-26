namespace GymFlow.Api.Realtime;

/// <summary>Nombres de grupo SignalR por tenant, para aislar la difusión de aforo.</summary>
public static class OccupancyGroups
{
    public static string ForTenant(Guid tenantId) => $"tenant:{tenantId}";

    public static string ForTenant(string tenantId) => $"tenant:{tenantId}";
}
