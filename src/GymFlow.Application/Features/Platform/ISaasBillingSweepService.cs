namespace GymFlow.Application.Features.Platform;

/// <summary>
/// Corte de billing SaaS: marca morosas (PastDue) las suscripciones cuyo período venció.
/// Corre sin tenant resuelto (job de background). Devuelve cuántas marcó.
/// </summary>
public interface ISaasBillingSweepService
{
    Task<int> RunAsync(CancellationToken ct = default);
}
