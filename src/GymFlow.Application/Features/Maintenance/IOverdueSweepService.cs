namespace GymFlow.Application.Features.Maintenance;

/// <summary>
/// Corte de morosidad. Lo dispara un job de background (Hangfire), fuera de una request,
/// por lo que opera sobre todos los tenants. Devuelve cuántas membresías marcó morosas.
/// </summary>
public interface IOverdueSweepService
{
    Task<int> RunAsync(CancellationToken ct = default);
}
