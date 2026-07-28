using GymFlow.Application.Features.Platform;
using Microsoft.Extensions.Logging;

namespace GymFlow.Infrastructure.Jobs;

/// <summary>
/// Punto de entrada del corte de billing SaaS para Hangfire (Fase 6). Delgado: Hangfire lo
/// resuelve por ejecución (scope propio, sin tenant) y delega en <see cref="ISaasBillingSweepService"/>.
/// </summary>
public sealed class SaasBillingSweepJob(ISaasBillingSweepService service, ILogger<SaasBillingSweepJob> logger)
{
    public const string RecurringJobId = "saas-billing-sweep";

    public async Task RunAsync()
    {
        var marked = await service.RunAsync();
        logger.LogInformation("Corte de billing SaaS: {Count} suscripción(es) marcadas como morosas.", marked);
    }
}
