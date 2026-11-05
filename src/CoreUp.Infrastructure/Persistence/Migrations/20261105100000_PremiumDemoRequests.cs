using CoreUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace CoreUp.Infrastructure.Persistence.Migrations;
[DbContext(typeof(CoreUpDbContext))]
[Migration("20261105100000_PremiumDemoRequests")]
public partial class PremiumDemoRequests : Migration {
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        CREATE TABLE premium_demo_requests (
            "Id" uuid PRIMARY KEY, "UserId" uuid NOT NULL REFERENCES users("Id") ON DELETE RESTRICT,
            "Status" varchar(16) NOT NULL, "Reason" varchar(1000) NOT NULL,
            "Revision" integer NOT NULL, "CreatedAt" timestamptz NOT NULL, "DecidedAt" timestamptz NULL
        );
        CREATE UNIQUE INDEX "IX_premium_demo_requests_UserId" ON premium_demo_requests ("UserId") WHERE "Status" = 'Pending';
        """);
    protected override void Down(MigrationBuilder migrationBuilder) => throw new System.NotSupportedException("Preserve request history; use a reviewed forward migration.");
}
