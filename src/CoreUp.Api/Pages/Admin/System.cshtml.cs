using Microsoft.AspNetCore.Mvc.RazorPages;
namespace CoreUp.Api.Pages.Admin;
public sealed class SystemModel(IConfiguration configuration, IWebHostEnvironment environment) : PageModel
{
    public string EnvironmentName => environment.EnvironmentName;
    public bool GoogleConfigured => (configuration.GetSection("Google:ClientIds").Get<string[]>() ?? []).Any(x => !string.IsNullOrWhiteSpace(x));
}
