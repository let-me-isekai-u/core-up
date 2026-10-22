using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreUp.Infrastructure.Persistence.Migrations;

public partial class ImportHollowBodyHold : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Keep existing IDs, prescriptions and history, including manually imported aliases.
        // Literal data is frozen here so future seed edits cannot change this migration.
        migrationBuilder.Sql("""
            INSERT INTO exercises
                ("Id", "CreatedAt", "DefaultDurationSeconds", "DefaultReps", "DefaultRestSeconds",
                 "DefaultSets", "Description", "Difficulty", "ExerciseType", "ImageUrl",
                 "Instructions", "IsActive", "MuscleGroup", "Name", "Slug")
            SELECT '00000000-0000-0000-0000-000000000026', '2026-01-01T00:00:00Z',
                   NULL, NULL, 0, NULL,
                   'Chờ duyệt chuyên môn. Nguồn: Thomas et al. (2017), The effects of a calisthenics training intervention on posture, strength and body composition, Isokinetics and Exercise Science 25, 215–222. DOI: 10.3233/IES-170001. Bảng 1, tr. 217; mục 2.5.4 và Hình 1f, tr. 218. Tên gốc: Hollow body position. Nghiên cứu dùng 4 hiệp × 45 giây trong chương trình phối hợp; không xác lập liều tập riêng cho người mới hoặc thừa cân. Không chứng minh giảm mỡ bụng tại chỗ.',
                   'Advanced', 'Duration', NULL,
                   'Tư thế giữ tĩnh minh họa tại Hình 1f, tr. 218. Chưa cung cấp hướng dẫn tự tập: cần duyệt kỹ thuật, biến thể phù hợp, tiêu chí dừng, số hiệp, thời gian giữ và nghỉ trước khi kích hoạt. Advanced là phân loại tạm của ứng dụng, không phải kết luận nghiên cứu; giá trị nghỉ 0 là dữ liệu chờ duyệt, không phải khuyến nghị tập không nghỉ.',
                   FALSE, 'Core', 'Giữ thân rỗng (Hollow Body Hold)', 'hollow-body-hold'
            WHERE NOT EXISTS (
                SELECT 1 FROM exercises
                WHERE lower(replace(replace(replace(trim("Slug"), '-', ''), '_', ''), ' ', ''))
                          IN ('hollowbodyhold', 'hollowbodyposition', 'hollowhold', 'hollowbody')
                   OR lower(replace(replace(replace(trim("Name"), '-', ''), '_', ''), ' ', ''))
                          IN ('hollowbodyhold', 'hollowbodyposition', 'hollowhold', 'hollowbody',
                              'hollowbodyposition/hollowbodyhold', 'hollowbodyhold/hollowbodyposition',
                              'giữthânrỗng', 'giữthânrỗng(hollowbodyhold)')
            );
            """);
        // An unrelated record occupying the reserved ID fails loudly via the primary key.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally retain catalog data: it may now be referenced by workout history,
        // and an alias match may have meant Up inserted nothing. Never delete user data.
    }
}
