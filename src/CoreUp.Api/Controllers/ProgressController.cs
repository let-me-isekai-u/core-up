using CoreUp.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreUp.Api.Controllers;

[Authorize]
[Route("api/progress")]
public sealed class ProgressController(WeightProgressService service) : ApiControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<ProgressSummaryResponse>> Summary(CancellationToken ct) =>
        Ok(await service.GetProgressAsync(CurrentUserId, ct));
}
