using System.Security.Claims;
using CoreUp.Domain;
using CoreUp.Infrastructure.Administration;
using CoreUp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CoreUp.Api.Administration;

public static class AdminSetup
{
    public const string CookieScheme = "AdminCookie";
    public static void AddAdmin(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<LocalPanelAdministrator>();
        builder.Services.AddHttpClient("LocalMailpit", client => {
            client.BaseAddress = new Uri("http://localhost:8025/");
            client.Timeout = TimeSpan.FromSeconds(5);
        });
        builder.Services.AddScoped<AccountAccessService>();
        builder.Services.AddScoped<IAuthorizationHandler, AdminAuthorizationHandler>();
        builder.Services.AddScoped<IAuthorizationHandler, PremiumAuthorizationHandler>();
        builder.Services.AddAuthentication().AddCookie(CookieScheme, options => {
            options.Cookie.Name = "__Host-CoreUpAdmin";
            options.Cookie.Path = "/";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.LoginPath = "/admin/login";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = false;
            options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
            options.Events.OnValidatePrincipal = async context => {
                var panel = context.HttpContext.RequestServices.GetRequiredService<LocalPanelAdministrator>();
                if (context.Principal is not null && panel.IsAuthenticated(context.Principal)) return;
                var idText = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var version = context.Principal?.FindFirstValue("token_version");
                var db = context.HttpContext.RequestServices.GetRequiredService<CoreUpDbContext>();
                var valid = Guid.TryParse(idText, out var id) && int.TryParse(version, out var tokenVersion) &&
                    await db.Users.AsNoTracking().AnyAsync(x => x.Id == id && x.EmailVerified &&
                        x.Role == ApplicationRole.Admin && x.TokenVersion == tokenVersion, context.HttpContext.RequestAborted);
                if (!valid) { context.RejectPrincipal(); await context.HttpContext.SignOutAsync(CookieScheme); }
            };
        });
        builder.Services.AddAuthorization(options => {
            options.AddPolicy("Admin", policy => policy.AddAuthenticationSchemes(CookieScheme)
                .RequireAuthenticatedUser().AddRequirements(new AdminRequirement()));
            options.AddPolicy("Premium", policy => policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser().AddRequirements(new PremiumRequirement()));
        });
        builder.Services.AddAntiforgery(options => {
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.HttpOnly = true; options.Cookie.SameSite = SameSiteMode.Strict;
        });
        builder.Services.AddRazorPages(options => {
            options.Conventions.AuthorizeFolder("/Admin", "Admin");
            options.Conventions.AllowAnonymousToPage("/Admin/Login");
        });
    }
}
