using GymFlow.Application.Features.Maintenance;
using Microsoft.Extensions.Logging;

namespace GymFlow.Infrastructure.Jobs;

/// <summary>
/// Punto de entrada del job de morosidad para Hangfire. Se mantiene delgado: Hangfire
/// lo resuelve del contenedor por cada ejecución (scope propio, sin tenant resuelto) y
/// delega en <see cref="IOverdueSweepService"/> la lógica de negocio.
/// </summary>
public sealed class OverdueSweepJob(IOverdueSweepService service, ILogger<OverdueSweepJob> logger)
{
    public const string RecurringJobId = "overdue-sweep";

    public async Task RunAsync()
    {
        var marked = await service.RunAsync();
        logger.LogInformation("Corte de morosidad: {Count} membresía(s) marcadas como morosas.", marked);
    }
}
