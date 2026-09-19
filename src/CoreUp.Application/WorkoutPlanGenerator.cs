using CoreUp.Domain;

namespace CoreUp.Application;

public sealed record GeneratedExercise(Exercise Exercise, int Sets, int? Reps, int? DurationSeconds, int RestSeconds);

public static class WorkoutPlanGenerator
{
    public static IReadOnlyList<GeneratedExercise> Generate(UserProfile profile, IReadOnlyCollection<Exercise> exercises)
    {
        var slugs = SelectSlugs(profile);
        var count = profile.PreferredWorkoutDurationMinutes switch { <= 20 => 5, <= 35 => 6, _ => 8 };
        var sets = profile.FitnessLevel switch
        {
            FitnessLevel.Beginner => 2,
            FitnessLevel.Intermediate => 3,
            _ => 4
        };
        var duration = profile.FitnessLevel switch
        {
            FitnessLevel.Beginner => 20,
            FitnessLevel.Intermediate => 40,
            _ => 55
        };

        var bySlug = exercises.Where(x => x.IsActive).ToDictionary(x => x.Slug, StringComparer.OrdinalIgnoreCase);
        return slugs.Where(bySlug.ContainsKey).Take(count).Select(slug =>
        {
            var exercise = bySlug[slug];
            int? reps = exercise.ExerciseType == ExerciseType.Repetition
                ? profile.FitnessLevel switch
                {
                    FitnessLevel.Beginner => Math.Min(exercise.DefaultReps ?? 10, 12),
                    FitnessLevel.Intermediate => Math.Max(exercise.DefaultReps ?? 12, 12),
                    _ => Math.Max(exercise.DefaultReps ?? 15, 15)
                }
                : null;
            return new GeneratedExercise(
                exercise,
                sets,
                reps,
                exercise.ExerciseType == ExerciseType.Duration ? duration : null,
                profile.FitnessLevel == FitnessLevel.Beginner ? 45 : exercise.DefaultRestSeconds);
        }).ToList();
    }

    private static IReadOnlyList<string> SelectSlugs(UserProfile profile)
    {
        if (profile.FitnessLevel == FitnessLevel.Beginner && profile.BmiCategory is BmiCategory.Overweight or BmiCategory.Obese)
            return ["bodyweight-squat", "incline-push-up", "glute-bridge", "bird-dog", "dead-bug", "plank", "step-up", "cat-cow"];

        return profile.FitnessLevel switch
        {
            FitnessLevel.Beginner => ["bodyweight-squat", "knee-push-up", "glute-bridge", "bird-dog", "dead-bug", "plank", "step-up", "jumping-jack"],
            FitnessLevel.Intermediate => ["bodyweight-squat", "forward-lunge", "push-up", "glute-bridge", "mountain-climber", "plank", "bicycle-crunch", "burpee"],
            _ => ["bodyweight-squat", "forward-lunge", "push-up", "triceps-dip", "burpee", "mountain-climber", "side-plank", "leg-raise"]
        };
    }
}
