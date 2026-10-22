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
        var exercises = await db.Exercises.Where(x => x.IsActive && x.MuscleGroup == "Core").ToListAsync(ct);
        var generated = WorkoutPlanGenerator.Generate(profile, exercises);
        var estimatedMinutes = WorkoutPlanGenerator.EstimateMinutes(generated);

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
            Name = $"{generated.Max(x => x.Exercise.Difficulty)} Core",
            Description = $"Core stability: {generated.Count} exercises. Your selected level is {profile.FitnessLevel}; this session uses available exercises up to {generated.Max(x => x.Exercise.Difficulty)}. Practice these movements with control before progressing. Session length is an estimate including breathing and rest; you do not need to fill the requested time. Rest longer or stop if you lose control or feel pain. Abdominal training does not reduce fat only around the belly.",
            Goal = profile.Goal, Difficulty = generated.Max(x => x.Exercise.Difficulty),
            EstimatedDurationMinutes = estimatedMinutes,
            IsActive = true, IsGenerated = true, CreatedAt = now
        };

        var order = 1;
        foreach (var item in generated)
        {
            plan.Exercises.Add(new WorkoutPlanExercise
            {
                Id = Guid.NewGuid(), WorkoutPlanId = plan.Id, ExerciseId = item.Exercise.Id,
                Exercise = item.Exercise, Order = order++, Sets = item.Sets, Reps = item.Reps,
                DurationSeconds = item.DurationSeconds, RestSeconds = item.RestSeconds,
                Notes = "Follow the exercise instructions for side counts and breathing. Rest longer or stop if needed; do not train through pain."
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
