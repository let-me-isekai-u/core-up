using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CoreUp.Application;
using CoreUp.Domain;
using CoreUp.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace CoreUp.Tests;

// Opt in with COREUP_TEST_POSTGRES pointing at a disposable local coreup_test_* database.
// The suite never resets or deletes a database and cannot target the development database.
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COREUP_TEST_POSTGRES")))
            Skip = "Set COREUP_TEST_POSTGRES to a disposable local coreup_test_* database.";
    }
}

[Collection("PostgreSQL")]
public sealed class PostgresCoreCatalogTests
{
    [PostgresFact]
    public async Task MigrationsAndHttpApis_KeepCoreScopeAuthenticationAndHistory()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CoreUpDbContext>();
            await db.Database.MigrateAsync();
            Assert.Equal(33, await db.Exercises.CountAsync());
            Assert.Equal(16, await db.Exercises.CountAsync(x => x.MuscleGroup == "Core"));
            Assert.Equal(16, await db.Exercises.CountAsync(x => x.IsActive));
            await CoreCatalogTests.Apply(db);
            await CoreCatalogTests.Apply(db);
            await db.Database.MigrateAsync();
            Assert.Equal(33, await db.Exercises.CountAsync());
        }
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        // Guest cannot personalize or supply profile data to these authenticated endpoints.
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/workout-plans/generate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/profile", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/exercises")).StatusCode);

        var credentials = new { username = "core_" + Guid.NewGuid().ToString("N")[..16], email = $"core-{Guid.NewGuid():N}@example.invalid", password = "CoreTestOnly-12345!" };
        var registered = await client.PostAsJsonAsync("/api/auth/register", credentials);
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        var auth = await Json(registered);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/auth/login", credentials)).StatusCode);
        var catalog = await Json(await client.GetAsync("/api/exercises"));
        Assert.Equal(16, catalog.GetArrayLength());
        foreach (var item in catalog.EnumerateArray()) Assert.Equal("Core", item.GetProperty("muscleGroup").GetString());
        var approved = catalog.EnumerateArray().Single(x => x.GetProperty("slug").GetString() == "supine-heel-slide");
        var detail = await Json(await client.GetAsync($"/api/exercises/{approved.GetProperty("id").GetString()}"));
        Assert.Equal("Repetition", detail.GetProperty("exerciseType").GetString());
        Assert.Equal(10, detail.GetProperty("defaultReps").GetInt32());
        Assert.Equal(JsonValueKind.Null, detail.GetProperty("defaultDurationSeconds").ValueKind);
        Assert.Contains("Dụng cụ", detail.GetProperty("instructions").GetString());
        foreach (var id in new[] { 1 })
            Assert.Equal(HttpStatusCode.NotFound,
                (await client.GetAsync($"/api/exercises/00000000-0000-0000-0000-{id:D12}")).StatusCode);
        foreach (var id in new[] { 26, 27, 29 })
        {
            var activated = await Json(await client.GetAsync($"/api/exercises/00000000-0000-0000-0000-{id:D12}"));
            Assert.Equal("Core", activated.GetProperty("muscleGroup").GetString());
            Assert.Equal(60, activated.GetProperty("defaultRestSeconds").GetInt32());
        }
        var created = await client.PostAsJsonAsync("/api/profile", Profile("Beginner"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/profile")).StatusCode);
        var firstPlan = await Json(await client.GetAsync("/api/workout-plans/active"));
        AssertCorePlan(firstPlan, "single-leg-abdominal-press");
        var firstPlanId = firstPlan.GetProperty("id").GetString();
        var start = await client.PostAsJsonAsync("/api/workout-sessions/start", new { workoutPlanId = firstPlanId });
        // Keep the complete session flow under the actual existing API contract.
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        var session = await Json(start);
        var sessionId = session.GetProperty("id").GetString();
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/workout-sessions/{sessionId}/complete", null)).StatusCode);

        foreach (var level in new[] { "Intermediate", "Advanced" })
        {
            Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/profile", Profile(level))).StatusCode);
            var plan = await Json(await client.PostAsync("/api/workout-plans/generate", null));
            AssertCorePlan(plan, "double-leg-abdominal-press");
            Assert.Equal("Intermediate", plan.GetProperty("difficulty").GetString());
        }
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"/api/workout-plans/{firstPlanId}")).StatusCode);
        var oldSession = await Json(await client.GetAsync($"/api/workout-sessions/{sessionId}"));
        Assert.Equal("Completed", oldSession.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/progress/summary")).StatusCode);

        using var otherClient = factory.CreateClient();
        var otherAuth = await Json(await otherClient.PostAsJsonAsync("/api/auth/register", new
        {
            username = "other_" + Guid.NewGuid().ToString("N")[..16],
            email = $"other-{Guid.NewGuid():N}@example.invalid", password = "CoreTestOnly-12345!"
        }));
        otherClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherAuth.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await otherClient.GetAsync($"/api/workout-plans/{firstPlanId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherClient.GetAsync($"/api/workout-sessions/{sessionId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await otherClient.GetAsync("/api/profile")).StatusCode);

        // Insufficient catalog fails before replacing the user's current plan.
        using var verification = factory.Services.CreateScope();
        var context = verification.ServiceProvider.GetRequiredService<CoreUpDbContext>();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var userId = Guid.Parse(auth.GetProperty("userId").GetString()!);
        var activeId = await context.WorkoutPlans.Where(x => x.UserId == userId && x.IsActive).Select(x => x.Id).SingleAsync();
        await context.Exercises.Where(x => x.Slug == "supine-heel-slide" || x.Slug == "modified-knee-plank")
            .ExecuteUpdateAsync(x => x.SetProperty(e => e.IsActive, false));
        var error = await Assert.ThrowsAsync<AppException>(() =>
            new WorkoutPlanService(context).GenerateAsync(userId, default));
        Assert.Equal(422, error.StatusCode);
        Assert.Equal(activeId, await context.WorkoutPlans.Where(x => x.UserId == userId && x.IsActive).Select(x => x.Id).SingleAsync());
        await transaction.RollbackAsync();
    }

    [PostgresFact]
    public async Task PostgresAliasImport_PreservesCustomIdAndCompletedSession()
    {
        await using var factory = Factory();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CoreUpDbContext>();
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.Exercises.Where(x => x.Slug == "bent-knee-fallout").ExecuteDeleteAsync();
        var alias = new Exercise
        {
            Id = Guid.NewGuid(), Name = "Single knee fallout", Slug = "custom-fallout", MuscleGroup = "Core",
            IsActive = false, Description = "Preserve custom review", DefaultRestSeconds = 75, CreatedAt = DateTime.UtcNow
        };
        var user = new User { Id = Guid.NewGuid(), Username = "fixture_" + Guid.NewGuid().ToString("N")[..16], Email = $"alias-{Guid.NewGuid():N}@example.invalid", PasswordHash = "test" };
        var session = new WorkoutSession
        {
            Id = Guid.NewGuid(), User = user, Status = WorkoutSessionStatus.Completed,
            Exercises = [new WorkoutSessionExercise
            {
                Id = Guid.NewGuid(), Exercise = alias, Order = 1, TargetSets = 1, CompletedSets = 1,
                TargetReps = 5, CompletedReps = 5, IsCompleted = true
            }]
        };
        db.WorkoutSessions.Add(session);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var before = JsonSerializer.Serialize(await db.WorkoutSessionExercises.AsNoTracking().Where(x => x.WorkoutSessionId == session.Id).ToListAsync());
        await CoreCatalogTests.Apply(db);
        await CoreCatalogTests.Apply(db);
        Assert.Equal(33, await db.Exercises.CountAsync());
        Assert.False(await db.Exercises.AnyAsync(x => x.Slug == "bent-knee-fallout"));
        var preserved = await db.Exercises.AsNoTracking().SingleAsync(x => x.Id == alias.Id);
        Assert.Equal("Preserve custom review", preserved.Description);
        Assert.Equal(75, preserved.DefaultRestSeconds);
        Assert.False(preserved.IsActive);
        Assert.Equal(before, JsonSerializer.Serialize(await db.WorkoutSessionExercises.AsNoTracking().Where(x => x.WorkoutSessionId == session.Id).ToListAsync()));
        await transaction.RollbackAsync();
    }

    private static object Profile(string level) => new
    {
        fullName = "Integration Test", heightCm = 165, weightKg = 90, fitnessLevel = level,
        goal = "LoseWeight", trainingDaysPerWeek = 3, preferredWorkoutDurationMinutes = 30
    };

    private static void AssertCorePlan(JsonElement plan, string press)
    {
        Assert.Contains("Core", plan.GetProperty("name").GetString());
        Assert.DoesNotContain("Full Body", plan.GetProperty("name").GetString());
        var exercises = plan.GetProperty("exercises").EnumerateArray().ToList();
        Assert.Equal(4, exercises.Count);
        Assert.Contains(exercises, x => x.GetProperty("exercise").GetProperty("slug").GetString() == press);
        Assert.All(exercises, x =>
        {
            Assert.Equal("Core", x.GetProperty("exercise").GetProperty("muscleGroup").GetString());
            Assert.Equal(60, x.GetProperty("restSeconds").GetInt32());
        });
        Assert.InRange(plan.GetProperty("estimatedDurationMinutes").GetInt32(), 1, 15);
    }

    private static async Task<JsonElement> Json(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, $"Unexpected HTTP status: {response.StatusCode}; {await response.Content.ReadAsStringAsync()}");
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    internal static WebApplicationFactory<Program> Factory()
    {
        var connection = Environment.GetEnvironmentVariable("COREUP_TEST_POSTGRES")!;
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.StartsWith("coreup_test_", settings.Database);
        Assert.Contains(settings.Host, new[] { "localhost", "127.0.0.1", "::1" });
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<CoreUpDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<CoreUpDbContext>>();
                services.AddDbContext<CoreUpDbContext>(options => options.UseNpgsql(connection));
            });
        });
    }
}
