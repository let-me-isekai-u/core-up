using CoreUp.Application;
using CoreUp.Domain;

namespace CoreUp.Tests;

public sealed class WorkoutPlanGeneratorTests
{
    [Fact]
    public void BeginnerOverweightPlan_DoesNotIncludeBurpee()
    {
        var profile = new UserProfile
        {
            FitnessLevel = FitnessLevel.Beginner,
            Goal = FitnessGoal.LoseWeight,
            BmiCategory = BmiCategory.Overweight,
            PreferredWorkoutDurationMinutes = 30
        };
        var slugs = new[]
        {
            "bodyweight-squat", "incline-push-up", "glute-bridge", "bird-dog",
            "dead-bug", "plank", "step-up", "cat-cow", "burpee"
        };
        var exercises = slugs.Select((slug, index) => new Exercise
        {
            Id = Guid.NewGuid(), Name = slug, Slug = slug, IsActive = true,
            ExerciseType = ExerciseType.Repetition, DefaultReps = 10,
            DefaultSets = 3, DefaultRestSeconds = 45
        }).ToList();

        var result = WorkoutPlanGenerator.Generate(profile, exercises);

        Assert.NotEmpty(result);
        Assert.DoesNotContain(result, x => x.Exercise.Slug == "burpee");
        Assert.All(result, x => Assert.Equal(2, x.Sets));
    }
}
