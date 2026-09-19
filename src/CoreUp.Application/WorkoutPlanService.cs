using CoreUp.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreUp.Application;

public sealed class WorkoutPlanService(IApplicationDbContext db)
{
    public async Task<IReadOnlyList<WorkoutPlanSummaryResponse>> GetAllAsync(Guid userId, CancellationToken ct) =>
        await db.WorkoutPlans.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new WorkoutPlanSummaryResponse(x.Id, x.Name, x.Goal, x.Difficulty,
                x.EstimatedDurationMinutes, x.IsActive, x.IsGenerated, x.Exercises.Count))
            .ToListAsync(ct);

    public async Task<WorkoutPlanResponse> GetActiveAsync(Guid userId, CancellationToken ct)
    {
        var plan = await PlanQuery().SingleOrDefaultAsync(x => x.UserId == userId && x.IsActive, ct)
            ?? throw new AppException(404, "No active workout plan was found.");
        return Map(plan);
    }

    public async Task<WorkoutPlanResponse> GetByIdAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var plan = await PlanQuery().SingleOrDefaultAsync(x => x.UserId == userId && x.Id == id, ct)
            ?? throw new AppException(404, "Workout plan was not found.");
        return Map(plan);
    }

    public async Task<WorkoutPlanResponse> GenerateAsync(Guid userId, CancellationToken ct)
    {
        var profile = await db.UserProfiles.SingleOrDefaultAsync(x => x.UserId == userId, ct)
            ?? throw new AppException(400, "Complete your profile before generating a workout plan.");
        var exercises = await db.Exercises.Where(x => x.IsActive).ToListAsync(ct);
        var generated = WorkoutPlanGenerator.Generate(profile, exercises);
        if (generated.Count == 0) throw new AppException(500, "Exercise seed data is missing.");

        var activePlans = await db.WorkoutPlans.Where(x => x.UserId == userId && x.IsActive).ToListAsync(ct);
        foreach (var active in activePlans)
        {
            active.IsActive = false;
            active.UpdatedAt = DateTime.UtcNow;
        }

        var now = DateTime.UtcNow;
        var plan = new WorkoutPlan
        {
            Id = Guid.NewGuid(), UserId = userId,
            Name = $"{profile.DifficultyName()} Full Body",
            Description = $"Rule-based {profile.Goal} plan generated from your current profile.",
            Goal = profile.Goal, Difficulty = profile.FitnessLevel,
            EstimatedDurationMinutes = profile.PreferredWorkoutDurationMinutes,
            IsActive = true, IsGenerated = true, CreatedAt = now
        };

        var order = 1;
        foreach (var item in generated)
        {
            plan.Exercises.Add(new WorkoutPlanExercise
            {
                Id = Guid.NewGuid(), WorkoutPlanId = plan.Id, ExerciseId = item.Exercise.Id,
                Exercise = item.Exercise, Order = order++, Sets = item.Sets, Reps = item.Reps,
                DurationSeconds = item.DurationSeconds, RestSeconds = item.RestSeconds
            });
        }

        db.WorkoutPlans.Add(plan);
        await db.SaveChangesAsync(ct);
        return Map(plan);
    }

    private IQueryable<WorkoutPlan> PlanQuery() => db.WorkoutPlans.AsNoTracking()
        .Include(x => x.Exercises.OrderBy(e => e.Order)).ThenInclude(x => x.Exercise);

    private static WorkoutPlanResponse Map(WorkoutPlan plan) => new(
        plan.Id, plan.Name, plan.Description, plan.Goal, plan.Difficulty,
        plan.EstimatedDurationMinutes, plan.IsActive, plan.IsGenerated,
        plan.Exercises.OrderBy(x => x.Order).Select(x => new WorkoutPlanExerciseResponse(
            x.Id, x.Order, x.Exercise.ToResponse(), x.Sets, x.Reps,
            x.DurationSeconds, x.RestSeconds, x.Notes)).ToList());
}

internal static class ProfileNamingExtensions
{
    public static string DifficultyName(this UserProfile profile) => profile.FitnessLevel.ToString();
}
