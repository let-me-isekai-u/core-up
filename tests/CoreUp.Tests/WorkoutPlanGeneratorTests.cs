using CoreUp.Application;
using CoreUp.Domain;
using CoreUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CoreUp.Tests;

public sealed class WorkoutPlanGeneratorTests
{
    [Theory]
    [InlineData(FitnessLevel.Beginner)]
    [InlineData(FitnessLevel.Intermediate)]
    [InlineData(FitnessLevel.Advanced)]
    public async Task AllLevelsAndBmiCategories_UseOnlyReviewedCoreAndPreserveDose(FitnessLevel level)
    {
        await using var db = await CatalogTestDatabase.Create();
        var exercises = await db.Exercises.AsNoTracking().ToListAsync();
        // Poison legacy flags: defense must work even without the catalog migration.
        foreach (var item in exercises.Where(x => x.MuscleGroup != "Core" || x.Slug == "hollow-body-hold"))
            item.IsActive = true;
        foreach (var bmi in Enum.GetValues<BmiCategory>())
        foreach (var goal in Enum.GetValues<FitnessGoal>())
        foreach (var minutes in new[] { 10, 20, 30, 180 })
        {
            var result = WorkoutPlanGenerator.Generate(Profile(level, bmi, minutes, goal), exercises);
            Assert.Equal(minutes <= 20 ? 3 : 4, result.Count);
            Assert.Equal(result.Count, result.Select(x => x.Exercise.Id).Distinct().Count());
            Assert.DoesNotContain(result, x => x.Exercise.Slug == "hollow-body-hold");
            Assert.All(result, item =>
            {
                Assert.Equal("Core", item.Exercise.MuscleGroup);
                Assert.True(item.Exercise.IsActive);
                Assert.True(item.Exercise.Difficulty <= level);
                Assert.Equal(item.Exercise.DefaultSets, item.Sets);
                Assert.Equal(item.Exercise.DefaultReps, item.Reps);
                Assert.Equal(item.Exercise.DefaultDurationSeconds, item.DurationSeconds);
                Assert.InRange(item.RestSeconds, 60, 90);
                if (item.Exercise.ExerciseType == ExerciseType.Duration)
                {
                    Assert.Null(item.Reps);
                    Assert.InRange(item.DurationSeconds!.Value, 1, 10);
                }
                else { Assert.Null(item.DurationSeconds); Assert.InRange(item.Reps!.Value, 1, 12); }
            });
            Assert.Single(result, x => x.Exercise.Slug.Contains("abdominal-press"));
            Assert.Contains(result, x => x.Exercise.Slug == (level == FitnessLevel.Beginner
                ? "single-leg-abdominal-press" : "double-leg-abdominal-press"));
        }
    }

    [Fact]
    public async Task InactiveProgression_RegressesWithoutIncreasingDose()
    {
        await using var db = await CatalogTestDatabase.Create();
        var exercises = await db.Exercises.ToListAsync();
        exercises.Single(x => x.Slug == "double-leg-abdominal-press").IsActive = false;
        var result = WorkoutPlanGenerator.Generate(Profile(FitnessLevel.Advanced), exercises);
        Assert.Equal(4, result.Count);
        Assert.Contains(result, x => x.Exercise.Slug == "single-leg-abdominal-press");
        Assert.DoesNotContain(result, x => !x.Exercise.IsActive);
    }

    [Fact]
    public async Task OneMissingMovement_UsesThreeAvailable_WithoutRepeatingToFillTime()
    {
        await using var db = await CatalogTestDatabase.Create();
        var exercises = await db.Exercises.Where(x => x.Slug != "supine-heel-slide").ToListAsync();
        Assert.Equal(3, WorkoutPlanGenerator.Generate(Profile(FitnessLevel.Beginner), exercises).Count);
    }

    [Fact]
    public async Task InsufficientData_ThrowsActionableError()
    {
        await using var db = await CatalogTestDatabase.Create();
        var exercises = await db.Exercises.Where(x => x.ExerciseType == ExerciseType.Duration).ToListAsync();
        var error = Assert.Throws<AppException>(() => WorkoutPlanGenerator.Generate(Profile(FitnessLevel.Beginner), exercises));
        Assert.Equal(422, error.StatusCode);
        Assert.Contains("reviewed", error.Message);
    }

    [Theory]
    [InlineData("dose")]
    [InlineData("unbalanced")]
    [InlineData("incompleteholds")]
    [InlineData("inactive")]
    [InlineData("noncore")]
    [InlineData("wrongtype")]
    [InlineData("missinginstructions")]
    [InlineData("ambiguity")]
    public async Task UnsafeOrAmbiguousRecord_IsExcluded(string fault)
    {
        await using var db = await CatalogTestDatabase.Create();
        var exercises = await db.Exercises.ToListAsync();
        var item = exercises.Single(x => x.Slug == (fault == "incompleteholds" ? "modified-side-plank" : "supine-heel-slide"));
        switch (fault)
        {
            case "dose": item.DefaultReps = 1000; break;
            case "unbalanced": item.DefaultReps = 7; break;
            case "incompleteholds": item.DefaultSets = 1; break;
            case "inactive": item.IsActive = false; break;
            case "noncore": item.MuscleGroup = "Legs"; break;
            case "wrongtype": item.ExerciseType = ExerciseType.Duration; break;
            case "missinginstructions": item.Instructions = null; break;
            case "ambiguity": exercises.Add(new Exercise
            {
                Id = Guid.NewGuid(), Slug = item.Slug.ToUpperInvariant(), IsActive = true, MuscleGroup = "Core",
                Difficulty = item.Difficulty, ExerciseType = item.ExerciseType, DefaultSets = 1, DefaultReps = 10,
                DefaultRestSeconds = 60, Description = item.Description, Instructions = item.Instructions
            }); break;
        }
        var result = WorkoutPlanGenerator.Generate(Profile(FitnessLevel.Beginner), exercises);
        Assert.Equal(3, result.Count);
        Assert.DoesNotContain(result, x => x.Exercise.Id == item.Id);
    }

    [Fact]
    public void InvalidLevelAndEmptyCatalog_FailExplicitly()
    {
        Assert.Equal(400, Assert.Throws<AppException>(() => WorkoutPlanGenerator.Generate(Profile((FitnessLevel)99), [])).StatusCode);
        Assert.Equal(422, Assert.Throws<AppException>(() => WorkoutPlanGenerator.Generate(Profile(FitnessLevel.Beginner), [])).StatusCode);
    }

    private static UserProfile Profile(FitnessLevel level, BmiCategory bmi = BmiCategory.Obese,
        int minutes = 30, FitnessGoal goal = FitnessGoal.LoseWeight) => new()
        { FitnessLevel = level, BmiCategory = bmi, PreferredWorkoutDurationMinutes = minutes, Goal = goal };
}

internal static class CatalogTestDatabase
{
    public static async Task<CoreUpDbContext> Create()
    {
        var db = new CoreUpDbContext(new DbContextOptionsBuilder<CoreUpDbContext>().UseSqlite("Data Source=:memory:").Options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();
        return db;
    }
}
