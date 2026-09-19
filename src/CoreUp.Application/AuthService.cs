using CoreUp.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CoreUp.Application;

public sealed class AuthService(
    IApplicationDbContext db,
    IPasswordHasher<User> passwordHasher,
    IJwtTokenService jwtTokens)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email, ct))
            throw new AppException(409, "Email is already registered.");

        var user = new User { Id = Guid.NewGuid(), Email = email, CreatedAt = DateTime.UtcNow };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return new AuthResponse(user.Id, user.Email, jwtTokens.Create(user), true);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.Include(x => x.Profile).SingleOrDefaultAsync(x => x.Email == email, ct)
            ?? throw new AppException(401, "Invalid email or password.");

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new AppException(401, "Invalid email or password.");

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return new AuthResponse(user.Id, user.Email, jwtTokens.Create(user), user.Profile is null);
    }
}
