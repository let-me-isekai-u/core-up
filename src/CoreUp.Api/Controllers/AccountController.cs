using CoreUp.Infrastructure.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreUp.Api.Controllers;

[Authorize]
[Route("api/account")]
public sealed class AccountController(AccountAccessService access) : ApiControllerBase
{
    [HttpGet("entitlement")]
    public async Task<ActionResult<EntitlementResponse>> Entitlement(CancellationToken ct) =>
        Ok(await access.CurrentAsync(CurrentUserId, ct));
}
