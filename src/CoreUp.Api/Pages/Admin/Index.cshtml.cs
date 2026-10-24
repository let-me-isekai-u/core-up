using CoreUp.Domain;
using CoreUp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace CoreUp.Api.Pages.Admin;
public sealed class IndexModel(CoreUpDbContext db) : PageModel
{
    public int Total { get; private set; }
    public int Premium { get; private set; }
    public int Expired { get; private set; }
    public List<RecentUser> Recent { get; private set; } = [];
    public List<AdminAuditEvent> Events { get; private set; } = [];
    public sealed record RecentUser(Guid Id, string Username, DateTime CreatedAt);
    public async Task OnGetAsync(CancellationToken ct) {
        var now = DateTime.UtcNow;
        Total = await db.Users.CountAsync(ct);
        Premium = await db.PremiumEntitlements.CountAsync(x => x.Status == PremiumStatus.Active && (x.ExpiresAt == null || x.ExpiresAt > now), ct);
        Expired = await db.PremiumEntitlements.CountAsync(x => x.Status == PremiumStatus.Active && x.ExpiresAt <= now, ct);
        Recent = await db.Users.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(10).Select(x => new RecentUser(x.Id, x.Username, x.CreatedAt)).ToListAsync(ct);
        Events = await db.AdminAuditEvents.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(10).ToListAsync(ct);
    }
}
