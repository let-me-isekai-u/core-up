using CoreUp.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreUp.Application;

public sealed class WorkoutSessionService(IApplicationDbContext db)
{
    public async Task<WorkoutSessionResponse> StartAsync(Guid userId, StartWorkoutRequest request, CancellationToken ct)
    {
        var plan = await db.WorkoutPlans.AsNoTracking()
            .Include(x => x.Exercises).ThenInclude(x => x.Exercise)
            .SingleOrDefaultAsync(x => x.Id == request.WorkoutPlanId && x.UserId == userId, ct)
            ?? throw new AppException(404, "Workout plan was not found.");

        if (!plan.IsActive)
            throw new AppException(409, "Giáo trình đã đổi. Hãy tải lại kế hoạch đang hoạt động.");
        var profile = await db.UserProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct);
        var assessment = profile is null ? null : FitnessAssessmentScoring.FromProfile(profile);
        if (assessment is null)
            throw new AppException(400, "Hãy hoàn tất questionnaire trước khi tập Free.");
        // Applies to new starts only. Existing sessions/history are never rewritten.
        FitnessAssessmentScoring.EnsureAllowedLevel(assessment, plan.Difficulty);
        if (plan.Exercises.Count == 0 || plan.Exercises.Any(x => !x.Exercise.IsActive || x.Exercise.MuscleGroup != "Core"))
            throw new AppException(422, "Giáo trình cũ có bài không còn phù hợp. Hãy xác nhận lại mức tập để nhận giáo trình Core chuẩn.");

        var session = new WorkoutSession
        {
            Id = Guid.NewGuid(), UserId = userId, WorkoutPlanId = plan.Id,
            StartedAt = DateTime.UtcNow,
            Status = WorkoutSessionStatus.InProgress,
            TotalExercises = plan.Exercises.Count,
            TotalSets = plan.Exercises.Sum(x => x.Sets)
        };
        foreach (var item in plan.Exercises.OrderBy(x => x.Order))
        {
            session.Exercises.Add(new WorkoutSessionExercise
            {
                Id = Guid.NewGuid(), WorkoutSessionId = session.Id, ExerciseId = item.ExerciseId,
                Order = item.Order, TargetSets = item.Sets,
                TargetReps = item.Reps, TargetDurationSeconds = item.DurationSeconds,
                RestSeconds = item.RestSeconds
            });
        }
        db.WorkoutSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return await GetAsync(userId, session.Id, ct);
    }

    public async Task<WorkoutSessionResponse> GetAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var session = await Query().SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct)
            ?? throw new AppException(404, "Workout session was not found.");
        return Map(session);
    }

    public async Task<WorkoutSessionResponse> UpdateExerciseAsync(
        Guid userId, Guid sessionId, Guid exerciseId, UpdateWorkoutExerciseRequest request, CancellationToken ct)
    {
        var session = await db.WorkoutSessions
            .Include(x => x.WorkoutPlan)
            .Include(x => x.Exercises).ThenInclude(x => x.Exercise)
            .SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, ct)
            ?? throw new AppException(404, "Workout session was not found.");
        if (session.Status != WorkoutSessionStatus.InProgress)
            throw new AppException(409, "Only an in-progress workout can be updated.");
        var exercise = session.Exercises.SingleOrDefault(x => x.Id == exerciseId)
            ?? throw new AppException(404, "Session exercise was not found.");

        exercise.CompletedSets = request.CompletedSets;
        exercise.CompletedReps = request.CompletedReps;
        exercise.CompletedDurationSeconds = request.CompletedDurationSeconds;
        exercise.IsCompleted = request.IsCompleted;
        RefreshTotals(session);
        await db.SaveChangesAsync(ct);
        return Map(session);
    }

    public async Task<WorkoutSessionResponse> CompleteAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var session = await EditableQuery(userId, id, ct);
        if (session.Status != WorkoutSessionStatus.InProgress)
            throw new AppException(409, "Only an in-progress workout can be completed.");
        session.CompletedAt = DateTime.UtcNow;
        session.Status = WorkoutSessionStatus.Completed;
        session.DurationSeconds = (int)Math.Max(0, (session.CompletedAt.Value - session.StartedAt).TotalSeconds);
        RefreshTotals(session);
        await db.SaveChangesAsync(ct);
        return Map(session);
    }

    public async Task<WorkoutSessionResponse> CancelAsync(Guid userId, Guid id, CancellationToken ct)
    {
        var session = await EditableQuery(userId, id, ct);
        if (session.Status != WorkoutSessionStatus.InProgress)
            throw new AppException(409, "Only an in-progress workout can be cancelled.");
        session.Status = WorkoutSessionStatus.Cancelled;
        session.CompletedAt = DateTime.UtcNow;
        session.DurationSeconds = (int)Math.Max(0, (session.CompletedAt.Value - session.StartedAt).TotalSeconds);
        RefreshTotals(session);
        await db.SaveChangesAsync(ct);
        return Map(session);
    }

    public async Task<IReadOnlyList<WorkoutHistoryResponse>> HistoryAsync(
        Guid userId, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return await db.WorkoutSessions.AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == WorkoutSessionStatus.Completed)
            .OrderByDescending(x => x.StartedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new WorkoutHistoryResponse(x.Id, x.WorkoutPlan != null ? x.WorkoutPlan.Name : null,
                x.StartedAt, x.CompletedAt, x.DurationSeconds, x.CompletedExercises,
                x.TotalExercises, x.CompletedSets, x.TotalReps))
            .ToListAsync(ct);
    }

    private async Task<WorkoutSession> EditableQuery(Guid userId, Guid id, CancellationToken ct) =>
        await db.WorkoutSessions.Include(x => x.WorkoutPlan)
            .Include(x => x.Exercises).ThenInclude(x => x.Exercise)
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct)
            ?? throw new AppException(404, "Workout session was not found.");

    private IQueryable<WorkoutSession> Query() => db.WorkoutSessions.AsNoTracking()
        .Include(x => x.WorkoutPlan)
        .Include(x => x.Exercises.OrderBy(e => e.Order)).ThenInclude(x => x.Exercise);

    private static void RefreshTotals(WorkoutSession session)
    {
        session.CompletedExercises = session.Exercises.Count(x => x.IsCompleted);
        session.CompletedSets = session.Exercises.Sum(x => x.CompletedSets);
        session.TotalReps = session.Exercises.Sum(x => x.CompletedReps);
    }

    private static WorkoutSessionResponse Map(WorkoutSession session) => new(
        session.Id, session.WorkoutPlanId, session.WorkoutPlan?.Name,
        session.StartedAt, session.CompletedAt, session.Status, session.DurationSeconds,
        session.TotalExercises, session.CompletedExercises, session.TotalSets,
        session.CompletedSets, session.TotalReps,
        session.Exercises.OrderBy(x => x.Order).Select(x => new WorkoutSessionExerciseResponse(
            x.Id, x.Order, x.Exercise.ToResponse(), x.TargetSets, x.CompletedSets,
            x.TargetReps, x.CompletedReps, x.TargetDurationSeconds,
            x.CompletedDurationSeconds, x.RestSeconds, x.IsCompleted)).ToList());
}
