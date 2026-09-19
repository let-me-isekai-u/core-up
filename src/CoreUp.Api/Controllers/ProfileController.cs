using CoreUp.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreUp.Api.Controllers;

[Authorize]
[Route("api/profile")]
public sealed class ProfileController(ProfileService profiles) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProfileResponse>> Get(CancellationToken ct) =>
        Ok(await profiles.GetAsync(CurrentUserId, ct));

    [HttpPost]
    public async Task<ActionResult<ProfileResponse>> Create(ProfileRequest request, CancellationToken ct)
    {
        var result = await profiles.CreateAsync(CurrentUserId, request, ct);
        return CreatedAtAction(nameof(Get), result);
    }

    [HttpPut]
    public async Task<ActionResult<ProfileResponse>> Update(ProfileRequest request, CancellationToken ct) =>
        Ok(await profiles.UpdateAsync(CurrentUserId, request, ct));
}
