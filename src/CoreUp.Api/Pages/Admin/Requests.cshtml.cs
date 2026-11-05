using CoreUp.Application;
using CoreUp.Domain;
using CoreUp.Infrastructure.Administration;
using CoreUp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
namespace CoreUp.Api.Pages.Admin;
public sealed class RequestsModel(CoreUpDbContext db,PremiumRequestService requests) : PageModel {
    public List<PremiumDemoRequest> Rows {get;private set;}=[];
    [BindProperty(SupportsGet=true)] public int PageNumber {get;set;}=1;
    [TempData] public string? Notice {get;set;}
    public async Task OnGetAsync(CancellationToken ct) { PageNumber=Math.Clamp(PageNumber,1,100000); Rows=await db.Set<PremiumDemoRequest>().AsNoTracking().OrderByDescending(x=>x.CreatedAt).Skip((PageNumber-1)*25).Take(25).ToListAsync(ct); }
    public async Task<IActionResult> OnPostAsync(Guid id,bool approve,int days,string? reason,bool confirm,CancellationToken ct) {
        try { await requests.DecideAsync(User,id,approve,days,reason,confirm,ct); Notice="Đã ghi quyết định và audit.";return RedirectToPage(); }
        catch(AppException ex){ModelState.AddModelError("",ex.Message);}
        catch(Exception ex) when(ex is DbUpdateException or Npgsql.PostgresException){ModelState.AddModelError("","Dữ liệu thay đổi. Tải lại trước khi thử lại.");}
        await OnGetAsync(ct);return Page();
    }
}
