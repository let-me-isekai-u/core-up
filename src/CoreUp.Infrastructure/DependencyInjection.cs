using CoreUp.Application;
using CoreUp.Infrastructure.Authentication;
using CoreUp.Infrastructure.Email;
using CoreUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CoreUp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            var password = configuration["POSTGRES_PASSWORD"];
            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("POSTGRES_PASSWORD is missing. Configure it in .env or environment variables.");
            connectionString = new NpgsqlConnectionStringBuilder
            {
                Host = configuration["POSTGRES_HOST"] ?? "localhost",
                Port = int.TryParse(configuration["POSTGRES_PORT"], out var port) ? port : 5432,
                Database = configuration["POSTGRES_DB"] ?? "coreup_db",
                Username = configuration["POSTGRES_USER"] ?? "coreup_user",
                Password = password
            }.ConnectionString;
        }

        services.AddDbContext<CoreUpDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<CoreUpDbContext>());
        services.Configure<SmtpOptions>(configuration.GetSection("Smtp"));
        services.AddSingleton<SmtpEmailSender>();
        services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<SmtpEmailSender>());
        services.AddHostedService(sp => sp.GetRequiredService<SmtpEmailSender>());
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        return services;
    }
}
