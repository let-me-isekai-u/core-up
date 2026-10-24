using CoreUp.Domain;
using CoreUp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace CoreUp.Api.Pages.Admin;
public sealed class AuditModel(CoreUpDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public int PageNumber { get; set; } = 1;
    public List<AdminAuditEvent> Events { get; private set; } = [];
    public bool More { get; private set; }
    public async Task OnGetAsync(CancellationToken ct) {
        PageNumber = Math.Clamp(PageNumber,1,1000000);
        Events = await db.AdminAuditEvents.AsNoTracking().OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).Skip((PageNumber-1)*25).Take(26).ToListAsync(ct);
        More = Events.Count > 25; if (More) Events.RemoveAt(25);
    }
}
