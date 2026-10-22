using System.Text.Json;
using CoreUp.Application;
using CoreUp.Domain;
using CoreUp.Infrastructure.Persistence;
using CoreUp.Infrastructure.Persistence.Migrations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace CoreUp.Tests;

public sealed class ExerciseImportTests
{
    private static readonly Guid ImportedId = Guid.Parse("00000000-0000-0000-0000-000000000026");

    [Fact]
    public async Task ImportTwice_AddsOnlyPendingCoreExercise_AndPreservesCatalog()
    {
        await using var db = await CreateDatabase();
        var before = await Catalog(db);
        Assert.Equal(25, await db.Exercises.CountAsync());

        await Import(db);
        await Import(db);

        Assert.Equal(26, await db.Exercises.CountAsync());
        Assert.Equal(before, await Catalog(db));
        var exercise = await db.Exercises.AsNoTracking().SingleAsync(x => x.Id == ImportedId);
        Assert.Equal("Giữ thân rỗng (Hollow Body Hold)", exercise.Name);
        Assert.Equal("hollow-body-hold", exercise.Slug);
        Assert.Equal("Core", exercise.MuscleGroup);
        Assert.Equal(ExerciseType.Duration, exercise.ExerciseType);
        Assert.Equal(FitnessLevel.Advanced, exercise.Difficulty);
        Assert.False(exercise.IsActive);
        Assert.Null(exercise.DefaultSets);
        Assert.Null(exercise.DefaultReps);
        Assert.Null(exercise.DefaultDurationSeconds);
        Assert.Equal(0, exercise.DefaultRestSeconds);
        Assert.Contains("10.3233/IES-170001", exercise.Description);
        Assert.Contains("217", exercise.Description);
        Assert.Contains("218", exercise.Description);
    }

    [Theory]
    [InlineData("hollow-body-hold", "Existing custom name")]
    [InlineData(" HOLLOW_BODY_POSITION ", "Existing custom name")]
    [InlineData("hollow-hold", "Existing custom name")]
    [InlineData("custom-slug", "Hollow Body Hold")]
    [InlineData("custom-slug", " HOLLOW BODY POSITION ")]
    [InlineData("custom-slug", "Hollow-Body-Hold")]
    [InlineData("custom-slug", "Hollow Hold")]
    [InlineData("custom-slug", "Hollow Body Position / Hollow Body Hold")]
    [InlineData("custom-slug", "Giữ thân rỗng (Hollow Body Hold)")]
    public async Task ExistingAlias_PreservesIdFieldsAndHistory(string slug, string name)
    {
        await using var db = await CreateDatabase();
        var existing = new Exercise
        {
            Id = Guid.NewGuid(), Name = name, Slug = slug, MuscleGroup = "Core",
            IsActive = true, ExerciseType = ExerciseType.Duration,
            DefaultSets = 2, DefaultDurationSeconds = 12, DefaultRestSeconds = 60,
            Description = "User-managed record", CreatedAt = DateTime.UtcNow
        };
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.invalid", PasswordHash = "test" };
        var session = new WorkoutSession
        {
            Id = Guid.NewGuid(), User = user, Status = WorkoutSessionStatus.Completed,
            Exercises = [new WorkoutSessionExercise
            {
                Id = Guid.NewGuid(), Exercise = existing, TargetSets = 2,
                TargetDurationSeconds = 12, CompletedSets = 2, IsCompleted = true
            }]
        };
        db.WorkoutSessions.Add(session);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var before = await Catalog(db);
        var history = JsonSerializer.Serialize(await db.WorkoutSessionExercises.AsNoTracking().ToListAsync());

        await Import(db);
        await Import(db);

        Assert.Equal(26, await db.Exercises.CountAsync());
        Assert.False(await db.Exercises.AnyAsync(x => x.Id == ImportedId));
        Assert.Equal(before, await Catalog(db));
        Assert.Equal(history, JsonSerializer.Serialize(await db.WorkoutSessionExercises.AsNoTracking().ToListAsync()));
        Assert.Equal(existing.Id, (await db.WorkoutSessionExercises.SingleAsync()).ExerciseId);
    }

    [Fact]
    public async Task PendingExercise_IsHiddenByService_AndNeverGenerated()
    {
        await using var db = await CreateDatabase();
        await Import(db);
        var service = new ExerciseService(db);
        Assert.Equal(25, (await service.GetAsync(null, null, default)).Count);
        Assert.DoesNotContain(await service.GetAsync(null, "Core", default), x => x.Id == ImportedId);
        var error = await Assert.ThrowsAsync<AppException>(() => service.GetByIdAsync(ImportedId, default));
        Assert.Equal(404, error.StatusCode);

        // Also disable a slug the generator actually selects, so the test proves filtering.
        var plank = await db.Exercises.SingleAsync(x => x.Slug == "plank");
        plank.IsActive = false;
        await db.SaveChangesAsync();
        var exercises = await db.Exercises.ToListAsync();
        foreach (var level in Enum.GetValues<FitnessLevel>())
        foreach (var bmi in Enum.GetValues<BmiCategory>())
        {
            var generated = WorkoutPlanGenerator.Generate(new UserProfile
            {
                FitnessLevel = level, BmiCategory = bmi, PreferredWorkoutDurationMinutes = 45
            }, exercises);
            Assert.NotEmpty(generated);
            Assert.All(generated, x => Assert.True(x.Exercise.IsActive));
            Assert.DoesNotContain(generated, x => x.Exercise.Id == ImportedId || x.Exercise.Slug == "plank");
        }
    }

    [Fact]
    public async Task ServiceMapping_PreservesFieldsIfLaterApproved()
    {
        await using var db = await CreateDatabase();
        await Import(db);
        var exercise = await db.Exercises.SingleAsync(x => x.Id == ImportedId);
        exercise.IsActive = true; // Test fixture only; production seed remains pending.
        await db.SaveChangesAsync();
        var response = await new ExerciseService(db).GetByIdAsync(ImportedId, default);
        Assert.Equal(exercise.Name, response.Name);
        Assert.Equal(exercise.Slug, response.Slug);
        Assert.Equal("Core", response.MuscleGroup);
        Assert.Equal(ExerciseType.Duration, response.ExerciseType);
        Assert.Equal(exercise.Description, response.Description);
        Assert.Equal(exercise.Instructions, response.Instructions);
        Assert.Null(response.DefaultSets);
        Assert.Null(response.DefaultDurationSeconds);
    }

    [Fact]
    public async Task ReservedIdCollision_FailsWithoutChangingExistingRecord()
    {
        await using var db = await CreateDatabase();
        db.Exercises.Add(new Exercise { Id = ImportedId, Name = "Unrelated", Slug = "unrelated", MuscleGroup = "Core" });
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<SqliteException>(() => Import(db));
        Assert.Equal("unrelated", (await db.Exercises.SingleAsync(x => x.Id == ImportedId)).Slug);
    }

    [Fact]
    public void Rollback_DoesNotDeletePotentiallyReferencedCatalogData() =>
        Assert.Empty(new ImportHollowBodyHold().DownOperations);

    private static async Task<CoreUpDbContext> CreateDatabase()
    {
        // Isolated relational test database; never uses local PostgreSQL or personal data.
        var db = new CoreUpDbContext(new DbContextOptionsBuilder<CoreUpDbContext>()
            .UseSqlite("Data Source=:memory:").Options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();
        await db.Exercises.Where(x => x.Id == ImportedId).ExecuteDeleteAsync();
        return db;
    }

    private static async Task Import(CoreUpDbContext db)
    {
        foreach (var operation in new ImportHollowBodyHold().UpOperations)
            await db.Database.ExecuteSqlRawAsync(Assert.IsType<SqlOperation>(operation).Sql);
    }

    private static async Task<string> Catalog(CoreUpDbContext db) =>
        JsonSerializer.Serialize(await db.Exercises.AsNoTracking()
            .Where(x => x.Id != ImportedId).OrderBy(x => x.Id).ToListAsync());
}
