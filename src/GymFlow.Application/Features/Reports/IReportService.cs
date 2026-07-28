namespace GymFlow.Application.Features.Reports;

/// <summary>
/// Reportes agregados del tenant para el panel (Fase 5). Solo lectura sobre las tablas de
/// negocio; el aislamiento por tenant lo garantiza el filtro global de EF.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Foto del dashboard para el rango de fechas (locales, zona del tenant). Si no se
    /// indican, cubre los últimos 30 días terminando hoy.
    /// </summary>
    Task<DashboardDto> GetDashboardAsync(DateOnly? from, DateOnly? to, CancellationToken ct = default);
}
