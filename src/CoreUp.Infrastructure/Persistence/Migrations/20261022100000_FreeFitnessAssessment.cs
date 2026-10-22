using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreUp.Infrastructure.Persistence.Migrations;

// Source-only migration. Not applied to any database in this implementation session.
public partial class FreeFitnessAssessment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<decimal>(name: "HeightCm", table: "user_profiles", type: "numeric(6,2)", precision: 6, scale: 2,
            nullable: true, oldClrType: typeof(decimal), oldType: "numeric(6,2)", oldPrecision: 6, oldScale: 2);
        migrationBuilder.AlterColumn<decimal>(name: "WeightKg", table: "user_profiles", type: "numeric(6,2)", precision: 6, scale: 2,
            nullable: true, oldClrType: typeof(decimal), oldType: "numeric(6,2)", oldPrecision: 6, oldScale: 2);
        migrationBuilder.AlterColumn<decimal>(name: "Bmi", table: "user_profiles", type: "numeric(5,2)", precision: 5, scale: 2,
            nullable: true, oldClrType: typeof(decimal), oldType: "numeric(5,2)", oldPrecision: 5, oldScale: 2);
        migrationBuilder.AlterColumn<string>(name: "BmiCategory", table: "user_profiles", type: "character varying(20)", maxLength: 20,
            nullable: true, oldClrType: typeof(string), oldType: "character varying(20)", oldMaxLength: 20);
        migrationBuilder.AddColumn<int>(name: "QuestionnaireVersion", table: "user_profiles", type: "integer", nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<string>(name: "QuestionnaireAnswers", table: "user_profiles", type: "text", nullable: true);
        migrationBuilder.AddColumn<DateTime>(name: "QuestionnaireCompletedAt", table: "user_profiles", type: "timestamp with time zone", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Refuse to invent measurements to satisfy the old required columns.
        migrationBuilder.Sql("""
            DO $$ BEGIN
                IF EXISTS (SELECT 1 FROM user_profiles WHERE "HeightCm" IS NULL OR "WeightKg" IS NULL OR "Bmi" IS NULL OR "BmiCategory" IS NULL OR "QuestionnaireCompletedAt" IS NOT NULL) THEN
                    RAISE EXCEPTION 'Cannot roll back FreeFitnessAssessment without losing optional measurement or questionnaire data. Use a reviewed forward migration.';
                END IF;
            END; $$;
            """);
        migrationBuilder.DropColumn(name: "QuestionnaireVersion", table: "user_profiles");
        migrationBuilder.DropColumn(name: "QuestionnaireAnswers", table: "user_profiles");
        migrationBuilder.DropColumn(name: "QuestionnaireCompletedAt", table: "user_profiles");
        migrationBuilder.AlterColumn<decimal>(name: "HeightCm", table: "user_profiles", type: "numeric(6,2)", precision: 6, scale: 2,
            nullable: false, oldClrType: typeof(decimal), oldType: "numeric(6,2)", oldPrecision: 6, oldScale: 2, oldNullable: true);
        migrationBuilder.AlterColumn<decimal>(name: "WeightKg", table: "user_profiles", type: "numeric(6,2)", precision: 6, scale: 2,
            nullable: false, oldClrType: typeof(decimal), oldType: "numeric(6,2)", oldPrecision: 6, oldScale: 2, oldNullable: true);
        migrationBuilder.AlterColumn<decimal>(name: "Bmi", table: "user_profiles", type: "numeric(5,2)", precision: 5, scale: 2,
            nullable: false, oldClrType: typeof(decimal), oldType: "numeric(5,2)", oldPrecision: 5, oldScale: 2, oldNullable: true);
        migrationBuilder.AlterColumn<string>(name: "BmiCategory", table: "user_profiles", type: "character varying(20)", maxLength: 20,
            nullable: false, oldClrType: typeof(string), oldType: "character varying(20)", oldMaxLength: 20, oldNullable: true);
    }
}
