# Core-Up administration — operator setup

Source implemented; NOT YET VERIFIED. Admin is server-rendered Razor Pages at
`https://localhost:7031/admin`. Run the `https` launch profile after owner-reviewed
build, schema preparation and database-role configuration. The currently running
old binary on HTTP 5027 does not include these source changes.

## Schema and backup (operator-only; not executed by the agent)

Use a private maintenance connection stored outside Git as
`MAINTENANCE_DATABASE_URL`. Never paste credentials into tickets/logs.

```sh
pg_dump "$MAINTENANCE_DATABASE_URL" -Fc -f /private/backup/coreup-before-access.dump
pg_restore --list /private/backup/coreup-before-access.dump
psql "$MAINTENANCE_DATABASE_URL" -X -c 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";'
```

Current development DB was inspected read-only: seven migrations through
`20261022100000_FreeFitnessAssessment` are applied. New migration
`20261024120000_AdminPremiumGoogleAccess` is hand-authored, with Designer and
snapshot metadata, **NOT APPLIED / NOT YET VERIFIED**. It adds role, nullable
passwords, external logins, one current entitlement/account and append-only audit.
It never updates existing passwords, IDs, profiles, workouts or exercise seeds.
`access-schema.sql` is a reference, not an independently runnable migration plan
(it does not update EF history). Do not execute both it and the EF migration.

Before application, the owner should build/review generated EF migration SQL,
check model drift, and rehearse upgrade/backup recovery on a disposable copy.
The agent did not execute these commands:

```sh
dotnet build src/CoreUp.Api/CoreUp.Api.csproj
dotnet tool run dotnet-ef migrations has-pending-model-changes --project src/CoreUp.Infrastructure --startup-project src/CoreUp.Api
dotnet tool run dotnet-ef migrations script 20261022100000_FreeFitnessAssessment 20261024120000_AdminPremiumGoogleAccess --project src/CoreUp.Infrastructure --startup-project src/CoreUp.Api --output /private/backup/access-upgrade.sql
```

Only after review and backup, explicitly select the maintenance connection and
apply through EF. No startup auto-migrations exist. `Down` deliberately refuses to
drop access/audit data; rollback requires a reviewed forward fix or backup recovery.
The append-only trigger is extra database protection, not a replacement for grants.

## Separate runtime credentials

The existing development role `coreup_user` was found to be a PostgreSQL superuser.
That is pre-existing configuration; no privilege was assigned or changed here.
**Do not use it for the new dashboard.** Admin requests refuse an elevated runtime
connection. SQL administration remains separate from the application.

Using the private operator connection, create a separate login without owner,
superuser, createdb, createrole, replication or bypassrls powers (example only):

```sql
CREATE ROLE coreup_app LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS;
-- In psql, set a private password interactively:
\password coreup_app
GRANT CONNECT ON DATABASE coreup_db TO coreup_app;
GRANT USAGE ON SCHEMA public TO coreup_app;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA public TO coreup_app;
REVOKE ALL ON "__EFMigrationsHistory" FROM coreup_app;
REVOKE UPDATE, DELETE, TRUNCATE ON admin_audit_events FROM coreup_app;
GRANT SELECT, INSERT ON admin_audit_events TO coreup_app;
```

Do not grant schema CREATE, table ownership or membership in an elevated role.
Use a private `ConnectionStrings__DefaultConnection` with that login for the web
app. Keep the migration/backup connection separate. Review exact privileges when
future deletion features are implemented. Do not expose PostgreSQL port publicly;
the existing Docker mapping is unchanged by this task. Use psql/pgAdmin over a
private connection, not a SQL console inside the dashboard.

## First admin (never automatic)

Register and verify an ordinary account first. For a Google-only account, use the
existing email recovery flow to set a local password before browser admin login.
Copy the exact User ID using an authorized query. Then run with private operator
credentials (replace UUID and reason):

```sh
psql "$MAINTENANCE_DATABASE_URL" -X -v ON_ERROR_STOP=1 \
  -v user_id=UUID -v role=Admin -v reason='Owner-authorized first administrator' \
  -f tools/admin/set-role.sql
```

The script validates an existing verified account, changes only its role/version,
and appends an atomic `LocalRoleChange` audit event. It is idempotent for unchanged
roles. To revoke administrator access use the same command with `-v role=User` and
a reason. TokenVersion revokes old browser/mobile sessions. Admin is not Premium.
No default account, first-user promotion or secret password exists.

## Browser and entitlement operations

Owner starts `dotnet run --project src/CoreUp.Api --launch-profile https` after
build/configuration; trust the local HTTPS development certificate through the
normal .NET tooling if needed. Bind localhost only. `/admin` refuses non-loopback
requests, plain HTTP and elevated DB credentials. Do not publish it behind a
reverse proxy that makes external requests appear local without independent
network authentication/access controls.

Cookie scheme `AdminCookie`: HttpOnly, Secure always, SameSite Strict, 30-minute
non-sliding session. Razor POST forms use antiforgery. Login: five attempts/minute
per remote IP. Cookie validation checks account/token version; Admin policy checks
verified database role on every protected request. No JWT in browser localStorage.

Overview uses real counts; Users searches username/email/ID with 25-item server
pagination; details show target identity and current entitlement. Grant durations:
7/30/180/365 days, custom UTC or explicit non-expiring demo. Reasons and confirmation
are mandatory; active grants require explicit extension to a later expiration.
Revision and serializable transaction prevent overlapping concurrent decisions.
Grant/extend/revoke and audit commit atomically. Audit is read-only and paginated.

`GET /api/account/entitlement` uses the current JWT subject only. `Premium` policy
queries server state on every request; apply `[Authorize(Policy = "Premium")]` to
future Premium-only API operations. No such workout/nutrition endpoints were added.
Expiry is calculated at request time, without a scheduler. Revocation is immediate
for subsequent protected requests. Free data/history remains intact.

## Read-only SQL examples

```sql
SELECT "Id", "Username", "Email", "EmailVerified", "Role", "CreatedAt"
FROM users ORDER BY "CreatedAt" DESC LIMIT 25;
SELECT "UserId", "Source", "GrantedAt", "ExpiresAt"
FROM premium_entitlements WHERE "Status" = 'Active'
AND ("ExpiresAt" IS NULL OR "ExpiresAt" > now()) ORDER BY "GrantedAt" DESC LIMIT 25;
SELECT "AdminUserId", "TargetUserId", "Action", "Reason", "CreatedAt"
FROM admin_audit_events ORDER BY "CreatedAt" DESC LIMIT 25;
```

Results contain personal data; keep them private. Do not select password/OTP/token
hashes for routine account management. pgAdmin should use separate read-only or
maintenance profiles as appropriate, never a shared public superuser connection.

## Required manual verification

Build and migration/model consistency; existing account password login; verified
admin and ordinary/Guest/Premium authorization; role revoke with old cookie/JWT;
CSRF/login rate cap/HTTPS/cookie expiration; pagination/search; concurrent grants,
renewal validation, expiry/revoke and unchanged histories; audit immutability;
Flutter account switch, unknown entitlement, ad cancellation, resume/expiry.
No automated tests or app builds were run for this source change.

## Local Control Panel operator (owner-requested)

Development localhost can use a dedicated browser operator independent of the
mobile users table. Enable `LocalAdmin:Enabled` and configure `LocalAdmin:Username`
and `LocalAdmin:Password` through private runtime configuration/environment. In
Development the optional private `~/.config/core-up/backend-runtime.json` file is
read first; explicit application/environment configuration takes precedence. No
credentials are included in Git. Production always disables this operator.

This operator signs only the AdminCookie session and receives no Core-Up mobile
JWT or Premium entitlement. It does not create a mobile user, require an email or
change client password rules. Existing verified application administrators remain
supported. The localhost/HTTPS, cookie, CSRF, session expiration and least-privilege
DB requirements still apply. Server restart invalidates local operator sessions.

Migration `20261024143037_LocalPanelAdministrator` makes grant actor attribution
nullable for the independent operator. Local panel changes write append-only audit
actions prefixed `LocalPanel:` with null application-user actor; a real target user
and reason remain mandatory. Grants/revocations retain their transactional checks.
Down deliberately refuses to invent a mobile actor for existing audit history.

### Local graduation-project simplification (2026-10-24)
- Development config `Auth:RequireRegistrationOtp=false`: new password registrations skip OTP delivery and verification navigation, including subsequent login/session restore. EmailVerified remains truthful (false); existing accounts awaiting OTP and Google verification are unchanged.
- Premium grant/revoke reason is optional; blank input gets an automatic audit description without schema changes.
- Admin user detail includes profile metrics, training preferences and questionnaire answers. Original passwords cannot be recovered from hashes; an admin password-reset form replaces the password and revokes prior sessions. Passwords are never included in audit records.
- Validation: backend build 0 errors/warnings; live HTTP registration/me, admin blank-reason Premium grant, password reset/login, and old-token rejection passed. QA account `qa_local_1792852908` retained for inspection. No Flutter commands or simulator run.

### Local OTP inbox (2026-10-25)
- `/admin/otp` displays Core-Up OTP emails from local Mailpit (`localhost:8025`) in a paginated table: username, email, password-set state, six-digit code, purpose, UTC sent time, and user-detail link. Available in Development under existing Admin authorization.
- Refresh retrieves the latest messages; emails remain in Mailpit, no duplicate plaintext OTP/password database storage. Five-minute labels describe message age, not guaranteed validity (codes may have been consumed or replaced).
- Users table includes password state and a password-reset link. Shared table styling improved for readable headers, row hover, and horizontal scrolling on smaller screens.
- Validation: backend build passed with zero warnings/errors; live HTTP checked anonymous redirect, admin login, freshly sent OTP for existing QA account, matching account/detail link, and healthy database. Flutter and Simulator were not run.
