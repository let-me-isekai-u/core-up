using CoreUp.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CoreUp.Api.Controllers;

[EnableRateLimiting("auth")]
[Route("api/auth")]
public sealed class AuthController(AuthService auth, AccountSecurityService security, IApplicationDbContext db) : ApiControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct) =>
        Ok(await auth.RegisterAsync(request, ct));

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await auth.LoginAsync(request, ct));

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult> Me(CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().Include(x => x.Profile).SingleAsync(x => x.Id == CurrentUserId, ct);
        return Ok(new { user.Id, user.Username, user.Email, user.EmailVerified, user.VerificationRequired, NeedsEmailVerification = !user.EmailVerified, NeedsProfile = user.Profile is null });
    }

    [Authorize]
    [HttpGet("email-verification/status")]
    public async Task<ActionResult> VerificationStatus(CancellationToken ct)
    {
        var challenge = await db.EmailChallenges.AsNoTracking().SingleOrDefaultAsync(
            x => x.UserId == CurrentUserId && x.Purpose == AccountSecurityService.Verification, ct);
        return Ok(new { ExpiresAt = challenge?.ExpiresAt, ResendAt = challenge?.SentAt.AddSeconds(60) });
    }

    [Authorize]
    [HttpPost("email-verification/send")]
    public async Task<ActionResult> SendVerification(CancellationToken ct) => Ok(await security.SendVerificationAsync(CurrentUserId, ct));

    [Authorize]
    [HttpPost("email-verification/verify")]
    public async Task<ActionResult> VerifyEmail(VerifyEmailRequest request, CancellationToken ct)
    {
        await security.VerifyEmailAsync(CurrentUserId, request.Code, ct);
        return Ok(new { message = "Email đã được xác minh." });
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<ActionResult> Forgot(EmailRequest request, CancellationToken ct) => Accepted(await security.ForgotAsync(request.Email, ct));

    [AllowAnonymous]
    [HttpPost("reset-password/verify")]
    public async Task<ActionResult> VerifyReset(VerifyResetRequest request, CancellationToken ct) => Ok(await security.VerifyResetAsync(request.Email, request.Code, ct));

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<ActionResult> Reset(ResetPasswordRequest request, CancellationToken ct)
    {
        await security.ResetPasswordAsync(request, ct);
        return Ok(new { message = "Đã đặt lại mật khẩu. Hãy đăng nhập lại." });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult> Change(ChangePasswordRequest request, CancellationToken ct)
    {
        await security.ChangePasswordAsync(CurrentUserId, request, ct);
        return Ok(new { message = "Đã đổi mật khẩu và thu hồi mọi phiên. Hãy đăng nhập lại." });
    }
}
