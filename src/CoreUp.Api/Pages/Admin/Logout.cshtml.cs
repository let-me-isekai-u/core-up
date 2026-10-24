using CoreUp.Api.Administration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace CoreUp.Api.Pages.Admin;
public sealed class LogoutModel : PageModel
{
    public async Task<IActionResult> OnPostAsync() {
        await HttpContext.SignOutAsync(AdminSetup.CookieScheme);
        return RedirectToPage("/Admin/Login");
    }
}
