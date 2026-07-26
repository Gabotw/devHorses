using GymFlow.Application.Abstractions.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GymFlow.Infrastructure.Persistence;

/// <summary>
/// Fábrica de diseño para las herramientas de EF (migraciones). Evita arrancar todo el
/// host de la API (Hangfire, seeder, conexión real) solo para scaffoldear una migración:
/// construye el <see cref="AppDbContext"/> directamente con un tenant nulo. La cadena de
/// conexión puede venir por variable de entorno; para generar migraciones basta el proveedor.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Database=gymflow_design;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        return new AppDbContext(options, new NullTenantProvider());
    }

    /// <summary>Sin tenant en tiempo de diseño: las migraciones no dependen del filtro global.</summary>
    private sealed class NullTenantProvider : ITenantProvider
    {
        public Guid? TenantId => null;
        public bool HasTenant => false;
        public Guid GetRequiredTenantId() =>
            throw new InvalidOperationException("Sin tenant en tiempo de diseño.");
        public void SetTenant(Guid tenantId) { }
    }
}
