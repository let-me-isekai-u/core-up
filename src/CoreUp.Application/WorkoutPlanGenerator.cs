using CoreUp.Domain;

namespace CoreUp.Application;

public sealed record GeneratedExercise(Exercise Exercise, int Sets, int? Reps, int? DurationSeconds, int RestSeconds);

public static class WorkoutPlanGenerator
{
    // Explicit prescription review boundary. Legacy Core labels and IsActive alone do not
    // approve automatic prescribing. Add a progression only after its technique/dose review.
    private static readonly IReadOnlyDictionary<string, (FitnessLevel Level, ExerciseType Type, int Sets, int Dose)> Reviewed =
        new Dictionary<string, (FitnessLevel, ExerciseType, int, int)>(StringComparer.OrdinalIgnoreCase)
        {
            ["supine-heel-slide"] = (FitnessLevel.Beginner, ExerciseType.Repetition, 1, 10),
            ["single-leg-abdominal-press"] = (FitnessLevel.Beginner, ExerciseType.Repetition, 1, 12),
            ["modified-knee-plank"] = (FitnessLevel.Beginner, ExerciseType.Duration, 2, 10),
            ["modified-side-plank"] = (FitnessLevel.Beginner, ExerciseType.Duration, 2, 10),
            ["double-leg-abdominal-press"] = (FitnessLevel.Intermediate, ExerciseType.Repetition, 1, 12)
        };

    public static IReadOnlyList<GeneratedExercise> Generate(UserProfile profile, IReadOnlyCollection<Exercise> exercises)
    {
        if (!Enum.IsDefined(profile.FitnessLevel) || profile.PreferredWorkoutDurationMinutes <= 0)
            throw new AppException(400, "A valid fitness level and workout duration are required.");

        var eligible = exercises.Where(x => IsEligible(x, profile.FitnessLevel))
            .GroupBy(x => x.Slug, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() == 1) // Ambiguous catalog data is never arbitrarily prescribed.
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.OrdinalIgnoreCase);

        // Regress to single-leg if the double-leg progression is unavailable. BMI neither
        // establishes ability nor overrides the selected level. Beginners always start gently.
        string[][] movements =
        [
            ["supine-heel-slide"],
            profile.FitnessLevel == FitnessLevel.Beginner
                ? ["single-leg-abdominal-press"]
                : ["double-leg-abdominal-press", "single-leg-abdominal-press"],
            ["modified-knee-plank"],
            ["modified-side-plank"]
        ];
        var target = profile.PreferredWorkoutDurationMinutes <= 20 ? 3 : 4;
        var selected = movements.Select(group => group.FirstOrDefault(eligible.ContainsKey))
            .OfType<string>().Take(target).Select(slug => eligible[slug]).ToList();
        if (selected.Count < 3)
            throw new AppException(422, "Not enough reviewed active Core exercises. At least three distinct eligible movements are required; review or restore the catalog before generating a plan.");

        // Use reviewed catalog defaults; never multiply volume or hold time by user level,
        // goal, BMI, or requested minutes. Advanced currently uses available lower-level work.
        return selected.Select(exercise => new GeneratedExercise(exercise,
            exercise.DefaultSets!.Value, exercise.DefaultReps,
            exercise.DefaultDurationSeconds, exercise.DefaultRestSeconds)).ToList();
    }

    public static int EstimateMinutes(IReadOnlyList<GeneratedExercise> exercises)
    {
        // Approximate transitions and breathing cadence only; repetitions remain repetitions.
        var seconds = exercises.Sum(x => x.Sets *
            ((x.DurationSeconds ?? x.Reps!.Value * (x.Exercise.Slug == "supine-heel-slide" ? 6 : 15)) + x.RestSeconds));
        return Math.Max(1, (int)Math.Ceiling(seconds / 60d));
    }

    private static bool IsEligible(Exercise exercise, FitnessLevel level)
    {
        if (!exercise.IsActive || exercise.MuscleGroup != "Core" ||
            string.IsNullOrWhiteSpace(exercise.Description) || string.IsNullOrWhiteSpace(exercise.Instructions) ||
            !Reviewed.TryGetValue(exercise.Slug, out var review)) return false;
        if (exercise.Difficulty != review.Level || exercise.Difficulty > level || exercise.ExerciseType != review.Type ||
            exercise.DefaultSets != review.Sets ||
            exercise.DefaultRestSeconds is < 60 or > 90) return false;
        // Exact doses keep alternating-side counts and the two-side plank instructions coherent.
        return review.Type == ExerciseType.Repetition
            ? exercise.DefaultReps == review.Dose && exercise.DefaultDurationSeconds is null
            : exercise.DefaultDurationSeconds == review.Dose && exercise.DefaultReps is null;
    }
}
