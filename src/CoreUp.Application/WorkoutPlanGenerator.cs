using CoreUp.Domain;

namespace CoreUp.Application;

public sealed record GeneratedExercise(Exercise Exercise, int Sets, int? Reps, int? DurationSeconds, int RestSeconds);

/// Free standard templates v1. Only the explicitly selected level controls selection.
/// Doses come from catalog defaults; measurements, goal, sex and preferred minutes do not.
public static class WorkoutPlanGenerator
{
    public const string Version = "standard-core-v1";

    private static string[] Slugs(FitnessLevel level) => level switch
    {
        FitnessLevel.Beginner => ["pelvic-tilt", "supine-heel-slide", "bent-knee-fallout", "modified-knee-plank", "bird-dog", "dead-bug"],
        FitnessLevel.Intermediate => ["supine-heel-slide", "single-leg-abdominal-press", "dead-bug", "modified-side-plank", "double-leg-abdominal-press", "plank"],
        FitnessLevel.Advanced => ["bird-dog", "reverse-crunch", "leg-raise", "side-plank", "double-leg-abdominal-press", "hollow-body-hold"],
        _ => throw new AppException(400, "Mức tập không hợp lệ.")
    };

    // Retains the existing method signature for callers; only FitnessLevel is used.
    public static IReadOnlyList<GeneratedExercise> Generate(UserProfile profile, IReadOnlyCollection<Exercise> exercises)
    {
        var selected = new List<GeneratedExercise>();
        foreach (var slug in Slugs(profile.FitnessLevel))
        {
            var matches = exercises.Where(x => string.Equals(x.Slug, slug, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count != 1 || !Eligible(matches[0], profile.FitnessLevel))
                throw new AppException(422, $"Giáo trình chuẩn chưa sẵn sàng: kiểm tra bài {slug} trong catalog. Không tự thay bài hoặc tăng liều.");
            var exercise = matches[0];
            selected.Add(new(exercise, exercise.DefaultSets!.Value, exercise.DefaultReps,
                exercise.DefaultDurationSeconds, exercise.DefaultRestSeconds));
        }
        return selected;
    }

    private static bool Eligible(Exercise exercise, FitnessLevel level) =>
        exercise.IsActive && exercise.MuscleGroup == "Core" &&
        Enum.IsDefined(exercise.Difficulty) && exercise.Difficulty <= level &&
        !string.IsNullOrWhiteSpace(exercise.Instructions) &&
        exercise.DefaultSets is > 0 and <= 100 && exercise.DefaultRestSeconds >= 0 &&
        (exercise.ExerciseType == ExerciseType.Duration
            ? exercise.DefaultDurationSeconds is > 0 and <= 86400 && exercise.DefaultReps is null
            : exercise.ExerciseType == ExerciseType.Repetition && exercise.DefaultReps is > 0 and <= 10000 && exercise.DefaultDurationSeconds is null);

    public static int EstimateMinutes(IReadOnlyList<GeneratedExercise> exercises)
    {
        // Display estimate only; the repetition player never pretends to count reps.
        var seconds = exercises.Sum(x => x.Sets *
            ((x.DurationSeconds ?? x.Reps!.Value * 6) + x.RestSeconds));
        return Math.Max(1, (int)Math.Ceiling(seconds / 60d));
    }
}
