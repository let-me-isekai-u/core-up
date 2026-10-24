using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CoreUp.Infrastructure.Administration;

/// A browser-only localhost operator, separate from mobile application accounts.
public sealed class LocalPanelAdministrator(IConfiguration configuration, IHostEnvironment environment)
{
    private readonly IConfiguration settings = LoadLocalSettings(configuration, environment);

    private static IConfiguration LoadLocalSettings(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment()) return configuration;
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".config", "core-up", "backend-runtime.json");
        return new ConfigurationBuilder().AddJsonFile(path, optional: true, reloadOnChange: false)
            .AddConfiguration(configuration).Build();
    }

    private readonly string sessionStamp = Guid.NewGuid().ToString("N");
    private const string SessionClaim = "coreup:local_panel_session";
    public bool Enabled => environment.IsDevelopment() &&
        settings.GetValue<bool>("LocalAdmin:Enabled") &&
        !string.IsNullOrEmpty(settings["LocalAdmin:Username"]) &&
        !string.IsNullOrEmpty(settings["LocalAdmin:Password"]);

    public bool Validate(string username, string password) => Enabled &&
        Matches(username, settings["LocalAdmin:Username"]!) &&
        Matches(password, settings["LocalAdmin:Password"]!);

    private static bool Matches(string actual, string expected) =>
        CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(actual)),
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)));

    public ClaimsPrincipal CreatePrincipal() => new(new ClaimsIdentity(new[] {
        new Claim(ClaimTypes.NameIdentifier, "local-panel-administrator"),
        new Claim(ClaimTypes.Name, settings["LocalAdmin:Username"]!),
        new Claim(SessionClaim, sessionStamp),
    }, "AdminCookie"));

    public bool IsAuthenticated(ClaimsPrincipal principal) => Enabled &&
        principal.Identities.Any(identity => identity.IsAuthenticated &&
            identity.AuthenticationType == "AdminCookie" &&
            identity.FindFirst(SessionClaim)?.Value == sessionStamp);
}
