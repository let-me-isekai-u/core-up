using CoreUp.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreUp.Application;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<EmailChallenge> EmailChallenges { get; }
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<Exercise> Exercises { get; }
    DbSet<WorkoutPlan> WorkoutPlans { get; }
    DbSet<WorkoutPlanExercise> WorkoutPlanExercises { get; }
    DbSet<WorkoutSession> WorkoutSessions { get; }
    DbSet<WorkoutSessionExercise> WorkoutSessionExercises { get; }
    DbSet<WeightRecord> WeightRecords { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IJwtTokenService
{
    string Create(User user);
}

public sealed class AppException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public interface IEmailSender
{
    Task SendAsync(string recipient, string subject, string body, CancellationToken ct);
}

public sealed class OtpOptions
{
    public string Key { get; set; } = string.Empty;
}
