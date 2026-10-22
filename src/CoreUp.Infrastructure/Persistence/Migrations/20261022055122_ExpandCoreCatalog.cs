using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreUp.Infrastructure.Persistence.Migrations;

public partial class ExpandCoreCatalog : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Keep all identities, descriptions and historical relationships. Mobility, aerobic,
        // hip-extension and posterior-chain entries are outside this abdominal catalog.
        migrationBuilder.Sql("UPDATE exercises SET \"IsActive\" = FALSE WHERE \"MuscleGroup\" <> 'Core';");
        // Freeze reviewed data in this migration; never read a mutable current seed here.
        var rows = new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000027"), new DateTime(2026, 10, 22, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, null, "Bài nhận biết vị trí chậu/cột sống. Chờ duyệt để dùng như bài tăng sức mạnh bụng; nguồn không hướng dẫn siết bụng và số lần do chuyên viên quyết định. Nguồn kỹ thuật: https://www.southtees.nhs.uk/resources/pelvic-tilt/.", "Beginner", "Repetition", null, "1. Nằm ngửa, gối gập, bàn chân tựa giường/thảm. 2. Nghiêng chậu nhẹ để lưng dưới chạm mặt tựa. 3. Nghiêng trở lại tạo một khoảng cong nhỏ; về giữa. Thở bình thường, không gồng bụng hoặc ép biên độ. Dụng cụ: mặt tựa chắc, thảm tùy chọn. Chưa có liều tập trong ứng dụng. Dừng khi đau, chóng mặt, khó thở, bụng phồng bất thường hoặc không giữ được tư thế. Không nín thở hay cố tập qua đau. Người có bệnh lý, chấn thương, mới phẫu thuật hoặc loãng xương cần hỏi chuyên viên y tế trước khi tập.", false, "Core", "Nghiêng chậu nhẹ", "pelvic-tilt" },
                    { new Guid("00000000-0000-0000-0000-000000000028"), new DateTime(2026, 10, 22, 0, 0, 0, 0, DateTimeKind.Utc), null, 10, 60, 1, "Kiểm soát bụng sâu khi trượt một chân, không phải bài tăng tải chân. Nguồn kỹ thuật: https://www.worcsacute.nhs.uk/leaflets/core-stability-1-physiotherapy/ Nguồn nêu 5 lần mỗi chân; ứng dụng ghi 10 lần tổng cộng, một hiệp. Đây là hướng dẫn vật lý trị liệu, không chứng minh riêng cho người thừa cân. Nghỉ 60 giây là lựa chọn ứng dụng trong khoảng nghỉ ổn định 0–90 giây của NASM, không phải liều điều trị: https://www.nasm.org/resource-center/blog/training/periodization-training-simplified-a-strategic-guide. Có thể nghỉ thêm hoặc dừng nếu chưa hồi phục.", "Beginner", "Repetition", null, "1. Nằm ngửa, gối gập, đặt chân trên thảm, tìm vị trí lưng trung tính. 2. Thở ra, siết nhẹ bụng và trượt một gót ra xa chậm. 3. Hít vào, kéo chân về, đổi chân. Một lần là một chân trượt đi rồi về; 10 lần tổng = 5 mỗi bên. Giữ chậu yên; giảm tầm trượt nếu lưng cong hoặc chậu xoay. Dụng cụ: thảm, gối đầu mỏng tùy chọn. Dừng khi đau, chóng mặt, khó thở, bụng phồng bất thường hoặc không giữ được tư thế. Không nín thở hay cố tập qua đau. Người có bệnh lý, chấn thương, mới phẫu thuật hoặc loãng xương cần hỏi chuyên viên y tế trước khi tập.", true, "Core", "Trượt gót chân nằm ngửa", "supine-heel-slide" },
                    { new Guid("00000000-0000-0000-0000-000000000029"), new DateTime(2026, 10, 22, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 0, null, "Ổn định bụng/chậu khi mở một gối. Chờ duyệt liều tập cho người dùng phổ thông: nguồn ở bối cảnh phục hồi gãy chậu, không tự chuyển 10 lần mỗi chân thành mặc định. Nguồn: https://www.cuh.nhs.uk/patient-information/exercises-after-a-pelvic-fracture/, mục Single knee fallout; https://www.worcsacute.nhs.uk/leaflets/core-stability-1-physiotherapy/", "Beginner", "Repetition", null, "1. Nằm ngửa, hai gối gập, chân tựa thảm. 2. Thở ra, siết nhẹ bụng sâu. 3. Mở chậm một gối ra bên trong tầm kiểm soát; giữ chân kia và chậu yên. 4. Đưa về, thả lỏng rồi đổi bên. Không xoay chậu hoặc để bụng phồng; không dùng biến thể nhấc chân tabletop. Dụng cụ: thảm. Chưa có liều tập được duyệt. Dừng khi đau, chóng mặt, khó thở, bụng phồng bất thường hoặc không giữ được tư thế. Không nín thở hay cố tập qua đau. Người có bệnh lý, chấn thương, mới phẫu thuật hoặc loãng xương cần hỏi chuyên viên y tế trước khi tập.", false, "Core", "Mở gối sang bên có kiểm soát", "bent-knee-fallout" },
                    { new Guid("00000000-0000-0000-0000-000000000030"), new DateTime(2026, 10, 22, 0, 0, 0, 0, DateTimeKind.Utc), null, 12, 60, 1, "Giữ thân ổn định khi tay và gối tạo lực đối nhau. Nguồn: https://www.mayoclinic.org/healthy-lifestyle/fitness/in-depth/core-strength/art-20546851, Single-leg abdominal press: giữ 3 nhịp thở sâu; hướng dẫn chung 1 hiệp 12–15 lần. Ứng dụng chọn 12 lần tổng, 6 mỗi bên, không tăng theo BMI. Nghỉ 60 giây là lựa chọn ứng dụng trong khoảng nghỉ ổn định 0–90 giây của NASM, không phải liều điều trị: https://www.nasm.org/resource-center/blog/training/periodization-training-simplified-a-strategic-guide. Có thể nghỉ thêm hoặc dừng nếu chưa hồi phục.", "Beginner", "Repetition", null, "1. Nằm ngửa, gối gập, giữ lưng trung tính. 2. Siết nhẹ bụng, nâng một chân để háng/gối khoảng 90 độ. 3. Đặt tay cùng bên lên gối; đẩy tay và gối vào nhau vừa sức, giữ tay thẳng và thở sâu 3 nhịp. 4. Hạ chân, đổi bên. Một lần là một lần ép rồi thả; 12 lần tổng = 6 mỗi bên. Không nghiêng chậu, cong lưng hoặc ép tối đa. Dụng cụ: thảm. Dừng khi đau, chóng mặt, khó thở, bụng phồng bất thường hoặc không giữ được tư thế. Không nín thở hay cố tập qua đau. Người có bệnh lý, chấn thương, mới phẫu thuật hoặc loãng xương cần hỏi chuyên viên y tế trước khi tập.", true, "Core", "Ép bụng một chân", "single-leg-abdominal-press" },
                    { new Guid("00000000-0000-0000-0000-000000000031"), new DateTime(2026, 10, 22, 0, 0, 0, 0, DateTimeKind.Utc), 10, null, 60, 2, "Giữ bụng ổn định trên cẳng tay và gối. Nguồn: https://mayfieldclinic.com/pe-exercore.htm, Modified plank: giữ 10 giây, lặp 5 lần. Ứng dụng bắt đầu 2 lần giữ 10 giây, thấp hơn số lần của nguồn; không tự tăng thời gian. Nghỉ 60 giây là lựa chọn ứng dụng trong khoảng nghỉ ổn định 0–90 giây của NASM, không phải liều điều trị: https://www.nasm.org/resource-center/blog/training/periodization-training-simplified-a-strategic-guide. Có thể nghỉ thêm hoặc dừng nếu chưa hồi phục.", "Beginner", "Duration", null, "1. Nằm sấp, đặt khuỷu dưới vai, cẳng tay và gối trên thảm. 2. Siết nhẹ bụng, nâng thân, giữ đầu–vai–hông–gối thẳng. 3. Thở đều, giữ tối đa 10 giây rồi hạ nghỉ; 2 hiệp = 2 lần giữ. Không võng lưng, nhấc mông cao hoặc dồn vai gây đau. Dụng cụ: thảm, đệm gối tùy chọn. Nếu không kiểm soát được tư thế, dừng và chọn bài nằm ngửa. Dừng khi đau, chóng mặt, khó thở, bụng phồng bất thường hoặc không giữ được tư thế. Không nín thở hay cố tập qua đau. Người có bệnh lý, chấn thương, mới phẫu thuật hoặc loãng xương cần hỏi chuyên viên y tế trước khi tập.", true, "Core", "Plank chống gối", "modified-knee-plank" },
                    { new Guid("00000000-0000-0000-0000-000000000032"), new DateTime(2026, 10, 22, 0, 0, 0, 0, DateTimeKind.Utc), 10, null, 60, 2, "Ổn định bên thân trên cẳng tay và gối gập. Nguồn: https://mayfieldclinic.com/pe-exercore.htm, Side plank chống gối: giữ 10 giây, lặp 5 lần. Ứng dụng dùng 2 hiệp tổng: trái một hiệp, phải một hiệp; không phải 2 hiệp mỗi bên. Nghỉ 60 giây là lựa chọn ứng dụng trong khoảng nghỉ ổn định 0–90 giây của NASM, không phải liều điều trị: https://www.nasm.org/resource-center/blog/training/periodization-training-simplified-a-strategic-guide. Có thể nghỉ thêm hoặc dừng nếu chưa hồi phục.", "Beginner", "Duration", null, "1. Nằm nghiêng, hai gối gập, khuỷu ngay dưới vai. 2. Siết nhẹ bụng, chống cẳng tay và gối để nâng hông; giữ vai–hông–gối thẳng. 3. Hiệp 1 giữ bên trái tối đa 10 giây, hạ nghỉ. Hiệp 2 đổi bên phải, tối đa 10 giây. Không xoay thân, sụp vai hoặc nín thở. Dụng cụ: thảm, đệm gối tùy chọn. Đau vai/gối hoặc không giữ thẳng được thì dừng. Dừng khi đau, chóng mặt, khó thở, bụng phồng bất thường hoặc không giữ được tư thế. Không nín thở hay cố tập qua đau. Người có bệnh lý, chấn thương, mới phẫu thuật hoặc loãng xương cần hỏi chuyên viên y tế trước khi tập.", true, "Core", "Plank nghiêng chống gối", "modified-side-plank" },
                    { new Guid("00000000-0000-0000-0000-000000000033"), new DateTime(2026, 10, 22, 0, 0, 0, 0, DateTimeKind.Utc), null, 12, 60, 1, "Biến thể sau khi thực hiện vững ép bụng một chân; Intermediate là phân loại ứng dụng. Nguồn: https://www.mayoclinic.org/healthy-lifestyle/fitness/in-depth/core-strength/art-20546851, Double-leg abdominal press: giữ 3 nhịp thở sâu; hướng dẫn chung 1 hiệp 12–15 lần. Ứng dụng chọn 1 hiệp 12 lần, không coi cân nặng là thước đo khả năng. Nghỉ 60 giây là lựa chọn ứng dụng trong khoảng nghỉ ổn định 0–90 giây của NASM, không phải liều điều trị: https://www.nasm.org/resource-center/blog/training/periodization-training-simplified-a-strategic-guide. Có thể nghỉ thêm hoặc dừng nếu chưa hồi phục.", "Intermediate", "Repetition", null, "1. Nằm ngửa, gối gập, siết nhẹ bụng, giữ lưng trung tính. 2. Nâng từng chân để háng/gối khoảng 90 độ, đặt tay lên gối tương ứng. 3. Đẩy tay và gối đối nhau vừa sức, tay thẳng, thở sâu 3 nhịp. 4. Hạ từng chân, thả lỏng rồi lặp; một lần gồm cả hai chân. Không cong lưng, nghiêng chậu hoặc nín thở. Dụng cụ: thảm. Nếu mất kiểm soát, quay về biến thể một chân. Dừng khi đau, chóng mặt, khó thở, bụng phồng bất thường hoặc không giữ được tư thế. Không nín thở hay cố tập qua đau. Người có bệnh lý, chấn thương, mới phẫu thuật hoặc loãng xương cần hỏi chuyên viên y tế trước khi tập.", true, "Core", "Ép bụng hai chân", "double-leg-abdominal-press" }
                };
        var aliases = new[]
        {
            new[] { "pelvic-tilt", "posterior pelvic tilt", "supine pelvic tilt" },
            new[] { "supine-heel-slide", "heel slide", "heel slides", "supine heel slides" },
            new[] { "bent-knee-fallout", "bent knee fall out", "single knee fallout", "knee fallout" },
            new[] { "single-leg-abdominal-press", "single leg abdominal press", "single leg abdominal isometric press" },
            new[] { "modified-knee-plank", "modified plank", "knee plank", "kneeling plank", "forearm knee plank" },
            new[] { "modified-side-plank", "side plank on knees", "knee side plank", "kneeling side plank" },
            new[] { "double-leg-abdominal-press", "double leg abdominal press", "double leg abdominal isometric press" }
        };
        for (var row = 0; row < rows.GetLength(0); row++)
        {
            var data = Enumerable.Range(0, rows.GetLength(1)).Select(column => Literal(rows[row, column]));
            var names = aliases[row].Append((string)rows[row, 13])
                .Select(Normalize).Distinct().Select(value => Literal(value));
            var matches = string.Join(", ", names);
            migrationBuilder.Sql($"""
                INSERT INTO exercises
                    ("Id", "CreatedAt", "DefaultDurationSeconds", "DefaultReps", "DefaultRestSeconds",
                     "DefaultSets", "Description", "Difficulty", "ExerciseType", "ImageUrl",
                     "Instructions", "IsActive", "MuscleGroup", "Name", "Slug")
                SELECT {string.Join(", ", data)}
                WHERE NOT EXISTS (
                    SELECT 1 FROM exercises
                    WHERE lower(replace(replace(replace(trim("Slug"), '-', ''), '_', ''), ' ', '')) IN ({matches})
                       OR lower(replace(replace(replace(trim("Name"), '-', ''), '_', ''), ' ', '')) IN ({matches})
                       OR trim("Name") = {Literal(rows[row, 13])}
                );
                """);
        }
        // Unrelated records occupying reserved IDs fail via PK; never overwrite or reuse IDs.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Catalog entries may be referenced by history. Keep them and retain the Core-only
        // safety restriction; rollback must not resurrect unrelated exercises or delete data.
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant()
        .Replace("-", "").Replace("_", "").Replace(" ", "");

    private static string Literal(object value) => value switch
    {
        null => "NULL",
        bool active => active ? "TRUE" : "FALSE",
        DateTime date => "'" + date.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) + "'",
        Guid id => "'" + id + "'",
        string text => "'" + text.Replace("'", "''") + "'",
        _ => Convert.ToString(value, CultureInfo.InvariantCulture)!
    };
}
