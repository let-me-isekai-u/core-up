using System.ComponentModel.DataAnnotations;
using CoreUp.Application;
using CoreUp.Domain;
using CoreUp.Infrastructure.Persistence;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CoreUp.Infrastructure.Authentication;

public sealed class GoogleTokenRequest
{
    [Required, MaxLength(16384)] public string IdToken { get; init; } = "";
}
public sealed class GoogleLinkRequest
{
    [Required, MaxLength(16384)] public string IdToken { get; init; } = "";
    [Required, MaxLength(128)] public string CurrentPassword { get; init; } = "";
}

public sealed class GoogleIdentityService(CoreUpDbContext db, IConfiguration config,
    IJwtTokenService tokens, IPasswordHasher<User> passwords, AccountSecurityService security)
{
    private async Task<GoogleJsonWebSignature.Payload> Validate(string token)
    {
        var audiences = config.GetSection("Google:ClientIds").Get<string[]>() ?? [];
        if (audiences.Length == 0 || audiences.Any(string.IsNullOrWhiteSpace))
            throw new AppException(503, "Google Sign-In chưa được cấu hình trên server.");
        try {
            var payload = await GoogleJsonWebSignature.ValidateAsync(token,
                new GoogleJsonWebSignature.ValidationSettings { Audience = audiences });
            if (string.IsNullOrWhiteSpace(payload.Subject) || payload.Subject.Length > 255 ||
                !payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Email) ||
                payload.Email.Length > 320 || !new EmailAddressAttribute().IsValid(payload.Email) ||
                payload.Issuer is not ("accounts.google.com" or "https://accounts.google.com"))
                throw new AppException(401, "Danh tính Google không hợp lệ hoặc email chưa xác minh.");
            return payload;
        } catch (InvalidJwtException) {
            throw new AppException(401, "Google ID token không hợp lệ hoặc đã hết hạn.");
        } catch (HttpRequestException) {
            throw new AppException(503, "Chưa kết nối được dịch vụ xác minh Google. Thử lại sau.");
        }
    }

    public async Task<AuthResponse> SignInAsync(string token, CancellationToken ct)
    {
        var identity = await Validate(token);
        var login = await db.ExternalLogins.AsNoTracking().SingleOrDefaultAsync(x => x.Provider == "Google" && x.ProviderSubject == identity.Subject, ct);
        User user;
        if (login != null) {
            user = await db.Users.Include(x => x.Profile).SingleAsync(x => x.Id == login.UserId, ct);
        } else {
            var email = identity.Email.Trim().ToLowerInvariant();
            if (await db.Users.AnyAsync(x => x.Email == email, ct))
                throw new AppException(409, "Email này thuộc tài khoản Core-Up hiện có. Đăng nhập Core-Up rồi liên kết Google trong hồ sơ.");
            // Google is authoritative for Gmail and verified Workspace domains.
            var trustedEmail = email.EndsWith("@gmail.com", StringComparison.Ordinal) || !string.IsNullOrWhiteSpace(identity.HostedDomain);
            var id = Guid.NewGuid();
            user = new User { Id = id, Username = "g_" + id.ToString("N")[..22], Email = email,
                PasswordHash = null, EmailVerified = trustedEmail, VerificationRequired = true, CreatedAt = DateTime.UtcNow };
            db.Users.Add(user);
            db.ExternalLogins.Add(new UserExternalLogin { Id = Guid.NewGuid(), UserId = id, Provider = "Google", ProviderSubject = identity.Subject, CreatedAt = DateTime.UtcNow });
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" }) {
                throw new AppException(409, "Thông tin tài khoản vừa thay đổi. Thử đăng nhập lại; không tự liên kết theo email.");
            }
        }
        if (!user.EmailVerified) {
            try { await security.SendVerificationAsync(user.Id, ct); }
            catch (AppException ex) when (ex.StatusCode is 409 or 429 or 503) { }
        }
        return new(user.Id, user.Email, tokens.Create(user), user.Profile == null, !user.EmailVerified, user.VerificationRequired);
    }

    public async Task LinkAsync(Guid userId, GoogleLinkRequest request, CancellationToken ct)
    {
        var user = await db.Users.SingleAsync(x => x.Id == userId, ct);
        if (!user.EmailVerified || string.IsNullOrEmpty(user.PasswordHash) ||
            passwords.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
            throw new AppException(403, "Cần tài khoản đã xác minh và mật khẩu Core-Up hiện tại để liên kết.");
        var identity = await Validate(request.IdToken);
        if (!string.Equals(user.Email, identity.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new AppException(409, "Chọn Google có cùng email với tài khoản Core-Up.");
        var existing = await db.ExternalLogins.Where(x => x.Provider == "Google" &&
            (x.ProviderSubject == identity.Subject || x.UserId == userId)).Take(2).ToListAsync(ct);
        if (existing.Count > 0) {
            if (existing.Count == 1 && existing[0].UserId == userId && existing[0].ProviderSubject == identity.Subject) return;
            throw new AppException(409, "Không thể liên kết danh tính Google này.");
        }
        db.ExternalLogins.Add(new UserExternalLogin { Id = Guid.NewGuid(), UserId = userId,
            Provider = "Google", ProviderSubject = identity.Subject, CreatedAt = DateTime.UtcNow });
        // Conflict with password-reset/revocation commits prevents a stale proof from linking.
        user.TokenVersion++; user.UpdatedAt = DateTime.UtcNow;
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new AppException(409, "Phiên đã thay đổi. Đăng nhập lại trước khi liên kết."); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" }) {
            throw new AppException(409, "Danh tính đã được liên kết. Không thay đổi tài khoản hiện có.");
        }
    }
}
