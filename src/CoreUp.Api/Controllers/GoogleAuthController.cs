using CoreUp.Application;
using CoreUp.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace CoreUp.Api.Controllers;
[Route("api/auth/google")]
[EnableRateLimiting("auth")]
public sealed class GoogleAuthController(GoogleIdentityService google) : ApiControllerBase
{
    [AllowAnonymous, HttpPost]
    public async Task<ActionResult<AuthResponse>> Login(GoogleTokenRequest request, CancellationToken ct) =>
        Ok(await google.SignInAsync(request.IdToken, ct));
    [Authorize, HttpPost("link")]
    public async Task<ActionResult> Link(GoogleLinkRequest request, CancellationToken ct) {
        await google.LinkAsync(CurrentUserId,request,ct);
        return Ok(new { message = "Đã liên kết Google. Đăng nhập lại để tiếp tục." });
    }
}
