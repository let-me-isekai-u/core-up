namespace CoreUp.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Key { get; init; } = string.Empty;
    public string Issuer { get; init; } = "CoreUp.Api";
    public string Audience { get; init; } = "CoreUp.Mobile";
    public int ExpirationMinutes { get; init; } = 120;
}
