using System.Data.Common;
using CoreUp.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CoreUp.Application;

public sealed class AuthService(
    IApplicationDbContext db,
    IPasswordHasher<User> passwordHasher,
    IJwtTokenService jwtTokens,
    AccountSecurityService? security = null)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var username = request.Username.Trim();
        var normalizedUsername = username.ToLowerInvariant();
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.NormalizedUsername == normalizedUsername, ct))
            throw new AppException(409, "Tên tài khoản đã được sử dụng.");
        if (await db.Users.AnyAsync(x => x.Email == email, ct))
            throw new AppException(409, "Email đã được sử dụng.");

        var user = new User { Id = Guid.NewGuid(), Username = username, Email = email, VerificationRequired = true, CreatedAt = DateTime.UtcNow };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is DbException { SqlState: "23505" })
        {
            // PostgreSQL uniqueness is the final authority, including concurrent requests.
            db.Users.Remove(user);
            if (await db.Users.AnyAsync(x => x.NormalizedUsername == normalizedUsername, ct))
                throw new AppException(409, "Tên tài khoản đã được sử dụng.");
            throw new AppException(409, "Email đã được sử dụng.");
        }

        if (security is not null) await security.SendVerificationAsync(user.Id, ct);
        return new AuthResponse(user.Id, user.Email, jwtTokens.Create(user), true, !user.EmailVerified, user.VerificationRequired);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        var user = await db.Users.Include(x => x.Profile).SingleOrDefaultAsync(x => x.NormalizedUsername == username, ct)
            ?? throw new AppException(401, "Tên tài khoản hoặc mật khẩu không đúng.");

        if (string.IsNullOrEmpty(user.PasswordHash))
            throw new AppException(401, "Tên tài khoản hoặc mật khẩu không đúng.");
        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new AppException(401, "Tên tài khoản hoặc mật khẩu không đúng.");

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        if (security is not null && !user.EmailVerified)
        {
            try { await security.SendVerificationAsync(user.Id, ct); }
            catch (AppException ex) when (ex.StatusCode is 429 or 409 or 503) { }
        }
        return new AuthResponse(user.Id, user.Email, jwtTokens.Create(user), user.Profile is null, !user.EmailVerified, user.VerificationRequired);
    }
}
