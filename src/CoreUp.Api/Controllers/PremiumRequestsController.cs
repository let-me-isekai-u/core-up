using CoreUp.Domain;
using CoreUp.Infrastructure.Administration;
using CoreUp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CoreUp.Api.Controllers;
[Authorize]
[Route("api/account/premium-requests")]
public sealed class PremiumRequestsController(CoreUpDbContext db, PremiumRequestService requests) : ApiControllerBase
{
    [HttpGet] public async Task<IActionResult> Get(CancellationToken ct) => Ok(await db.Set<PremiumDemoRequest>().AsNoTracking().Where(x=>x.UserId==CurrentUserId).OrderByDescending(x=>x.CreatedAt).Take(25).ToListAsync(ct));
    [HttpPost] public async Task<IActionResult> Submit(CancellationToken ct) { await requests.SubmitAsync(CurrentUserId,ct); return Accepted(); }
    [HttpDelete("pending")] public async Task<IActionResult> Cancel(CancellationToken ct) { await requests.CancelAsync(CurrentUserId,ct); return NoContent(); }
}
