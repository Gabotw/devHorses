using GymFlow.Domain.Entities;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.Features.Platform;

// --- Auth del super-admin ---
public sealed record PlatformLoginRequest(string Email, string Password);

public sealed record PlatformLoginResult(
    string AccessToken,
    DateTime ExpiresAtUtc,
    Guid AdminId,
    string FullName);

// --- Planes de plataforma (catálogo SaaS) ---
public sealed record PlatformPlanDto(
    Guid Id,
    string Name,
    decimal MonthlyPrice,
    int BillingPeriodDays,
    int? MaxMembers,
    bool IsActive)
{
    public static PlatformPlanDto From(PlatformPlan p) =>
        new(p.Id, p.Name, p.MonthlyPrice, p.BillingPeriodDays, p.MaxMembers, p.IsActive);
}

public sealed record UpsertPlatformPlanRequest(
    string Name,
    decimal MonthlyPrice,
    int BillingPeriodDays,
    int? MaxMembers);

// --- Suscripción de un tenant ---
public sealed record SubscriptionDto(
    Guid Id,
    Guid TenantId,
    Guid PlatformPlanId,
    string PlanName,
    decimal PriceAtSubscription,
    int BillingPeriodDays,
    TenantSubscriptionStatus Status,
    DateOnly CurrentPeriodStart,
    DateOnly CurrentPeriodEnd,
    DateTime? CanceledAtUtc)
{
    public static SubscriptionDto From(Subscription s) =>
        new(s.Id, s.TenantId, s.PlatformPlanId, s.PlanName, s.PriceAtSubscription,
            s.BillingPeriodDays, s.Status, s.CurrentPeriodStart, s.CurrentPeriodEnd, s.CanceledAtUtc);
}

/// <summary>Un gimnasio con su suscripción vigente (o null si nunca se suscribió), para la consola.</summary>
public sealed record TenantBillingDto(
    Guid TenantId,
    string Name,
    string Subdomain,
    TenantSubscriptionStatus SubscriptionStatus,
    int MemberCount,
    SubscriptionDto? Subscription);

/// <summary>Asigna o cambia el plan de un tenant. <paramref name="StartDate"/> null = hoy (zona del tenant).</summary>
public sealed record AssignSubscriptionRequest(Guid PlatformPlanId, DateOnly? StartDate);
