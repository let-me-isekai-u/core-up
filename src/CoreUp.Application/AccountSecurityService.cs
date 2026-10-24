using System.Data.Common;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using CoreUp.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CoreUp.Application;

public sealed class AccountSecurityService(
    IApplicationDbContext db, IPasswordHasher<User> passwords, IEmailSender email,
    IOptions<OtpOptions> options, TimeProvider clock)
{
    public const string Verification = "verify-email";
    public const string PasswordReset = "reset-password";
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    private string Hash(EmailChallenge c, string value) => Convert.ToHexString(
        HMACSHA256.HashData(Encoding.UTF8.GetBytes(options.Value.Key),
            Encoding.UTF8.GetBytes($"{c.Id}:{c.UserId}:{c.Purpose}:{c.TokenVersion}:{value}")));
    private bool Matches(EmailChallenge c, string value, string? expected) =>
        expected is not null && CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Hash(c, value)), Encoding.UTF8.GetBytes(expected));

    public async Task<OtpSentResponse> SendVerificationAsync(Guid userId, CancellationToken ct)
    {
        var user = await User(userId, ct);
        if (user.EmailVerified) return new("Email đã được xác minh.", 0, 0);
        await IssueAsync(user, Verification, ct);
        return new("Mã xác minh đã được gửi. Vui lòng kiểm tra email.");
    }

    public async Task<OtpSentResponse> ForgotAsync(string address, CancellationToken ct)
    {
        // Same response and minimum response time for unknown, cooldown and quota states.
        // SMTP delivery happens outside the HTTP request via the in-memory sender queue.
        var timer = Stopwatch.StartNew();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == address.Trim().ToLowerInvariant(), ct);
        if (user is not null)
        {
            try { await IssueAsync(user, PasswordReset, ct); }
            catch (AppException ex) when (ex.StatusCode is 429 or 409 or 503) { }
        }
        var remaining = TimeSpan.FromMilliseconds(300) - timer.Elapsed;
        if (remaining > TimeSpan.Zero) await Task.Delay(remaining, ct);
        return new("Nếu email đủ điều kiện, mã đặt lại mật khẩu sẽ được gửi. Vui lòng kiểm tra hộp thư.");
    }

    private async Task IssueAsync(User user, string purpose, CancellationToken ct)
    {
        var now = Now;
        var challenge = await db.EmailChallenges.SingleOrDefaultAsync(x => x.UserId == user.Id && x.Purpose == purpose, ct);
        if (challenge is not null && (now < challenge.SentAt.AddSeconds(60) ||
            (now < challenge.SendWindowStart.AddHours(1) && challenge.SendCount >= 5)))
            throw new AppException(429, "Vui lòng chờ trước khi yêu cầu mã mới (60 giây; tối đa 5 email/giờ).");
        if (challenge is null)
        {
            challenge = new EmailChallenge { Id = Guid.NewGuid(), UserId = user.Id, Purpose = purpose, SendWindowStart = now };
            db.EmailChallenges.Add(challenge);
        }
        if (now >= challenge.SendWindowStart.AddHours(1)) { challenge.SendWindowStart = now; challenge.SendCount = 0; }
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        challenge.TokenVersion = user.TokenVersion;
        challenge.CodeHash = Hash(challenge, code);
        challenge.ExpiresAt = now.AddMinutes(5);
        challenge.SentAt = now;
        challenge.Attempts = 0;
        challenge.Consumed = false;
        challenge.ResetGrantHash = null;
        challenge.ResetGrantExpiresAt = null;
        challenge.ResetGrantConsumed = false;
        challenge.SendCount++;
        challenge.Revision++;
        await SaveAsync(ct);
        await email.SendAsync(user.Email, purpose == Verification ? "Core-Up: xác minh email" : "Core-Up: đặt lại mật khẩu",
            $"Mã OTP của bạn: {code}\nMã có hiệu lực 5 phút, chỉ dùng một lần. Không chia sẻ mã này.\nNếu không yêu cầu, hãy bỏ qua email này.", ct);
    }

    private async Task<EmailChallenge> ValidateCodeAsync(User user, string purpose, string code, CancellationToken ct)
    {
        var challenge = await db.EmailChallenges.SingleOrDefaultAsync(x => x.UserId == user.Id && x.Purpose == purpose, ct);
        if (challenge is null || challenge.Consumed || challenge.Attempts >= 5 || challenge.ExpiresAt <= Now || challenge.TokenVersion != user.TokenVersion)
            throw InvalidCode();
        challenge.Revision++;
        if (!Matches(challenge, code, challenge.CodeHash))
        {
            challenge.Attempts++;
            await SaveAsync(ct);
            throw InvalidCode();
        }
        challenge.Consumed = true;
        return challenge;
    }

    public async Task VerifyEmailAsync(Guid userId, string code, CancellationToken ct)
    {
        var user = await User(userId, ct);
        await ValidateCodeAsync(user, Verification, code, ct);
        user.EmailVerified = true;
        user.UpdatedAt = Now;
        await SaveAsync(ct); // Consuming the OTP and updating the user is one transaction.
    }

    public async Task<ResetGrantResponse> VerifyResetAsync(string address, string code, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == address.Trim().ToLowerInvariant(), ct) ?? throw InvalidCode();
        var challenge = await ValidateCodeAsync(user, PasswordReset, code, ct);
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        challenge.ResetGrantHash = Hash(challenge, "grant:" + token);
        challenge.ResetGrantExpiresAt = Now.AddMinutes(5);
        await SaveAsync(ct);
        return new(token, challenge.ResetGrantExpiresAt.Value);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == request.Email.Trim().ToLowerInvariant(), ct) ?? throw InvalidCode();
        var c = await db.EmailChallenges.SingleOrDefaultAsync(x => x.UserId == user.Id && x.Purpose == PasswordReset, ct);
        if (c is null || !c.Consumed || c.ResetGrantConsumed || c.ResetGrantExpiresAt <= Now || c.TokenVersion != user.TokenVersion ||
            !Matches(c, "grant:" + request.ResetToken, c.ResetGrantHash)) throw InvalidCode();
        c.ResetGrantConsumed = true;
        c.ResetGrantHash = null;
        c.Revision++;
        user.PasswordHash = passwords.HashPassword(user, request.NewPassword);
        user.TokenVersion++;
        user.UpdatedAt = Now;
        await SaveAsync(ct); // User version + grant revision prevent replay/races.
        await NotifyChangedAsync(user, ct);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await User(userId, ct);
        if (string.IsNullOrEmpty(user.PasswordHash))
            throw new AppException(400, "Tài khoản dùng Google. Dùng khôi phục qua email để thiết lập mật khẩu trước.");
        if (passwords.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
            throw new AppException(400, "Mật khẩu hiện tại không đúng.");
        if (passwords.VerifyHashedPassword(user, user.PasswordHash, request.NewPassword) != PasswordVerificationResult.Failed)
            throw new AppException(400, "Mật khẩu mới phải khác mật khẩu hiện tại.");
        user.PasswordHash = passwords.HashPassword(user, request.NewPassword);
        user.TokenVersion++;
        user.UpdatedAt = Now;
        await SaveAsync(ct);
        await NotifyChangedAsync(user, ct);
    }

    private async Task NotifyChangedAsync(User user, CancellationToken ct)
    {
        // A delivery queue failure must not turn a committed password change into an apparent
        // failure/retry. The user can request a fresh reset if needed.
        try { await email.SendAsync(user.Email, "Core-Up: mật khẩu đã thay đổi", "Mật khẩu Core-Up vừa được thay đổi. Mọi phiên cũ đã bị thu hồi. Nếu không phải bạn, hãy đặt lại mật khẩu ngay.", ct); }
        catch (AppException ex) when (ex.StatusCode == 503) { }
    }
    private async Task<User> User(Guid id, CancellationToken ct) =>
        await db.Users.SingleOrDefaultAsync(x => x.Id == id, ct)
        ?? throw new AppException(401, "Phiên đăng nhập không hợp lệ.");
    private static AppException InvalidCode() => new(400, "Mã không hợp lệ, đã hết hạn, đã dùng hoặc đã bị thay thế. Vui lòng yêu cầu mã mới.");
    private async Task SaveAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new AppException(409, "Yêu cầu đã được xử lý hoặc thay đổi. Vui lòng thử lại với mã mới."); }
        catch (DbUpdateException ex) when (ex.InnerException is DbException { SqlState: "23505" })
        { throw new AppException(429, "Một yêu cầu mã khác đang được xử lý. Vui lòng thử lại sau."); }
    }
}
