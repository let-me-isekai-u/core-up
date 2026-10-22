using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CoreUp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using CoreUp.Domain;
using Npgsql;

namespace CoreUp.Tests;

[Collection("PostgreSQL")]
public sealed class UsernameAuthenticationTests
{
    [PostgresFact]
    public async Task RegisterLoginValidationJwtAndConcurrentUniqueness()
    {
        await using var factory = PostgresCoreCatalogTests.Factory();
        using var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<CoreUpDbContext>().Database.MigrateAsync();
        var name = "auth_" + Guid.NewGuid().ToString("N")[..12];
        var email = name + "@example.com";
        const string password = "TestPassword-123!";
        async Task<HttpResponseMessage> Register(string username, string address, string secret = password) =>
            await client.PostAsJsonAsync("/api/auth/register", new { username, email = address, password = secret });
        var registered = await Register(name, email.ToUpperInvariant());
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        var auth = await registered.Content.ReadFromJsonAsync<JsonElement>();
        var id = auth.GetProperty("userId").GetGuid();
        Assert.Equal(email, auth.GetProperty("email").GetString());
        Assert.True(auth.GetProperty("needsProfile").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(auth.GetProperty("accessToken").GetString()));
        using (var scope = factory.Services.CreateScope())
        {
            var user = await scope.ServiceProvider.GetRequiredService<CoreUpDbContext>().Users.SingleAsync(x => x.Id == id);
            Assert.NotEqual(password, user.PasswordHash);
            Assert.NotEqual(PasswordVerificationResult.Failed, new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, password));
        }
        Assert.Equal(HttpStatusCode.Conflict, (await Register(name, "different" + email)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Register(name.ToUpperInvariant(), "other" + email)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await Register(name + "x", email.ToUpperInvariant())).StatusCode);
        foreach (var invalid in new[] { "", "ab", "a-b", "tên", new string('a', 25) })
            Assert.Equal(HttpStatusCode.BadRequest, (await Register(invalid, "valid" + email)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Register(name + "a", "invalid-email")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Register(name + "b", email, "short")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Register(name + "c", email, new string('p', 129))).StatusCode);
        var loggedIn = await client.PostAsJsonAsync("/api/auth/login", new { username = name.ToUpperInvariant(), password });
        Assert.Equal(HttpStatusCode.OK, loggedIn.StatusCode);
        var login = await loggedIn.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(id, login.GetProperty("userId").GetGuid());
        var wrongPassword = await client.PostAsJsonAsync("/api/auth/login", new { username = name, password = "wrongpass" });
        var unknownUser = await client.PostAsJsonAsync("/api/auth/login", new { username = "unknown_user", password });
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownUser.StatusCode);
        var firstError = await wrongPassword.Content.ReadFromJsonAsync<JsonElement>();
        var secondError = await unknownUser.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(firstError.GetProperty("detail").GetString(), secondError.GetProperty("detail").GetString());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.GetProperty("accessToken").GetString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/exercises")).StatusCode);
        // A valid account awaiting profile returns 404, not 401; restore must preserve it.
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/profile")).StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/profile")).StatusCode);
        var raceName = name + "race";
        var race = await Task.WhenAll(Register(raceName, "race1" + email), Register(raceName.ToUpperInvariant(), "race2" + email));
        Assert.Single(race, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(race, x => x.StatusCode == HttpStatusCode.Conflict);
        var raceEmail = "raceemail" + email;
        var emailRace = await Task.WhenAll(Register(name + "r1", raceEmail), Register(name + "r2", raceEmail.ToUpperInvariant()));
        Assert.Single(emailRace, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(emailRace, x => x.StatusCode == HttpStatusCode.Conflict);
        HttpResponseMessage? limited = null;
        for (var i = 0; i < 31; i++)
            limited = await client.PostAsJsonAsync("/api/auth/login", new { username = "unknown_user", password });
        Assert.Equal(HttpStatusCode.TooManyRequests, limited!.StatusCode);
    }

    [PostgresFact]
    public async Task MigrationBackfillsLegacyAccountsWithoutChangingTheirData()
    {
        // Dedicated database for migration-from-previous-version testing; never down-migrate
        // the shared test database or development data.
        var settings = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("COREUP_TEST_POSTGRES"));
        Assert.StartsWith("coreup_test_", settings.Database);
        Assert.Contains(settings.Host, new[] { "localhost", "127.0.0.1", "::1" });
        var testName = "coreup_test_legacy_" + Guid.NewGuid().ToString("N");
        await using var admin = new NpgsqlConnection(settings.ConnectionString);
        await admin.OpenAsync();
        await new NpgsqlCommand($"CREATE DATABASE \"{testName}\"", admin).ExecuteNonQueryAsync();
        settings.Database = testName;
        try
        {
            await using var db = new CoreUpDbContext(new DbContextOptionsBuilder<CoreUpDbContext>().UseNpgsql(settings.ConnectionString).Options);
            var migrator = db.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
            await migrator.MigrateAsync("20261022061935_ActivateOwnerApprovedCoreExercises");
            await using var connection = new NpgsqlConnection(settings.ConnectionString);
            await connection.OpenAsync();
            var id = Guid.NewGuid();
            var hash = new PasswordHasher<User>().HashPassword(new User(), "OldPassword-123!");
            // Same email prefixes, invalid username punctuation and differing case cannot collide.
            foreach (var email in new[] { "a+b@example.com", "a+b@other.com", "A.B@example.com" })
            {
                var userId = email.StartsWith("A.B") ? id : Guid.NewGuid();
                await using var insert = new NpgsqlCommand("INSERT INTO users (\"Id\",\"Email\",\"PasswordHash\",\"CreatedAt\") VALUES ($1,$2,$3,now())", connection);
                insert.Parameters.AddWithValue(userId); insert.Parameters.AddWithValue(email); insert.Parameters.AddWithValue(hash);
                await insert.ExecuteNonQueryAsync();
            }
            // Representative existing profile, weight, plan and completed session, before
            // the new migration. Catalog references and IDs must remain intact.
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO user_profiles ("Id","UserId","FullName","HeightCm","WeightKg","FitnessLevel","Goal","TrainingDaysPerWeek","PreferredWorkoutDurationMinutes","Bmi","BmiCategory","CreatedAt")
                VALUES (gen_random_uuid(),{id},'Legacy User',170,70,'Beginner','Maintain',3,20,24.22,'Normal',now());
                INSERT INTO weight_records ("Id","UserId","WeightKg","RecordedAt") VALUES (gen_random_uuid(),{id},70,now());
                INSERT INTO workout_plans ("Id","UserId","Name","Goal","Difficulty","EstimatedDurationMinutes","IsActive","IsGenerated","CreatedAt")
                VALUES ('11111111-1111-1111-1111-111111111111',{id},'Legacy Core','Maintain','Beginner',20,true,true,now());
                INSERT INTO workout_plan_exercises ("Id","WorkoutPlanId","ExerciseId","Order","Sets","Reps","RestSeconds")
                VALUES (gen_random_uuid(),'11111111-1111-1111-1111-111111111111','00000000-0000-0000-0000-000000000014',1,1,10,60);
                INSERT INTO workout_sessions ("Id","UserId","WorkoutPlanId","StartedAt","CompletedAt","Status","DurationSeconds","TotalExercises","CompletedExercises","TotalSets","CompletedSets","TotalReps")
                VALUES ('22222222-2222-2222-2222-222222222222',{id},'11111111-1111-1111-1111-111111111111',now(),now(),'Completed',120,1,1,1,1,10);
                INSERT INTO workout_session_exercises ("Id","WorkoutSessionId","ExerciseId","Order","TargetSets","CompletedSets","TargetReps","CompletedReps","CompletedDurationSeconds","RestSeconds","IsCompleted")
                VALUES (gen_random_uuid(),'22222222-2222-2222-2222-222222222222','00000000-0000-0000-0000-000000000014',1,1,1,10,10,0,60,true);
                """);
            var tables = new[] { "user_profiles", "weight_records", "workout_plans", "workout_plan_exercises", "workout_sessions", "workout_session_exercises" };
            var snapshots = new Dictionary<string, string>();
            foreach (var table in tables)
                snapshots[table] = (string)(await new NpgsqlCommand($"SELECT jsonb_agg(to_jsonb(t) ORDER BY \"Id\")::text FROM {table} t", connection).ExecuteScalarAsync())!;
            async Task<string> Snapshot() => (string)(await new NpgsqlCommand("SELECT jsonb_agg(to_jsonb(u) - 'Username' - 'NormalizedUsername' ORDER BY \"Id\")::text FROM users u", connection).ExecuteScalarAsync())!;
            var before = await Snapshot();
            var catalogBefore = (string)(await new NpgsqlCommand("SELECT jsonb_agg(to_jsonb(e) ORDER BY \"Id\")::text FROM exercises e", connection).ExecuteScalarAsync())!;
            await migrator.MigrateAsync();
            Assert.Equal(before, await Snapshot());
            foreach (var table in tables)
                Assert.Equal(snapshots[table], await new NpgsqlCommand($"SELECT jsonb_agg(to_jsonb(t) ORDER BY \"Id\")::text FROM {table} t", connection).ExecuteScalarAsync());
            Assert.Equal(catalogBefore, await new NpgsqlCommand("SELECT jsonb_agg(to_jsonb(e) ORDER BY \"Id\")::text FROM exercises e", connection).ExecuteScalarAsync());
            var users = await db.Users.AsNoTracking().ToListAsync();
            Assert.Equal(3, users.Select(x => x.NormalizedUsername).Distinct().Count());
            Assert.All(users, x => Assert.Matches("^[A-Za-z0-9_]{3,24}$", x.Username));
            var legacy = users.Single(x => x.Id == id);
            Assert.Equal(hash, legacy.PasswordHash);
            var service = new CoreUp.Application.AuthService(db, new PasswordHasher<User>(), new TestTokens());
            var restored = await service.LoginAsync(new CoreUp.Application.LoginRequest { Username = legacy.Username.ToUpperInvariant(), Password = "OldPassword-123!" }, default);
            Assert.Equal(id, restored.UserId);
            Assert.False(restored.NeedsProfile);
            Assert.NotEqual(PasswordVerificationResult.Failed, new PasswordHasher<User>().VerifyHashedPassword(legacy, legacy.PasswordHash, "OldPassword-123!"));
            await migrator.MigrateAsync();
            Assert.Equal(before, await Snapshot());
        }
        finally
        {
            NpgsqlConnection.ClearAllPools();
            await new NpgsqlCommand($"DROP DATABASE \"{testName}\" WITH (FORCE)", admin).ExecuteNonQueryAsync();
        }
    }
}

file sealed class TestTokens : CoreUp.Application.IJwtTokenService
{
    public string Create(User user) => "fixture-token";
}

[CollectionDefinition("PostgreSQL", DisableParallelization = true)]
public sealed class PostgresCollection;
