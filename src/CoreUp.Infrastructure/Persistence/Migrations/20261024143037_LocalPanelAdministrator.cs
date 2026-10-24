using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreUp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LocalPanelAdministrator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_premium_state",
                table: "premium_entitlements");

            migrationBuilder.AlterColumn<Guid>(
                name: "GrantedByAdminId",
                table: "premium_entitlements",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddCheckConstraint(
                name: "CK_premium_state",
                table: "premium_entitlements",
                sql: "(\"Status\" = 'Active' AND \"RevokedAt\" IS NULL AND \"RevokedByAdminId\" IS NULL) OR (\"Status\" = 'Revoked' AND \"RevokedAt\" IS NOT NULL AND \"RevocationReason\" IS NOT NULL AND length(\"RevocationReason\") > 0)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) =>
            throw new NotSupportedException("Local panel audit attribution must be preserved. Use a reviewed forward migration.");
    }
}
