using CoreUp.Domain;

namespace CoreUp.Infrastructure.Persistence;

internal static class ExerciseSeed
{
    private static readonly DateTime SeedDate = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static readonly Exercise[] All =
    [
        Reps(1, "Bodyweight Squat", "bodyweight-squat", "Legs", 12, "A controlled squat using body weight.", "Keep the chest tall and push the knees in line with the toes."),
        Reps(2, "Forward Lunge", "forward-lunge", "Legs", 10, "Alternating forward lunges for legs and balance.", "Step forward, lower both knees, then drive back."),
        Reps(3, "Glute Bridge", "glute-bridge", "Glutes", 12, "Hip extension exercise performed on the floor.", "Brace the core and squeeze the glutes at the top."),
        Reps(4, "Calf Raise", "calf-raise", "Calves", 15, "Standing calf strengthening exercise.", "Rise onto the balls of the feet and lower slowly."),
        Timed(5, "Wall Sit", "wall-sit", "Legs", 30, "Static squat hold against a wall.", "Keep knees near ninety degrees and the back supported."),
        Reps(6, "Push-up", "push-up", "Chest", 10, "Classic upper-body pushing exercise.", "Keep a straight line from head to heels."),
        Reps(7, "Knee Push-up", "knee-push-up", "Chest", 10, "Modified push-up supported on the knees.", "Brace the core and lower the chest with control."),
        Reps(8, "Incline Push-up", "incline-push-up", "Chest", 10, "Push-up using an elevated stable surface.", "Keep the body straight and bring the chest toward the surface."),
        Reps(9, "Triceps Dip", "triceps-dip", "Arms", 10, "Bodyweight dip emphasizing the triceps.", "Use a stable surface and keep shoulders away from ears."),
        Reps(10, "Superman", "superman", "Back", 12, "Floor exercise for the posterior chain.", "Lift arms and legs gently without forcing the lower back."),
        Reps(11, "Bird Dog", "bird-dog", "Core", 10, "Contralateral core stability exercise.", "Extend opposite arm and leg while keeping hips level."),
        Timed(12, "Plank", "plank", "Core", 30, "Static core bracing exercise.", "Keep ribs down and the body in a straight line."),
        Timed(13, "Side Plank", "side-plank", "Core", 25, "Lateral core stability hold.", "Stack the shoulders and keep the hips lifted."),
        Reps(14, "Crunch", "crunch", "Core", 15, "Short-range abdominal flexion exercise.", "Lift the shoulder blades without pulling the neck."),
        Reps(15, "Bicycle Crunch", "bicycle-crunch", "Core", 16, "Rotational abdominal exercise.", "Rotate the torso slowly toward the opposite knee."),
        Reps(16, "Reverse Crunch", "reverse-crunch", "Core", 12, "Lower abdominal curl performed on the floor.", "Roll the pelvis up without using momentum."),
        Reps(17, "Leg Raise", "leg-raise", "Core", 10, "Straight-leg core strengthening exercise.", "Keep the lower back controlled against the floor."),
        Timed(18, "Mountain Climber", "mountain-climber", "Cardio", 30, "Dynamic plank cardio exercise.", "Drive alternating knees forward while keeping hips stable."),
        Timed(19, "Jumping Jack", "jumping-jack", "Cardio", 30, "Full-body rhythmic cardio movement.", "Land softly and maintain a steady pace."),
        Timed(20, "High Knees", "high-knees", "Cardio", 30, "Running-in-place cardio drill.", "Lift knees comfortably and land lightly."),
        Reps(21, "Step-up", "step-up", "Legs", 10, "Step onto a stable elevated surface.", "Drive through the lead foot and control the descent."),
        Reps(22, "Burpee", "burpee", "Full Body", 8, "High-intensity full-body conditioning movement.", "Move through squat, plank and stand with controlled landings.", FitnessLevel.Intermediate),
        Reps(23, "Dead Bug", "dead-bug", "Core", 10, "Supine core stability exercise.", "Lower opposite arm and leg while keeping the back stable."),
        Reps(24, "Cat-Cow", "cat-cow", "Mobility", 10, "Gentle spinal mobility sequence.", "Alternate spinal flexion and extension with the breath."),
        Timed(25, "Child's Pose", "childs-pose", "Mobility", 30, "Resting mobility pose for back and hips.", "Sit hips toward heels and reach forward comfortably."),
        // Pending review: the study protocol is not a beginner prescription.
        new Exercise
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000026"),
            Name = "Giữ thân rỗng (Hollow Body Hold)",
            Slug = "hollow-body-hold",
            MuscleGroup = "Core",
            Difficulty = FitnessLevel.Advanced, // Conservative placeholder, not a research rating.
            ExerciseType = ExerciseType.Duration,
            DefaultSets = null,
            DefaultReps = null,
            DefaultDurationSeconds = null,
            DefaultRestSeconds = 0, // Required field; unset placeholder, not a rest prescription.
            IsActive = false,
            CreatedAt = SeedDate,
            Description = "Chờ duyệt chuyên môn. Nguồn: Thomas et al. (2017), The effects of a calisthenics training intervention on posture, strength and body composition, Isokinetics and Exercise Science 25, 215–222. DOI: 10.3233/IES-170001. Bảng 1, tr. 217; mục 2.5.4 và Hình 1f, tr. 218. Tên gốc: Hollow body position. Nghiên cứu dùng 4 hiệp × 45 giây trong chương trình phối hợp; không xác lập liều tập riêng cho người mới hoặc thừa cân. Không chứng minh giảm mỡ bụng tại chỗ.",
            Instructions = "Tư thế giữ tĩnh minh họa tại Hình 1f, tr. 218. Chưa cung cấp hướng dẫn tự tập: cần duyệt kỹ thuật, biến thể phù hợp, tiêu chí dừng, số hiệp, thời gian giữ và nghỉ trước khi kích hoạt. Advanced là phân loại tạm của ứng dụng, không phải kết luận nghiên cứu; giá trị nghỉ 0 là dữ liệu chờ duyệt, không phải khuyến nghị tập không nghỉ."
        }
    ];

    private static Exercise Reps(int id, string name, string slug, string muscle, int reps,
        string description, string instructions, FitnessLevel level = FitnessLevel.Beginner) =>
        Create(id, name, slug, muscle, ExerciseType.Repetition, 3, reps, null, 45, description, instructions, level);

    private static Exercise Timed(int id, string name, string slug, string muscle, int seconds,
        string description, string instructions, FitnessLevel level = FitnessLevel.Beginner) =>
        Create(id, name, slug, muscle, ExerciseType.Duration, 3, null, seconds, 45, description, instructions, level);

    private static Exercise Create(int id, string name, string slug, string muscle, ExerciseType type,
        int sets, int? reps, int? seconds, int rest, string description, string instructions, FitnessLevel level) => new()
        {
            Id = Guid.Parse($"00000000-0000-0000-0000-{id:D12}"), Name = name, Slug = slug,
            MuscleGroup = muscle, ExerciseType = type, Difficulty = level,
            DefaultSets = sets, DefaultReps = reps, DefaultDurationSeconds = seconds,
            DefaultRestSeconds = rest, Description = description, Instructions = instructions,
            IsActive = true, CreatedAt = SeedDate
        };
}
