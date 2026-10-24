using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using CoreUp.Api.Administration;
using CoreUp.Domain;
using CoreUp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CoreUp.Api.Pages.Admin;

[EnableRateLimiting("admin-login")]
public sealed class LoginModel(CoreUpDbContext db, IPasswordHasher<User> passwords) : PageModel
{
    [BindProperty, Required, MaxLength(24)] public string Username { get; set; } = "";
    [BindProperty, Required, MaxLength(128)] public string Password { get; set; } = "";
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) return Page();
        var name = Username.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.NormalizedUsername == name, ct);
        if (user == null || user.Role != ApplicationRole.Admin || !user.EmailVerified || string.IsNullOrEmpty(user.PasswordHash) ||
            passwords.VerifyHashedPassword(user, user.PasswordHash, Password) == PasswordVerificationResult.Failed) {
            await Task.Delay(300, ct);
            ModelState.AddModelError("", "Không thể đăng nhập quản trị bằng thông tin này.");
            Password = ""; ModelState.Remove(nameof(Password)); return Page();
        }
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("token_version", user.TokenVersion.ToString()) }, AdminSetup.CookieScheme);
        await HttpContext.SignInAsync(AdminSetup.CookieScheme, new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false, AllowRefresh = false });
        return RedirectToPage("/Admin/Index");
    }
}
