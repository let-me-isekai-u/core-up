using System.Text.Json;
using CoreUp.Domain;

namespace CoreUp.Application;

/// Application classification v1, not a validated clinical assessment.
/// Answers are ordered exactly as the seven Vietnamese questionnaire prompts.
public static class FitnessAssessmentScoring
{
    public const int Version = 1;
    public static FitnessAssessmentResponse Evaluate(IReadOnlyList<int> answers, FitnessLevel? selected = null)
    {
        if (answers is null || answers.Count != 7 || answers.Any(x => x is < 0 or > 2))
            throw new AppException(400, "Vui lòng trả lời đủ 7 câu hỏi bằng các lựa chọn có sẵn.");
        // Q1–Q5: 0/1/2 points each; Q6 is the goal, Q7 is a safety flag, not points.
        var score = answers.Take(5).Sum();
        var level = score >= 8 && answers[0] == 2 && answers[2] == 2 && answers[3] == 2 && answers[4] == 2
            ? FitnessLevel.Advanced
            : score >= 4 ? FitnessLevel.Intermediate : FitnessLevel.Beginner;
        var limited = answers[0] == 0 || answers[2] == 0 || answers[3] == 0 || answers[4] == 0;
        if (limited || answers[6] != 0) level = FitnessLevel.Beginner;
        var explanation = $"Điểm khả năng: {score}/10 từ 5 câu đầu (mỗi câu 0–2 điểm). " +
            "0–3: Dễ; 4–7: Vừa; 8–10 chỉ gợi ý Khó khi đã tập thường xuyên, thành thạo động tác, giữ Plank trên 20 giây và kiểm soát bài khó. " +
            "Nếu chưa từng tập, chưa quen động tác, chưa giữ được Plank, nhanh mệt hoặc có/không chắc về hạn chế vận động, gợi ý Dễ. Nếu chưa đạt đủ điều kiện Khó thì giữ Vừa. Mục tiêu không cộng điểm.";
        var notes = new List<string>
        {
            "Đây là gợi ý theo thiết kế ứng dụng, không phải chẩn đoán hay đánh giá y khoa đã kiểm định. Điểm cao không chứng minh bài tập an toàn cho bạn.",
            "Dừng khi đau, chóng mặt, khó thở hoặc mất kiểm soát tư thế; không cố hoàn thành số lần/thời gian."
        };
        if (answers[6] != 0)
            notes.Insert(0, "Bạn báo có hoặc chưa chắc về đau/chấn thương/hạn chế vận động. Hãy hỏi chuyên viên y tế trước khi bắt đầu hoặc tăng mức; có thể để sau và chưa tập.");
        return new(level, explanation, notes, selected, answers.ToArray(), score, Version);
    }

    public static FitnessAssessmentResponse? FromProfile(UserProfile profile) =>
        profile.QuestionnaireVersion != Version || profile.QuestionnaireCompletedAt is null || profile.QuestionnaireAnswers is null ? null :
        Evaluate(JsonSerializer.Deserialize<int[]>(profile.QuestionnaireAnswers)!, profile.FitnessLevel);
}
