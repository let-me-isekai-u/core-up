using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CoreUp.Application;
using CoreUp.Domain;
using CoreUp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CoreUp.Tests;

file sealed class SecurityClock : TimeProvider
{
    private DateTimeOffset now = DateTimeOffset.UtcNow;
    public override DateTimeOffset GetUtcNow() => now;
    public void Advance(int seconds) => now = now.AddSeconds(seconds);
}

[Collection("PostgreSQL")]
public sealed class AccountSecurityTests
{
    private static async Task<JsonElement> Json(HttpResponseMessage r)
    { Assert.True(r.IsSuccessStatusCode, $"HTTP {(int)r.StatusCode}"); return await r.Content.ReadFromJsonAsync<JsonElement>(); }
    private static async Task<(string Name, string Email, Guid Id)> Register(WebApplicationFactory<Program> f, HttpClient client)
    {
        using (var scope = f.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<CoreUpDbContext>().Database.MigrateAsync();
        var name = "sec_" + Guid.NewGuid().ToString("N")[..12];
        var email = name + "@example.invalid";
        var auth = await Json(await client.PostAsJsonAsync("/api/auth/register", new { username = name, email, password = "Password-123!" }));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.GetProperty("accessToken").GetString());
        Assert.True(auth.GetProperty("needsEmailVerification").GetBoolean());
        return (name, email, auth.GetProperty("userId").GetGuid());
    }
    private static async Task Verify(WebApplicationFactory<Program> f, HttpClient c, string email) =>
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync("/api/auth/email-verification/verify", new { code = f.Services.GetRequiredService<TestEmailSender>().Code(email) })).StatusCode);

    [PostgresFact]
    public async Task VerificationAttemptsExpiryResendReplayAndLegacyAccess()
    {
        var clock = new SecurityClock();
        await using var factory = PostgresCoreCatalogTests.Factory().WithWebHostBuilder(b => b.ConfigureServices(s =>
        { s.RemoveAll<TimeProvider>(); s.AddSingleton<TimeProvider>(clock); }));
        using var c = factory.CreateClient();
        var user = await Register(factory, c);
        var mail = factory.Services.GetRequiredService<TestEmailSender>();
        var first = mail.Code(user.Email);
        Assert.Matches("^[0-9]{6}$", first);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/exercises")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/auth/me")).StatusCode);
        var status = await Json(await c.GetAsync("/api/auth/email-verification/status"));
        Assert.True(status.TryGetProperty("expiresAt", out _));
        Assert.False(status.TryGetProperty("codeHash", out _));
        using (var scope = factory.Services.CreateScope())
        {
            var challenge = await scope.ServiceProvider.GetRequiredService<CoreUpDbContext>().EmailChallenges.SingleAsync(x => x.UserId == user.Id);
            Assert.NotEqual(first, challenge.CodeHash);
            Assert.Equal(64, challenge.CodeHash.Length);
            Assert.Equal(300, (challenge.ExpiresAt - challenge.SentAt).TotalSeconds);
        }
        var wrong = first == "000000" ? "111111" : "000000";
        for (var i = 0; i < 5; i++)
            Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/auth/email-verification/verify", new { code = wrong })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/auth/email-verification/verify", new { code = first })).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await c.PostAsync("/api/auth/email-verification/send", null)).StatusCode);
        clock.Advance(61);
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsync("/api/auth/email-verification/send", null)).StatusCode);
        // Expiry is server-side, independent of the Flutter countdown.
        clock.Advance(301);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/auth/email-verification/verify", new { code = mail.Code(user.Email) })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsync("/api/auth/email-verification/send", null)).StatusCode);
        await Verify(factory, c, user.Email);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/auth/email-verification/verify", new { code = mail.Code(user.Email) })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/exercises")).StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreUpDbContext>();
            Assert.True((await db.Users.SingleAsync(x => x.Id == user.Id)).EmailVerified);
            var legacy = new User { Id = Guid.NewGuid(), Username = "legacy_" + Guid.NewGuid().ToString("N")[..12], Email = Guid.NewGuid()+"@example.invalid", CreatedAt = DateTime.UtcNow };
            legacy.PasswordHash = new PasswordHasher<User>().HashPassword(legacy, "Password-123!");
            db.Users.Add(legacy); await db.SaveChangesAsync();
            using var oldClient = factory.CreateClient();
            var login = await Json(await oldClient.PostAsJsonAsync("/api/auth/login", new { username = legacy.Username, password = "Password-123!" }));
            Assert.True(login.GetProperty("needsEmailVerification").GetBoolean());
            Assert.False(login.GetProperty("verificationRequired").GetBoolean());
            oldClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("accessToken").GetString());
            Assert.Equal(HttpStatusCode.OK, (await oldClient.GetAsync("/api/exercises")).StatusCode);
            Assert.False((await db.Users.AsNoTracking().SingleAsync(x => x.Id == legacy.Id)).EmailVerified);
        }
    }

    [PostgresFact]
    public async Task ForgotResetIsGenericPurposeBoundAndRevokesAllOldTokens()
    {
        await using var factory = PostgresCoreCatalogTests.Factory();
        using var c = factory.CreateClient();
        var user = await Register(factory, c);
        await Verify(factory, c, user.Email);
        var oldToken = c.DefaultRequestHeaders.Authorization;
        using var anonymous = factory.CreateClient();
        var unknown = await anonymous.PostAsJsonAsync("/api/auth/forgot-password", new { email = "unknown-"+Guid.NewGuid()+"@example.invalid" });
        var known = await anonymous.PostAsJsonAsync("/api/auth/forgot-password", new { email = user.Email });
        Assert.Equal(HttpStatusCode.Accepted, unknown.StatusCode);
        Assert.Equal(unknown.StatusCode, known.StatusCode);
        Assert.Equal(await unknown.Content.ReadAsStringAsync(), await known.Content.ReadAsStringAsync());
        var repeat = await anonymous.PostAsJsonAsync("/api/auth/forgot-password", new { email = user.Email });
        Assert.Equal(await known.Content.ReadAsStringAsync(), await repeat.Content.ReadAsStringAsync());
        var mail = factory.Services.GetRequiredService<TestEmailSender>();
        Assert.Single(mail.Messages, x => x.Recipient == user.Email && x.Subject.Contains("đặt lại"));
        var verificationCode = mail.Code(user.Email);
        var resetCode = mail.Code(user.Email, "đặt lại");
        // Use another purpose's code only when numerically different (both are random six digits).
        if (verificationCode != resetCode)
            Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsJsonAsync("/api/auth/reset-password/verify", new { email = user.Email, code = verificationCode })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsJsonAsync("/api/auth/reset-password", new { email = user.Email, resetToken = new string('A',64), newPassword = "NewPassword-456!", confirmPassword = "NewPassword-456!" })).StatusCode);
        var grant = await Json(await anonymous.PostAsJsonAsync("/api/auth/reset-password/verify", new { email = user.Email, code = resetCode }));
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsJsonAsync("/api/auth/reset-password/verify", new { email = user.Email, code = resetCode })).StatusCode);
        var request = new { email = user.Email, resetToken = grant.GetProperty("resetToken").GetString(), newPassword = "NewPassword-456!", confirmPassword = "NewPassword-456!" };
        Assert.Equal(HttpStatusCode.BadRequest, (await anonymous.PostAsJsonAsync("/api/auth/reset-password", new { request.email, request.resetToken, request.newPassword, confirmPassword = "mismatch" })).StatusCode);
        var results = await Task.WhenAll(anonymous.PostAsJsonAsync("/api/auth/reset-password", request), anonymous.PostAsJsonAsync("/api/auth/reset-password", request));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(results, x => x.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/exercises")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/auth/login", new { username = user.Name, password = "Password-123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.PostAsJsonAsync("/api/auth/login", new { username = user.Name, password = "NewPassword-456!" })).StatusCode);
        Assert.Contains(mail.Messages, x => x.Recipient == user.Email && x.Subject.Contains("mật khẩu đã thay đổi"));
    }

    [PostgresFact]
    public async Task ChangePasswordValidatesAndRevokesCurrentAndOtherSessions()
    {
        await using var factory = PostgresCoreCatalogTests.Factory();
        using var c = factory.CreateClient();
        var user = await Register(factory, c);
        await Verify(factory, c, user.Email);
        var oldToken = c.DefaultRequestHeaders.Authorization;
        using var other = factory.CreateClient(); other.DefaultRequestHeaders.Authorization = oldToken;
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = "incorrect", newPassword = "ChangedPassword-123!", confirmPassword = "ChangedPassword-123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = "Password-123!", newPassword = "Password-123!", confirmPassword = "Password-123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = "Password-123!", newPassword = "ChangedPassword-123!", confirmPassword = "mismatch" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = "Password-123!", newPassword = "ChangedPassword-123!", confirmPassword = "ChangedPassword-123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await other.GetAsync("/api/exercises")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await other.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = "ChangedPassword-123!", newPassword = "ReplayPassword-123!", confirmPassword = "ReplayPassword-123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await other.PostAsJsonAsync("/api/auth/login", new { username = user.Name, password = "Password-123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await other.PostAsJsonAsync("/api/auth/login", new { username = user.Name, password = "ChangedPassword-123!" })).StatusCode);
    }

    [PostgresFact]
    public async Task PerAccountSendQuotaAndConcurrentVerification()
    {
        var clock = new SecurityClock();
        await using var factory = PostgresCoreCatalogTests.Factory().WithWebHostBuilder(b => b.ConfigureServices(s =>
        { s.RemoveAll<TimeProvider>(); s.AddSingleton<TimeProvider>(clock); }));
        using var c = factory.CreateClient();
        var user = await Register(factory, c);
        for (var i = 0; i < 4; i++) { clock.Advance(61); Assert.Equal(HttpStatusCode.OK, (await c.PostAsync("/api/auth/email-verification/send", null)).StatusCode); }
        clock.Advance(61);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await c.PostAsync("/api/auth/email-verification/send", null)).StatusCode);
        var code = factory.Services.GetRequiredService<TestEmailSender>().Code(user.Email);
        var verified = await Task.WhenAll(c.PostAsJsonAsync("/api/auth/email-verification/verify", new { code }), c.PostAsJsonAsync("/api/auth/email-verification/verify", new { code }));
        Assert.Single(verified, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(verified, x => x.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict);
    }
    [PostgresFact]
    public async Task ResetOtpReplacementExpiryAndGrantExpiryDoNotChangePassword()
    {
        var clock = new SecurityClock();
        await using var factory = PostgresCoreCatalogTests.Factory().WithWebHostBuilder(b => b.ConfigureServices(s =>
        { s.RemoveAll<TimeProvider>(); s.AddSingleton<TimeProvider>(clock); }));
        using var c = factory.CreateClient();
        var user = await Register(factory, c);
        await Verify(factory, c, user.Email);
        var mail = factory.Services.GetRequiredService<TestEmailSender>();
        await c.PostAsJsonAsync("/api/auth/forgot-password", new { email = user.Email });
        var first = mail.Code(user.Email, "đặt lại");
        clock.Advance(61);
        await c.PostAsJsonAsync("/api/auth/forgot-password", new { email = user.Email });
        var second = mail.Code(user.Email, "đặt lại");
        if (first != second)
            Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/auth/reset-password/verify", new { email = user.Email, code = first })).StatusCode);
        clock.Advance(301);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/auth/reset-password/verify", new { email = user.Email, code = second })).StatusCode);
        await c.PostAsJsonAsync("/api/auth/forgot-password", new { email = user.Email });
        var grant = await Json(await c.PostAsJsonAsync("/api/auth/reset-password/verify", new { email = user.Email, code = mail.Code(user.Email, "đặt lại") }));
        clock.Advance(301);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/auth/reset-password", new { email = user.Email, resetToken = grant.GetProperty("resetToken").GetString(), newPassword = "ShouldNotChange-123!", confirmPassword = "ShouldNotChange-123!" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/exercises")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync("/api/auth/login", new { username = user.Name, password = "Password-123!" })).StatusCode);
    }

}
