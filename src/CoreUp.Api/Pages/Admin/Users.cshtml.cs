using CoreUp.Domain;
using CoreUp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace CoreUp.Api.Pages.Admin;
public sealed class UsersModel(CoreUpDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public bool PremiumOnly { get; set; }
    public int Total { get; private set; }
    public List<Row> Rows { get; private set; } = [];
    public sealed record Row(Guid Id, string Username, string Email, DateTime CreatedAt, bool EmailVerified, ApplicationRole Role, bool HasPassword, bool Premium, DateTime? ExpiresAt, string AccountState, PremiumStatus? PremiumState);
    public async Task OnGetAsync(CancellationToken ct) {
        PageNumber = Math.Clamp(PageNumber, 1, 1000000);
        var query = db.Users.AsNoTracking();
        var search = (Search ?? "").Trim();
        if (search.Length > 320) search = search[..320];
        if (search.Length > 0) {
            Guid.TryParse(search, out var id); var lower = search.ToLowerInvariant();
            query = query.Where(x => x.NormalizedUsername.Contains(lower) || x.Email.Contains(lower) || x.Id == id);
        }
        var now = DateTime.UtcNow;
        if (PremiumOnly) query = query.Where(u => db.PremiumEntitlements.Any(e => e.UserId == u.Id && e.Status == PremiumStatus.Active && (e.ExpiresAt == null || e.ExpiresAt > now)));
        Total = await query.CountAsync(ct);
        PageNumber = Math.Min(PageNumber, Math.Max(1, (Total + 24) / 25));
        Rows = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((PageNumber-1)*25).Take(25)
            .Select(u => new Row(u.Id, u.Username, u.Email, u.CreatedAt, u.EmailVerified, u.Role, u.PasswordHash != null,
                db.PremiumEntitlements.Any(e => e.UserId == u.Id && e.Status == PremiumStatus.Active && (e.ExpiresAt == null || e.ExpiresAt > now)),
                db.PremiumEntitlements.Where(e => e.UserId == u.Id).Select(e => e.ExpiresAt).FirstOrDefault(), u.AccountState,
                db.PremiumEntitlements.Where(e => e.UserId == u.Id).Select(e => (PremiumStatus?)e.Status).FirstOrDefault())).ToListAsync(ct);
    }
}
