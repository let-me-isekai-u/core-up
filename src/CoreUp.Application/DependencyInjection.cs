using CoreUp.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CoreUp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AccountSecurityService>();
        services.AddScoped<AuthService>();
        services.AddScoped<ProfileService>();
        services.AddScoped<ExerciseService>();
        services.AddScoped<WorkoutPlanService>();
        services.AddScoped<WorkoutSessionService>();
        services.AddScoped<WeightProgressService>();
        return services;
    }
}
