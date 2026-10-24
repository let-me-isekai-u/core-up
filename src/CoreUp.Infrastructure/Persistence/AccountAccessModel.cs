using CoreUp.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreUp.Infrastructure.Persistence;

internal static class AccountAccessModel
{
    public static void Configure(ModelBuilder model)
    {
        model.Entity<UserExternalLogin>(e => {
            e.ToTable("user_external_logins"); e.HasKey(x => x.Id);
            e.Property(x => x.Provider).HasMaxLength(32).IsRequired();
            e.Property(x => x.ProviderSubject).HasMaxLength(255).IsRequired();
            e.HasIndex(x => new { x.Provider, x.ProviderSubject }).IsUnique();
            e.HasIndex(x => new { x.UserId, x.Provider }).IsUnique();
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<PremiumEntitlement>(e => {
            e.ToTable("premium_entitlements", t => {
                t.HasCheckConstraint("CK_premium_dates", "\"ExpiresAt\" IS NULL OR \"ExpiresAt\" > \"GrantedAt\"");
                t.HasCheckConstraint("CK_premium_state", "(\"Status\" = 'Active' AND \"RevokedAt\" IS NULL AND \"RevokedByAdminId\" IS NULL) OR (\"Status\" = 'Revoked' AND \"RevokedAt\" IS NOT NULL AND \"RevocationReason\" IS NOT NULL AND length(\"RevocationReason\") > 0)");
                t.HasCheckConstraint("CK_premium_source", "\"Source\" = 'AdminDemoGrant'");
            });
            e.HasKey(x => x.Id); e.HasIndex(x => x.UserId).IsUnique();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.Source).HasConversion<string>().HasMaxLength(32);
            e.Property(x => x.RevocationReason).HasMaxLength(1000);
            e.Property(x => x.Revision).IsConcurrencyToken();
            e.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.GrantedByAdminId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.RevokedByAdminId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<AdminAuditEvent>(e => {
            e.ToTable("admin_audit_events"); e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(64).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
            e.Property(x => x.PreviousState).IsRequired(); e.Property(x => x.NewState).IsRequired();
            e.HasIndex(x => x.CreatedAt); e.HasIndex(x => new { x.TargetUserId, x.CreatedAt });
            e.HasOne<User>().WithMany().HasForeignKey(x => x.AdminUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<User>().WithMany().HasForeignKey(x => x.TargetUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
