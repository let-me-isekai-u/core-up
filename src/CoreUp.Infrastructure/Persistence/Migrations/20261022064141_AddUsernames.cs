using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreUp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsernames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "users",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedUsername",
                table: "users",
                type: "character varying(24)",
                maxLength: 24,
                nullable: false,
                computedColumnSql: "lower(\"Username\")",
                stored: true);

            // All legacy accounts receive a deterministic, ASCII-safe unique name.
            // No email-derived names: punctuation, case and repeated email prefixes cannot collide.
            migrationBuilder.Sql("""
                WITH assigned AS (
                    SELECT "Id", 'u_' || row_number() OVER (ORDER BY "Id")::text AS username
                    FROM users
                )
                UPDATE users SET "Username" = assigned.username
                FROM assigned WHERE users."Id" = assigned."Id";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_users_NormalizedUsername",
                table: "users",
                column: "NormalizedUsername",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_NormalizedUsername",
                table: "users");

            migrationBuilder.DropColumn(
                name: "NormalizedUsername",
                table: "users");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "users");
        }
    }
}
