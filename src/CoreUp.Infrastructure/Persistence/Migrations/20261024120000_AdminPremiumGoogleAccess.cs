using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace CoreUp.Infrastructure.Persistence.Migrations;
// Hand-authored; NOT YET VERIFIED. Apply only after backup and operator review.
public partial class AdminPremiumGoogleAccess : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
ALTER TABLE users ALTER COLUMN "PasswordHash" DROP NOT NULL;
ALTER TABLE users ADD COLUMN "Role" character varying(16) NOT NULL DEFAULT 'User';
ALTER TABLE users ADD CONSTRAINT "CK_users_role" CHECK ("Role" IN ('User','Admin'));
CREATE TABLE user_external_logins (
  "Id" uuid NOT NULL,
  "UserId" uuid NOT NULL,
  "Provider" character varying(32) NOT NULL,
  "ProviderSubject" character varying(255) NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  CONSTRAINT "PK_user_external_logins" PRIMARY KEY ("Id"),
  CONSTRAINT "FK_user_external_logins_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE RESTRICT
);
CREATE UNIQUE INDEX "IX_user_external_logins_Provider_ProviderSubject" ON user_external_logins ("Provider", "ProviderSubject");
CREATE UNIQUE INDEX "IX_user_external_logins_UserId_Provider" ON user_external_logins ("UserId", "Provider");
CREATE TABLE premium_entitlements (
  "Id" uuid NOT NULL,
  "UserId" uuid NOT NULL,
  "Status" character varying(16) NOT NULL,
  "Source" character varying(32) NOT NULL,
  "GrantedAt" timestamp with time zone NOT NULL,
  "ExpiresAt" timestamp with time zone,
  "RevokedAt" timestamp with time zone,
  "GrantedByAdminId" uuid NOT NULL,
  "RevokedByAdminId" uuid,
  "RevocationReason" character varying(1000),
  "CreatedAt" timestamp with time zone NOT NULL,
  "UpdatedAt" timestamp with time zone NOT NULL,
  "Revision" integer NOT NULL,
  CONSTRAINT "PK_premium_entitlements" PRIMARY KEY ("Id"),
  CONSTRAINT "FK_premium_entitlements_users_UserId" FOREIGN KEY ("UserId") REFERENCES users ("Id") ON DELETE RESTRICT,
  CONSTRAINT "FK_premium_entitlements_users_GrantedByAdminId" FOREIGN KEY ("GrantedByAdminId") REFERENCES users ("Id") ON DELETE RESTRICT,
  CONSTRAINT "FK_premium_entitlements_users_RevokedByAdminId" FOREIGN KEY ("RevokedByAdminId") REFERENCES users ("Id") ON DELETE RESTRICT,
  CONSTRAINT "CK_premium_dates" CHECK ("ExpiresAt" IS NULL OR "ExpiresAt" > "GrantedAt"),
  CONSTRAINT "CK_premium_state" CHECK (("Status" = 'Active' AND "RevokedAt" IS NULL AND "RevokedByAdminId" IS NULL) OR ("Status" = 'Revoked' AND "RevokedAt" IS NOT NULL AND "RevokedByAdminId" IS NOT NULL AND "RevocationReason" IS NOT NULL AND length("RevocationReason") > 0)),
  CONSTRAINT "CK_premium_source" CHECK ("Source" = 'AdminDemoGrant')
);
CREATE UNIQUE INDEX "IX_premium_entitlements_UserId" ON premium_entitlements ("UserId");
CREATE INDEX "IX_premium_entitlements_GrantedByAdminId" ON premium_entitlements ("GrantedByAdminId");
CREATE INDEX "IX_premium_entitlements_RevokedByAdminId" ON premium_entitlements ("RevokedByAdminId");
CREATE TABLE admin_audit_events (
  "Id" uuid NOT NULL,
  "AdminUserId" uuid,
  "TargetUserId" uuid NOT NULL,
  "Action" character varying(64) NOT NULL,
  "PreviousState" text NOT NULL,
  "NewState" text NOT NULL,
  "Reason" character varying(1000) NOT NULL,
  "CreatedAt" timestamp with time zone NOT NULL,
  CONSTRAINT "PK_admin_audit_events" PRIMARY KEY ("Id"),
  CONSTRAINT "FK_admin_audit_events_users_AdminUserId" FOREIGN KEY ("AdminUserId") REFERENCES users ("Id") ON DELETE RESTRICT,
  CONSTRAINT "FK_admin_audit_events_users_TargetUserId" FOREIGN KEY ("TargetUserId") REFERENCES users ("Id") ON DELETE RESTRICT
);
CREATE INDEX "IX_admin_audit_events_AdminUserId" ON admin_audit_events ("AdminUserId");
CREATE INDEX "IX_admin_audit_events_CreatedAt" ON admin_audit_events ("CreatedAt");
CREATE INDEX "IX_admin_audit_events_TargetUserId_CreatedAt" ON admin_audit_events ("TargetUserId", "CreatedAt");
CREATE FUNCTION coreup_audit_append_only() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN RAISE EXCEPTION 'Administrative audit events are append-only'; END;
$$;
CREATE TRIGGER coreup_audit_append_only BEFORE UPDATE OR DELETE ON admin_audit_events
FOR EACH ROW EXECUTE FUNCTION coreup_audit_append_only();
""");
    }
    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException("Access/audit data must be preserved. Use a reviewed forward migration or operator backup recovery.");
}
