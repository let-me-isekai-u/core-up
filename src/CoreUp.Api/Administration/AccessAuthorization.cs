using System.Security.Claims;
using CoreUp.Domain;
using CoreUp.Infrastructure.Administration;
using CoreUp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace CoreUp.Api.Administration;

public sealed record AdminRequirement : IAuthorizationRequirement;
public sealed record PremiumRequirement : IAuthorizationRequirement;

public sealed class AdminAuthorizationHandler(CoreUpDbContext db, LocalPanelAdministrator panel) : AuthorizationHandler<AdminRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        if (panel.IsAuthenticated(context.User)) { context.Succeed(requirement); return; }
        if (Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) &&
            await db.Users.AsNoTracking().AnyAsync(x => x.Id == id && x.Role == ApplicationRole.Admin && x.EmailVerified))
            context.Succeed(requirement);
    }
}

public sealed class PremiumAuthorizationHandler(AccountAccessService access) : AuthorizationHandler<PremiumRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PremiumRequirement requirement)
    {
        if (Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) &&
            (await access.CurrentAsync(id, CancellationToken.None)).HasPremium)
            context.Succeed(requirement);
    }
}
