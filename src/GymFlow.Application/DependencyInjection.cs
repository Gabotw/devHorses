using GymFlow.Application.Features.Auth;
using GymFlow.Application.Features.Classes;
using GymFlow.Application.Features.CheckIns;
using GymFlow.Application.Features.Maintenance;
using GymFlow.Application.Features.Me;
using GymFlow.Application.Features.MemberAuth;
using GymFlow.Application.Features.Members;
using GymFlow.Application.Features.Memberships;
using GymFlow.Application.Features.Payments;
using GymFlow.Application.Features.Platform;
using GymFlow.Application.Features.Plans;
using GymFlow.Application.Features.Reports;
using Microsoft.Extensions.DependencyInjection;

namespace GymFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<IMembershipPlanService, MembershipPlanService>();
        services.AddScoped<IMembershipService, MembershipService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IOverdueSweepService, OverdueSweepService>();
        services.AddScoped<ICheckInService, CheckInService>();
        services.AddScoped<IMemberAuthService, MemberAuthService>();
        services.AddScoped<IMeService, MeService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IClassService, ClassService>();

        // Billing del SaaS (Fase 6) — nivel plataforma.
        services.AddScoped<IPlatformAuthService, PlatformAuthService>();
        services.AddScoped<IPlatformPlanService, PlatformPlanService>();
        services.AddScoped<IPlatformBillingService, PlatformBillingService>();
        services.AddScoped<ISaasBillingSweepService, SaasBillingSweepService>();
        return services;
    }
}
