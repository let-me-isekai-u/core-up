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
    public int Total { get; private set; }
    public List<Row> Rows { get; private set; } = [];
    public sealed record Row(Guid Id, string Username, string Email, DateTime CreatedAt, bool EmailVerified, ApplicationRole Role, bool Premium, DateTime? ExpiresAt);
    public async Task OnGetAsync(CancellationToken ct) {
        PageNumber = Math.Clamp(PageNumber, 1, 1000000);
        var query = db.Users.AsNoTracking();
        var search = (Search ?? "").Trim();
        if (search.Length > 320) search = search[..320];
        if (search.Length > 0) {
            Guid.TryParse(search, out var id); var lower = search.ToLowerInvariant();
            query = query.Where(x => x.NormalizedUsername.Contains(lower) || x.Email.Contains(lower) || x.Id == id);
        }
        Total = await query.CountAsync(ct);
        var now = DateTime.UtcNow;
        Rows = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((PageNumber-1)*25).Take(25)
            .Select(u => new Row(u.Id, u.Username, u.Email, u.CreatedAt, u.EmailVerified, u.Role,
                db.PremiumEntitlements.Any(e => e.UserId == u.Id && e.Status == PremiumStatus.Active && (e.ExpiresAt == null || e.ExpiresAt > now)),
                db.PremiumEntitlements.Where(e => e.UserId == u.Id).Select(e => e.ExpiresAt).FirstOrDefault())).ToListAsync(ct);
    }
}
