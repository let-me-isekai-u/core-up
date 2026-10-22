using CoreUp.Domain;

namespace CoreUp.Infrastructure.Persistence;

// Technique references are not evidence of universal safety or long-term outcomes.
// Review date: 2026-10-22. Defaults are conservative application choices, not clinical orders.
internal static class CoreExerciseSeed
{
    private const string Mayo = "https://www.mayoclinic.org/healthy-lifestyle/fitness/in-depth/core-strength/art-20546851";
    private const string Mayfield = "https://mayfieldclinic.com/pe-exercore.htm";
    private const string Nhs = "https://www.worcsacute.nhs.uk/leaflets/core-stability-1-physiotherapy/";
    private const string RestPolicy = " Nghỉ 60 giây là lựa chọn ứng dụng trong khoảng nghỉ ổn định 0–90 giây của NASM, không phải liều điều trị: https://www.nasm.org/resource-center/blog/training/periodization-training-simplified-a-strategic-guide. Có thể nghỉ thêm hoặc dừng nếu chưa hồi phục.";
    private const string Safety = " Dừng khi đau, chóng mặt, khó thở, bụng phồng bất thường hoặc không giữ được tư thế. Không nín thở hay cố tập qua đau. Người có bệnh lý, chấn thương, mới phẫu thuật hoặc loãng xương cần hỏi chuyên viên y tế trước khi tập.";

    public static readonly Exercise[] All =
    [
        Create(27, "Nghiêng chậu nhẹ", "pelvic-tilt", FitnessLevel.Beginner, ExerciseType.Repetition,
            1, 10, null, true,
            "Chủ dự án phê duyệt ngày 22/10/2026. Nguồn kỹ thuật: https://www.nhsfife.org/services/all-services/patient-advice/stage-two-exercises-for-your-tummy-muscles/, Pelvic Tilt. Nguồn nêu 3 hiệp 10 lần, 3–4 buổi/tuần trong hướng dẫn pelvic health Stage 2. Core-Up chọn khởi đầu 1 hiệp 10 lần; đây là thiết kế ứng dụng, không phải liều tối ưu hoặc chứng nhận an toàn phổ quát." + RestPolicy,
            "1. Nằm ngửa trên thảm, gối gập, bàn chân tựa sàn. 2. Thở ra chậm, nghiêng chậu nhẹ để lưng dưới phẳng về thảm, không ép mạnh. 3. Hít vào, nghiêng trở lại tạo khoảng cong nhỏ; một chu kỳ đi–về tính một lần. Làm 10 lần trong một hiệp. Không nhấc mông thành bridge, giật thân, ép biên độ hoặc nín thở. Giảm biên độ khi khó kiểm soát. Dụng cụ: thảm; gối đầu mỏng tùy chọn. Dừng nếu đau tăng, bụng phồng dạng chóp, són tiểu hoặc cảm giác nặng/sa vùng chậu; hỏi chuyên viên nếu triệu chứng kéo dài."),
        Create(28, "Trượt gót chân nằm ngửa", "supine-heel-slide", FitnessLevel.Beginner, ExerciseType.Repetition,
            1, 10, null, true,
            "Kiểm soát bụng sâu khi trượt một chân, không phải bài tăng tải chân. Nguồn kỹ thuật: " + Nhs + " Nguồn nêu 5 lần mỗi chân; ứng dụng ghi 10 lần tổng cộng, một hiệp. Đây là hướng dẫn vật lý trị liệu, không chứng minh riêng cho người thừa cân." + RestPolicy,
            "1. Nằm ngửa, gối gập, đặt chân trên thảm, tìm vị trí lưng trung tính. 2. Thở ra, siết nhẹ bụng và trượt một gót ra xa chậm. 3. Hít vào, kéo chân về, đổi chân. Một lần là một chân trượt đi rồi về; 10 lần tổng = 5 mỗi bên. Giữ chậu yên; giảm tầm trượt nếu lưng cong hoặc chậu xoay. Dụng cụ: thảm, gối đầu mỏng tùy chọn."),
        Create(29, "Mở gối sang bên có kiểm soát", "bent-knee-fallout", FitnessLevel.Beginner, ExerciseType.Repetition,
            1, 10, null, true,
            "Chủ dự án phê duyệt ngày 22/10/2026. Nguồn: https://www.nhsfife.org/services/all-services/patient-advice/stage-two-exercises-for-your-tummy-muscles/, Bent Knee Fall Outs. Nguồn nêu 3 hiệp 10 lần, tập hai bên, 3–4 buổi/tuần; không làm rõ tổng hay mỗi bên. Core-Up quy định 1 hiệp 10 lần tổng = 5 mỗi bên, thiết kế khởi đầu bảo thủ, không phải liều lâm sàng tối ưu." + RestPolicy,
            "1. Nằm ngửa, hai gối gập, bàn chân gần nhau và luôn tựa thảm. 2. Thở ra, siết nhẹ bụng; mở chậm một gối sang bên trong tầm kiểm soát, giữ chậu và chân kia yên. 3. Hít vào, đưa gối về giữa và thả lỏng, rồi đổi bên. Một lượt mở–về một chân tính một lần; 10 lần tổng = 5 mỗi bên. Không xoay chậu, để bụng phồng, ép gối sát sàn hoặc nâng chân tabletop. Giảm tầm mở khi mất kiểm soát. Dụng cụ: thảm. Dừng nếu đau tăng, són tiểu, phồng bụng dạng chóp hoặc nặng/sa vùng chậu; hỏi chuyên viên khi cần."),
        Create(30, "Ép bụng một chân", "single-leg-abdominal-press", FitnessLevel.Beginner, ExerciseType.Repetition,
            1, 12, null, true,
            "Giữ thân ổn định khi tay và gối tạo lực đối nhau. Nguồn: " + Mayo + ", Single-leg abdominal press: giữ 3 nhịp thở sâu; hướng dẫn chung 1 hiệp 12–15 lần. Ứng dụng chọn 12 lần tổng, 6 mỗi bên, không tăng theo BMI." + RestPolicy,
            "1. Nằm ngửa, gối gập, giữ lưng trung tính. 2. Siết nhẹ bụng, nâng một chân để háng/gối khoảng 90 độ. 3. Đặt tay cùng bên lên gối; đẩy tay và gối vào nhau vừa sức, giữ tay thẳng và thở sâu 3 nhịp. 4. Hạ chân, đổi bên. Một lần là một lần ép rồi thả; 12 lần tổng = 6 mỗi bên. Không nghiêng chậu, cong lưng hoặc ép tối đa. Dụng cụ: thảm."),
        Create(31, "Plank chống gối", "modified-knee-plank", FitnessLevel.Beginner, ExerciseType.Duration,
            2, null, 10, true,
            "Giữ bụng ổn định trên cẳng tay và gối. Nguồn: " + Mayfield + ", Modified plank: giữ 10 giây, lặp 5 lần. Ứng dụng bắt đầu 2 lần giữ 10 giây, thấp hơn số lần của nguồn; không tự tăng thời gian." + RestPolicy,
            "1. Nằm sấp, đặt khuỷu dưới vai, cẳng tay và gối trên thảm. 2. Siết nhẹ bụng, nâng thân, giữ đầu–vai–hông–gối thẳng. 3. Thở đều, giữ tối đa 10 giây rồi hạ nghỉ; 2 hiệp = 2 lần giữ. Không võng lưng, nhấc mông cao hoặc dồn vai gây đau. Dụng cụ: thảm, đệm gối tùy chọn. Nếu không kiểm soát được tư thế, dừng và chọn bài nằm ngửa."),
        Create(32, "Plank nghiêng chống gối", "modified-side-plank", FitnessLevel.Beginner, ExerciseType.Duration,
            2, null, 10, true,
            "Ổn định bên thân trên cẳng tay và gối gập. Nguồn: " + Mayfield + ", Side plank chống gối: giữ 10 giây, lặp 5 lần. Ứng dụng dùng 2 hiệp tổng: trái một hiệp, phải một hiệp; không phải 2 hiệp mỗi bên." + RestPolicy,
            "1. Nằm nghiêng, hai gối gập, khuỷu ngay dưới vai. 2. Siết nhẹ bụng, chống cẳng tay và gối để nâng hông; giữ vai–hông–gối thẳng. 3. Hiệp 1 giữ bên trái tối đa 10 giây, hạ nghỉ. Hiệp 2 đổi bên phải, tối đa 10 giây. Không xoay thân, sụp vai hoặc nín thở. Dụng cụ: thảm, đệm gối tùy chọn. Đau vai/gối hoặc không giữ thẳng được thì dừng."),
        Create(33, "Ép bụng hai chân", "double-leg-abdominal-press", FitnessLevel.Intermediate, ExerciseType.Repetition,
            1, 12, null, true,
            "Biến thể sau khi thực hiện vững ép bụng một chân; Intermediate là phân loại ứng dụng. Nguồn: " + Mayo + ", Double-leg abdominal press: giữ 3 nhịp thở sâu; hướng dẫn chung 1 hiệp 12–15 lần. Ứng dụng chọn 1 hiệp 12 lần, không coi cân nặng là thước đo khả năng." + RestPolicy,
            "1. Nằm ngửa, gối gập, siết nhẹ bụng, giữ lưng trung tính. 2. Nâng từng chân để háng/gối khoảng 90 độ, đặt tay lên gối tương ứng. 3. Đẩy tay và gối đối nhau vừa sức, tay thẳng, thở sâu 3 nhịp. 4. Hạ từng chân, thả lỏng rồi lặp; một lần gồm cả hai chân. Không cong lưng, nghiêng chậu hoặc nín thở. Dụng cụ: thảm. Nếu mất kiểm soát, quay về biến thể một chân.")
    ];

    private static Exercise Create(int id, string name, string slug, FitnessLevel difficulty,
        ExerciseType type, int? sets, int? reps, int? duration, bool active,
        string description, string instructions) => new()
        {
            Id = Guid.Parse($"00000000-0000-0000-0000-{id:D12}"),
            Name = name, Slug = slug, MuscleGroup = "Core", Difficulty = difficulty,
            ExerciseType = type, DefaultSets = sets, DefaultReps = reps,
            DefaultDurationSeconds = duration, DefaultRestSeconds = active ? 60 : 0,
            Description = description, Instructions = instructions + Safety, IsActive = active,
            CreatedAt = new DateTime(2026, 10, 22, 0, 0, 0, DateTimeKind.Utc)
        };
}
