using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreUp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ActivateOwnerApprovedCoreExercises : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000026"),
                columns: new[] { "DefaultDurationSeconds", "DefaultRestSeconds", "DefaultSets", "Description", "Instructions", "IsActive" },
                values: new object[] { 10, 60, 2, "Chủ dự án phê duyệt ngày 22/10/2026. Bài giữ tĩnh Hollow Body Hold, không phải Hollow Rock động. Nguồn kỹ thuật: American Council on Exercise (2025), Crunch Time: An Evidence-based Approach to Training the Abs, https://www.acefitness.org/resources/pros/expert-articles/8989/crunch-time-an-evidence-based-approach-to-training-the-abs/. Thomas et al. (2017), DOI 10.3233/IES-170001, Bảng 1 tr. 217, mục 2.5.4 và Hình 1f tr. 218 dùng 4 × 45 giây trong chương trình phối hợp 8 tuần. Core-Up chọn Advanced, 2 × 10 giây, nghỉ 60 giây làm mặc định thiết kế; ACE không đưa liều này và nghiên cứu không chứng minh đây là liều tối ưu. Không chứng minh giảm mỡ bụng tại chỗ.", "1. Nằm ngửa trên thảm, gối gập; thở ra và siết nhẹ bụng để giữ lưng dưới tiếp xúc thảm. 2. Nâng đầu/vai vừa phải, duỗi chân và đưa tay qua đầu chỉ đến mức giữ lưng dưới ổn định, không kéo cổ. 3. Giữ tĩnh tối đa 10 giây, thở đều, không đung đưa; hạ xuống nghỉ 60 giây rồi lặp, tổng 2 hiệp. Lỗi: võng lưng, nín thở, rướn cổ hoặc lắc thành Hollow Rock. Giảm khó bằng co cả hai gối về tabletop, hoặc co một chân; biến thể này không tự biến record Advanced thành bài Beginner. Dừng ngay khi mất tiếp xúc lưng dưới, đau lưng/cổ, chóng mặt, khó thở hoặc bụng phồng bất thường. Không cố giữ đủ thời gian qua đau; người có bệnh lý/chấn thương hoặc mới phẫu thuật cần hỏi chuyên viên trước khi tập. Dụng cụ: thảm; không thêm tạ.", true });

            migrationBuilder.UpdateData(
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000027"),
                columns: new[] { "DefaultReps", "DefaultRestSeconds", "DefaultSets", "Description", "Instructions", "IsActive" },
                values: new object[] { 10, 60, 1, "Chủ dự án phê duyệt ngày 22/10/2026. Nguồn kỹ thuật: https://www.nhsfife.org/services/all-services/patient-advice/stage-two-exercises-for-your-tummy-muscles/, Pelvic Tilt. Nguồn nêu 3 hiệp 10 lần, 3–4 buổi/tuần trong hướng dẫn pelvic health Stage 2. Core-Up chọn khởi đầu 1 hiệp 10 lần; đây là thiết kế ứng dụng, không phải liều tối ưu hoặc chứng nhận an toàn phổ quát. Nghỉ 60 giây là lựa chọn ứng dụng trong khoảng nghỉ ổn định 0–90 giây của NASM, không phải liều điều trị: https://www.nasm.org/resource-center/blog/training/periodization-training-simplified-a-strategic-guide. Có thể nghỉ thêm hoặc dừng nếu chưa hồi phục.", "1. Nằm ngửa trên thảm, gối gập, bàn chân tựa sàn. 2. Thở ra chậm, nghiêng chậu nhẹ để lưng dưới phẳng về thảm, không ép mạnh. 3. Hít vào, nghiêng trở lại tạo khoảng cong nhỏ; một chu kỳ đi–về tính một lần. Làm 10 lần trong một hiệp. Không nhấc mông thành bridge, giật thân, ép biên độ hoặc nín thở. Giảm biên độ khi khó kiểm soát. Dụng cụ: thảm; gối đầu mỏng tùy chọn. Dừng nếu đau tăng, bụng phồng dạng chóp, són tiểu hoặc cảm giác nặng/sa vùng chậu; hỏi chuyên viên nếu triệu chứng kéo dài. Dừng khi đau, chóng mặt, khó thở, bụng phồng bất thường hoặc không giữ được tư thế. Không nín thở hay cố tập qua đau. Người có bệnh lý, chấn thương, mới phẫu thuật hoặc loãng xương cần hỏi chuyên viên y tế trước khi tập.", true });

            migrationBuilder.UpdateData(
                table: "exercises",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000029"),
                columns: new[] { "DefaultReps", "DefaultRestSeconds", "DefaultSets", "Description", "Instructions", "IsActive" },
                values: new object[] { 10, 60, 1, "Chủ dự án phê duyệt ngày 22/10/2026. Nguồn: https://www.nhsfife.org/services/all-services/patient-advice/stage-two-exercises-for-your-tummy-muscles/, Bent Knee Fall Outs. Nguồn nêu 3 hiệp 10 lần, tập hai bên, 3–4 buổi/tuần; không làm rõ tổng hay mỗi bên. Core-Up quy định 1 hiệp 10 lần tổng = 5 mỗi bên, thiết kế khởi đầu bảo thủ, không phải liều lâm sàng tối ưu. Nghỉ 60 giây là lựa chọn ứng dụng trong khoảng nghỉ ổn định 0–90 giây của NASM, không phải liều điều trị: https://www.nasm.org/resource-center/blog/training/periodization-training-simplified-a-strategic-guide. Có thể nghỉ thêm hoặc dừng nếu chưa hồi phục.", "1. Nằm ngửa, hai gối gập, bàn chân gần nhau và luôn tựa thảm. 2. Thở ra, siết nhẹ bụng; mở chậm một gối sang bên trong tầm kiểm soát, giữ chậu và chân kia yên. 3. Hít vào, đưa gối về giữa và thả lỏng, rồi đổi bên. Một lượt mở–về một chân tính một lần; 10 lần tổng = 5 mỗi bên. Không xoay chậu, để bụng phồng, ép gối sát sàn hoặc nâng chân tabletop. Giảm tầm mở khi mất kiểm soát. Dụng cụ: thảm. Dừng nếu đau tăng, són tiểu, phồng bụng dạng chóp hoặc nặng/sa vùng chậu; hỏi chuyên viên khi cần. Dừng khi đau, chóng mặt, khó thở, bụng phồng bất thường hoặc không giữ được tư thế. Không nín thở hay cố tập qua đau. Người có bệnh lý, chấn thương, mới phẫu thuật hoặc loãng xương cần hỏi chuyên viên y tế trước khi tập.", true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Preserve owner approval and catalog data; use a reviewed forward migration to change it.
        }
    }
}
