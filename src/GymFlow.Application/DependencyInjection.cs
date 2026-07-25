using GymFlow.Application.Features.Auth;
using GymFlow.Application.Features.Members;
using GymFlow.Application.Features.Memberships;
using GymFlow.Application.Features.Plans;
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
        return services;
    }
}
