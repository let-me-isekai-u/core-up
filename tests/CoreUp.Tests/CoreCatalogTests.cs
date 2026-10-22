using System.Text.Json;
using CoreUp.Application;
using CoreUp.Domain;
using CoreUp.Infrastructure.Persistence;
using CoreUp.Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace CoreUp.Tests;

public sealed class CoreCatalogTests
{
    [Fact]
    public async Task SeedAndRepeatedMigration_Agree_AndPreserveExistingFields()
    {
        await using var current = await CatalogTestDatabase.Create();
        var expected = await Snapshot(current);
        var old = await current.Exercises.AsNoTracking().Where(x => x.CreatedAt < new DateTime(2026, 10, 22)).ToListAsync();
        await RestoreBaseline(current);
        await Apply(current);
        await Apply(current);
        Assert.Equal(expected, await Snapshot(current));
        Assert.Equal(33, await current.Exercises.CountAsync());
        Assert.Equal(16, await current.Exercises.CountAsync(x => x.MuscleGroup == "Core"));
        Assert.Equal(13, await current.Exercises.CountAsync(x => x.IsActive));
        Assert.False(await current.Exercises.AnyAsync(x => x.IsActive && x.MuscleGroup != "Core"));
        Assert.Equal(33, await current.Exercises.Select(x => x.Slug).Distinct().CountAsync());
        foreach (var original in old)
        {
            var after = await current.Exercises.AsNoTracking().SingleAsync(x => x.Id == original.Id);
            Assert.Equal(JsonSerializer.Serialize(original), JsonSerializer.Serialize(after));
        }
    }

    [Theory]
    [InlineData("pelvic-tilt", "Posterior pelvic tilt", "pelvic-tilt")]
    [InlineData("custom-heel", "Heel Slides", "supine-heel-slide")]
    [InlineData("bent_knee_fall_out", "Custom fallout", "bent-knee-fallout")]
    [InlineData("custom-single", " Single Leg Abdominal Press ", "single-leg-abdominal-press")]
    [InlineData("kneeling-plank", "Custom kneeling", "modified-knee-plank")]
    [InlineData("custom-side", "Side plank on knees", "modified-side-plank")]
    [InlineData("custom-double", "Double-leg abdominal press", "double-leg-abdominal-press")]
    public async Task AliasAndVariantImport_SkipsCandidate_PreservesHistoricalRelationship(string slug, string name, string canonical)
    {
        await using var db = await CatalogTestDatabase.Create();
        await RestoreBaseline(db);
        var alias = new Exercise
        {
            Id = Guid.NewGuid(), Name = name, Slug = slug, MuscleGroup = "Core", IsActive = false,
            DefaultReps = 2, Description = "Custom preserved", CreatedAt = DateTime.UtcNow
        };
        var user = new User { Id = Guid.NewGuid(), Email = "history@example.invalid", PasswordHash = "test" };
        var plan = new WorkoutPlan
        {
            Id = Guid.NewGuid(), User = user, Name = "Historical Full Body", IsActive = true,
            Exercises = [new WorkoutPlanExercise
            {
                Id = Guid.NewGuid(), ExerciseId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Order = 1, Sets = 3, Reps = 12, RestSeconds = 45
            }]
        };
        var session = new WorkoutSession
        {
            Id = Guid.NewGuid(), User = user, WorkoutPlan = plan, Status = WorkoutSessionStatus.Completed,
            Exercises = [new WorkoutSessionExercise
            {
                Id = Guid.NewGuid(), Exercise = alias, Order = 1, TargetSets = 1, CompletedSets = 1,
                TargetReps = 2, CompletedReps = 2, IsCompleted = true
            }]
        };
        db.WorkoutSessions.Add(session);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var history = JsonSerializer.Serialize(await db.WorkoutSessionExercises.AsNoTracking().ToListAsync());
        var plans = JsonSerializer.Serialize(await db.WorkoutPlans.AsNoTracking().ToListAsync());
        var planItems = JsonSerializer.Serialize(await db.WorkoutPlanExercises.AsNoTracking().ToListAsync());
        var original = JsonSerializer.Serialize(await db.Exercises.AsNoTracking().SingleAsync(x => x.Id == alias.Id));
        await Apply(db);
        await Apply(db);
        Assert.Equal(33, await db.Exercises.CountAsync());
        Assert.Equal(original, JsonSerializer.Serialize(await db.Exercises.AsNoTracking().SingleAsync(x => x.Id == alias.Id)));
        // Canonical slug can be the alias itself, but no canonical new GUID may have been added.
        Assert.Single(await db.Exercises.Where(x => x.Slug == slug).ToListAsync());
        if (slug != canonical) Assert.False(await db.Exercises.AnyAsync(x => x.Slug == canonical));
        Assert.Equal(history, JsonSerializer.Serialize(await db.WorkoutSessionExercises.AsNoTracking().ToListAsync()));
        Assert.Equal(plans, JsonSerializer.Serialize(await db.WorkoutPlans.AsNoTracking().ToListAsync()));
        Assert.Equal(planItems, JsonSerializer.Serialize(await db.WorkoutPlanExercises.AsNoTracking().ToListAsync()));
    }

    [Fact]
    public async Task ActiveApiCatalog_OnlyReturnsCore_EvenIfLegacyEntryReactivated()
    {
        await using var db = await CatalogTestDatabase.Create();
        var squat = await db.Exercises.SingleAsync(x => x.Slug == "bodyweight-squat");
        squat.IsActive = true;
        await db.SaveChangesAsync();
        var service = new ExerciseService(db);
        var active = await service.GetAsync(null, null, default);
        Assert.Equal(13, active.Count);
        Assert.All(active, x => Assert.Equal("Core", x.MuscleGroup));
        Assert.Empty(await service.GetAsync(null, "Legs", default));
        Assert.Equal(404, (await Assert.ThrowsAsync<AppException>(() => service.GetByIdAsync(squat.Id, default))).StatusCode);
        foreach (var slug in new[] { "pelvic-tilt", "bent-knee-fallout", "hollow-body-hold" })
        {
            var pending = await db.Exercises.SingleAsync(x => x.Slug == slug);
            Assert.False(pending.IsActive);
            Assert.Null(pending.DefaultSets);
            Assert.Null(pending.DefaultReps);
            Assert.Null(pending.DefaultDurationSeconds);
            Assert.Equal(404, (await Assert.ThrowsAsync<AppException>(() => service.GetByIdAsync(pending.Id, default))).StatusCode);
        }
        Assert.Empty(new ExpandCoreCatalog().DownOperations);
    }

    private static async Task<string> Snapshot(CoreUpDbContext db)
    {
        var rows = await db.Exercises.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
        // SQLite has no timestamp-with-time-zone type; PostgreSQL stores these as UTC.
        foreach (var row in rows) row.CreatedAt = DateTime.SpecifyKind(row.CreatedAt, DateTimeKind.Utc);
        return JsonSerializer.Serialize(rows);
    }

    internal static async Task Apply(CoreUpDbContext db)
    {
        foreach (var operation in new ExpandCoreCatalog().UpOperations)
            await db.Database.ExecuteSqlRawAsync(Assert.IsType<SqlOperation>(operation).Sql);
    }

    private static async Task RestoreBaseline(CoreUpDbContext db)
    {
        await db.Exercises.Where(x => x.CreatedAt >= new DateTime(2026, 10, 22)).ExecuteDeleteAsync();
        await db.Exercises.Where(x => x.Slug != "hollow-body-hold").ExecuteUpdateAsync(x => x.SetProperty(e => e.IsActive, true));
    }
}
