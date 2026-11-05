using CoreUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
namespace CoreUp.Infrastructure.Persistence.Migrations;
[DbContext(typeof(CoreUpDbContext))]
[Migration("20261105090000_AccountLifecycle")]
public partial class AccountLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<string>(name: "AccountState", table: "users", type: "character varying(24)", maxLength: 24, nullable: false, defaultValue: "Active");
    protected override void Down(MigrationBuilder migrationBuilder) => throw new System.NotSupportedException("Preserve account lifecycle state; use a reviewed forward migration.");
}
