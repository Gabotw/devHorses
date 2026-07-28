using GymFlow.Application.Abstractions.Payments;
using GymFlow.Application.Abstractions.Persistence;
using GymFlow.Application.Abstractions.Security;
using GymFlow.Application.Abstractions.Time;
using GymFlow.Infrastructure.Jobs;
using GymFlow.Infrastructure.Payments;
using GymFlow.Infrastructure.Persistence;
using GymFlow.Infrastructure.Security;
using GymFlow.Infrastructure.Time;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GymFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = NpgsqlConnectionStringResolver.Resolve(
            configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException(
                    "Falta la cadena de conexión 'Default' (Postgres/Neon)."));

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<AppDbSeeder>();

        // --- Pasarela de pago (puerto IPaymentGateway → adaptador Culqi) ---
        services.Configure<CulqiSettings>(configuration.GetSection(CulqiSettings.SectionName));
        services.AddHttpClient<IPaymentGateway, CulqiPaymentGateway>();

        return services;
    }

    /// <summary>
    /// Registra Hangfire (almacenamiento en Postgres) y los jobs. Se llama aparte de
    /// <see cref="AddInfrastructure"/> porque levanta un servidor de background: solo
    /// tiene sentido en el host de la API, no en escenarios de solo-datos (tests/EF tooling).
    /// </summary>
    public static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = NpgsqlConnectionStringResolver.Resolve(
            configuration.GetConnectionString("Default")
                ?? throw new InvalidOperationException(
                    "Falta la cadena de conexión 'Default' (Postgres/Neon)."));

        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));

        services.AddHangfireServer();
        services.AddScoped<OverdueSweepJob>();
        services.AddScoped<SaasBillingSweepJob>();

        return services;
    }
}
