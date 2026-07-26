using System.Text;
using GymFlow.Api.Infrastructure;
using GymFlow.Api.Multitenancy;
using GymFlow.Api.Realtime;
using GymFlow.Api.Security;
using GymFlow.Application;
using GymFlow.Application.Abstractions.Realtime;
using GymFlow.Application.Abstractions.Security;
using GymFlow.Application.Abstractions.Tenancy;
using GymFlow.Infrastructure;
using GymFlow.Infrastructure.Jobs;
using GymFlow.Infrastructure.Persistence;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Render (y otros PaaS) inyectan el puerto por la variable PORT. Kestrel escucha ahí.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// --- Capas ---
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddBackgroundJobs(builder.Configuration);

// --- Multi-tenancy: provider scoped por request ---
builder.Services.AddScoped<ITenantProvider, TenantProvider>();

// --- Auth / JWT ---
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
if (string.IsNullOrWhiteSpace(jwtSettings.SigningKey))
    throw new InvalidOperationException("Falta Jwt:SigningKey en la configuración.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        // SignalR (WebSocket) no puede mandar el header Authorization: el token llega por
        // query string en el handshake del hub. Solo se acepta para rutas /hubs.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.Manager, p => p.RequireRole(Policies.ManagerRoles));
    options.AddPolicy(Policies.Staff, p => p.RequireRole(Policies.StaffRoles));
    options.AddPolicy(Policies.Member, p => p.RequireClaim(GymFlowClaims.ActorType, GymFlowClaims.ActorMember));
});

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
});
builder.Services.AddOpenApi();

// --- CORS para las apps cliente (web/móvil) ---
// Con Cors:AllowedOrigins definido, se restringe a esos orígenes (y se permiten credenciales
// para SignalR). Sin él, se permite cualquier origen (JWT por header/query, sin cookies).
const string CorsPolicy = "gymflow-clients";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod();
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowCredentials();
        else
            policy.AllowAnyOrigin();
    });
});

// --- Tiempo real: aforo por SignalR (adaptador del puerto IOccupancyNotifier) ---
var signalR = builder.Services.AddSignalR();

// Backplane Redis opcional: necesario solo con múltiples instancias de la Api. Si no hay
// cadena de conexión, SignalR usa memoria (suficiente para una sola instancia en validación).
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnection))
    signalR.AddStackExchangeRedis(redisConnection);

builder.Services.AddScoped<IOccupancyNotifier, SignalROccupancyNotifier>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Detrás del proxy de Render la TLS termina fuera del contenedor; redirigir a HTTPS
    // dentro solo causaría bucles. Solo se fuerza HTTPS en local.
    app.UseHttpsRedirection();
}

app.UseCors(CorsPolicy);

app.UseAuthentication();
// El tenant se resuelve DESPUÉS de autenticar: request autenticada usa el claim,
// anónima usa subdominio/header. Antes de autorizar.
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHub<OccupancyHub>("/hubs/occupancy");
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Dashboard de Hangfire solo en Development (por defecto solo acepta peticiones locales).
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire");
}

// Corte de morosidad diario (03:00). Marca morosas las membresías vencidas sin renovar.
app.Services.GetRequiredService<IRecurringJobManager>().AddOrUpdate<OverdueSweepJob>(
    OverdueSweepJob.RecurringJobId,
    job => job.RunAsync(),
    Cron.Daily(3));

// Migración al arrancar (en todos los entornos) y seed opcional. En Development siempre
// siembra; en producción solo si Seed:Enabled=true (útil para poblar el tenant demo una vez).
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    await sp.GetRequiredService<AppDbContext>().Database.MigrateAsync();

    var seedEnabled = app.Environment.IsDevelopment()
        || app.Configuration.GetValue<bool>("Seed:Enabled");
    if (seedEnabled)
        await sp.GetRequiredService<AppDbSeeder>().SeedAsync();
}

app.Run();

/// <summary>Punto de entrada expuesto para pruebas de integración (WebApplicationFactory).</summary>
public partial class Program;
