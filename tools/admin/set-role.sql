-- Run only through an authorized private schema-owner/operator connection.
-- psql "$MAINTENANCE_DATABASE_URL" -X -v ON_ERROR_STOP=1 -v user_id=UUID -v role=Admin -v reason='Owner bootstrap' -f tools/admin/set-role.sql
-- Setting role=User revokes admin immediately (next request), without removing Premium.
BEGIN;
SELECT EXISTS (SELECT 1 FROM users WHERE "Id" = :'user_id'::uuid AND ("EmailVerified" OR :'role' = 'User'))
 AND :'role' IN ('Admin','User') AND length(trim(:'reason')) BETWEEN 3 AND 1000 AS allowed \gset
\if :allowed
WITH previous AS (
 SELECT "Id", "Role" FROM users WHERE "Id" = :'user_id'::uuid FOR UPDATE
), changed AS (
 UPDATE users u SET "Role" = :'role', "TokenVersion" = "TokenVersion" + 1, "UpdatedAt" = now()
 FROM previous p WHERE u."Id" = p."Id" AND u."Role" <> :'role'
 RETURNING u."Id", p."Role" AS old_role, u."Role" AS new_role
)
INSERT INTO admin_audit_events ("Id","AdminUserId","TargetUserId","Action","PreviousState","NewState","Reason","CreatedAt")
 SELECT gen_random_uuid(),NULL,"Id",'LocalRoleChange',json_build_object('Role',old_role)::text,
 json_build_object('Role',new_role)::text,trim(:'reason'),now() FROM changed;
COMMIT;
\else
ROLLBACK;
\echo 'Refused: existing account, verified email for promotion, valid role and reason required.'
\quit 1
\endif
