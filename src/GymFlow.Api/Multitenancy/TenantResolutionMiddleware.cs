using System.Security.Claims;
using GymFlow.Api.Security;
using GymFlow.Application.Abstractions.Tenancy;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Api.Multitenancy;

/// <summary>
/// Resuelve el tenant de cada request y lo fija en ITenantProvider.
///
/// Prioridad de confianza:
/// 1. Request autenticada → el tenant sale del claim <c>tenant_id</c> del JWT (emitido
///    por el servidor, fuente de verdad). Si además viene subdominio o header y no
///    coinciden con el claim, se rechaza (defensa contra cross-tenant).
/// 2. Request anónima (p.ej. login) → se resuelve por header <c>X-Tenant-Id</c> o por
///    subdominio, validando SIEMPRE contra la tabla de tenants.
///
/// Debe registrarse DESPUÉS de UseAuthentication para poder leer los claims.
/// </summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    private const string TenantHeader = "X-Tenant-Id";
    private const string TenantSubdomainHeader = "X-Tenant-Subdomain";

    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider, AppDbContext db)
    {
        // Endpoints que no requieren tenant (health, swagger) pasan sin resolver.
        if (IsTenantExempt(context))
        {
            await next(context);
            return;
        }

        Guid? resolved;

        if (context.User.Identity?.IsAuthenticated == true)
        {
            resolved = ResolveFromClaims(context, out var claimTenantId);
            if (resolved is null)
            {
                await Reject(context, StatusCodes.Status401Unauthorized, "Token sin tenant válido.");
                return;
            }

            // Si el cliente también manda header/subdominio, deben coincidir con el token.
            var hinted = TryGetHeaderTenant(context) ?? await TryGetSubdomainTenantIdAsync(context, db);
            if (hinted is not null && hinted != claimTenantId)
            {
                await Reject(context, StatusCodes.Status403Forbidden, "El tenant indicado no coincide con el del token.");
                return;
            }
        }
        else
        {
            // Prioridad anónima: X-Tenant-Id (guid) > X-Tenant-Subdomain (slug) > host.
            if (TryGetHeaderTenant(context) is { } headerTenant)
                resolved = await ValidateTenantExistsAsync(headerTenant, db);
            else if (GetSubdomainHeader(context) is { } slug)
                resolved = await ResolveBySubdomainAsync(slug, db);
            else
                resolved = await TryGetSubdomainTenantIdAsync(context, db);
        }

        if (resolved is null)
        {
            await Reject(context, StatusCodes.Status400BadRequest,
                "No se pudo resolver el gimnasio (tenant). Usa subdominio o cabecera X-Tenant-Id.");
            return;
        }

        tenantProvider.SetTenant(resolved.Value);
        await next(context);
    }

    private static Guid? ResolveFromClaims(HttpContext context, out Guid claimTenantId)
    {
        claimTenantId = Guid.Empty;
        var raw = context.User.FindFirstValue(GymFlowClaims.TenantId);
        if (Guid.TryParse(raw, out var parsed) && parsed != Guid.Empty)
        {
            claimTenantId = parsed;
            return parsed;
        }
        return null;
    }

    private static Guid? TryGetHeaderTenant(HttpContext context)
    {
        var raw = context.Request.Headers[TenantHeader].ToString();
        return Guid.TryParse(raw, out var id) && id != Guid.Empty ? id : null;
    }

    private static async Task<Guid?> ValidateTenantExistsAsync(Guid tenantId, AppDbContext db)
    {
        var exists = await db.Tenants.AnyAsync(t => t.Id == tenantId);
        return exists ? tenantId : null;
    }

    private static string? GetSubdomainHeader(HttpContext context)
    {
        var raw = context.Request.Headers[TenantSubdomainHeader].ToString();
        return string.IsNullOrWhiteSpace(raw) ? null : raw.Trim().ToLowerInvariant();
    }

    private static async Task<Guid?> ResolveBySubdomainAsync(string subdomain, AppDbContext db)
    {
        var tenant = await db.Tenants
            .Where(t => t.Subdomain == subdomain)
            .Select(t => new { t.Id })
            .FirstOrDefaultAsync();
        return tenant?.Id;
    }

    private static async Task<Guid?> TryGetSubdomainTenantIdAsync(HttpContext context, AppDbContext db)
    {
        var host = context.Request.Host.Host; // sin puerto
        var subdomain = ExtractSubdomain(host);
        return subdomain is null ? null : await ResolveBySubdomainAsync(subdomain, db);
    }

    /// <summary>
    /// Extrae el subdominio de un host tipo "acme.gymflow.pe" → "acme".
    /// Ignora hosts locales/planos (localhost, IPs, dominio de dos labels).
    /// </summary>
    private static string? ExtractSubdomain(string host)
    {
        if (string.IsNullOrWhiteSpace(host) || host == "localhost")
            return null;

        var labels = host.Split('.');
        if (labels.Length < 3)
            return null;

        var candidate = labels[0].Trim().ToLowerInvariant();
        return candidate is "www" or "" ? null : candidate;
    }

    private static bool IsTenantExempt(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        return path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/hangfire", StringComparison.OrdinalIgnoreCase)
            // Billing SaaS (Fase 6): el super-admin opera cross-tenant. Sus endpoints no
            // resuelven tenant; la policy Platform (actor=platform) los protege.
            || path.StartsWith("/api/platform", StringComparison.OrdinalIgnoreCase)
            || path == "/";
    }

    private static async Task Reject(HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { error = message });
    }
}
