using System.Data;
using System.Text.Json;
using CoreUp.Application;
using CoreUp.Domain;
using CoreUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreUp.Infrastructure.Administration;

public sealed record EntitlementResponse(bool HasPremium, string Status, string? Source, DateTime? ExpiresAt);

public sealed class AccountAccessService(CoreUpDbContext db, TimeProvider clock)
{
    public async Task<EntitlementResponse> CurrentAsync(Guid userId, CancellationToken ct)
    {
        var e = await db.PremiumEntitlements.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct);
        if (e is null) return new(false, "Free", null, null);
        var active = e.Status == PremiumStatus.Active && (e.ExpiresAt == null || e.ExpiresAt > clock.GetUtcNow().UtcDateTime);
        return new(active, e.Status == PremiumStatus.Revoked ? "Revoked" : active ? "Active" : "Expired", e.Source.ToString(), e.ExpiresAt);
    }

    public async Task ChangeAsync(Guid actor, Guid target, bool revoke, DateTime? expiration,
        bool nonExpiring, bool extend, string reason, int expectedRevision, CancellationToken ct)
    {
        reason = reason.Trim();
        if (reason.Length is < 3 or > 1000) throw new AppException(400, "Reason must contain 3–1000 characters.");
        var now = clock.GetUtcNow().UtcDateTime;
        if (!revoke && ((!nonExpiring && expiration == null) || (nonExpiring && expiration != null) || expiration <= now))
            throw new AppException(400, "Select a future UTC expiration or explicitly select non-expiring access.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        // Recheck authorization inside the transaction as well as at the HTTP boundary.
        if (!await db.Users.AnyAsync(x => x.Id == actor && x.Role == ApplicationRole.Admin && x.EmailVerified, ct))
            throw new AppException(403, "Administrator access required.");
        if (!await db.Users.AnyAsync(x => x.Id == target, ct)) throw new AppException(404, "Account not found.");
        var e = await db.PremiumEntitlements.SingleOrDefaultAsync(x => x.UserId == target, ct);
        if ((e?.Revision ?? 0) != expectedRevision) throw new AppException(409, "Entitlement changed. Reload and confirm again.");
        var previous = e is null ? "null" : JsonSerializer.Serialize(e);
        var active = e?.Status == PremiumStatus.Active && (e.ExpiresAt == null || e.ExpiresAt > now);
        if (revoke && (e == null || e.Status == PremiumStatus.Revoked)) throw new AppException(409, "No entitlement to revoke.");
        if (!revoke && active && !extend) throw new AppException(409, "Explicit extension confirmation is required.");
        if (!revoke && active && (e!.ExpiresAt == null || (!nonExpiring && expiration <= e.ExpiresAt)))
            throw new AppException(400, "An extension must increase the expiration; revoke to remove non-expiring access.");
        if (e == null) {
            e = new PremiumEntitlement { Id = Guid.NewGuid(), UserId = target, CreatedAt = now };
            db.PremiumEntitlements.Add(e);
        }
        if (revoke) {
            e.Status = PremiumStatus.Revoked; e.RevokedAt = now; e.RevokedByAdminId = actor; e.RevocationReason = reason;
        } else {
            e.Status = PremiumStatus.Active; e.Source = PremiumSource.AdminDemoGrant; e.GrantedAt = now;
            e.ExpiresAt = nonExpiring ? null : expiration; e.GrantedByAdminId = actor;
            e.RevokedAt = null; e.RevokedByAdminId = null; e.RevocationReason = null;
        }
        e.UpdatedAt = now; e.Revision++;
        db.AdminAuditEvents.Add(new AdminAuditEvent { Id = Guid.NewGuid(), AdminUserId = actor, TargetUserId = target,
            Action = revoke ? "PremiumRevoked" : active ? "PremiumExtended" : "PremiumGranted",
            PreviousState = previous, NewState = JsonSerializer.Serialize(e), Reason = reason, CreatedAt = now });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
