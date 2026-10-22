using CoreUp.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreUp.Application;

public sealed class WeightProgressService(IApplicationDbContext db)
{
    public async Task<IReadOnlyList<WeightRecordResponse>> GetWeightsAsync(Guid userId, CancellationToken ct) =>
        await db.WeightRecords.AsNoTracking().Where(x => x.UserId == userId)
            .OrderBy(x => x.RecordedAt)
            .Select(x => new WeightRecordResponse(x.Id, x.WeightKg, x.RecordedAt))
            .ToListAsync(ct);

    public async Task<WeightRecordResponse> AddWeightAsync(
        Guid userId, AddWeightRecordRequest request, CancellationToken ct)
    {
        var profile = await db.UserProfiles.SingleOrDefaultAsync(x => x.UserId == userId, ct)
            ?? throw new AppException(400, "Complete your profile before recording weight.");
        var record = new WeightRecord
        {
            Id = Guid.NewGuid(), UserId = userId,
            WeightKg = request.WeightKg, RecordedAt = DateTime.UtcNow
        };
        profile.WeightKg = request.WeightKg;
        profile.Bmi = profile.HeightCm is decimal height ? BmiCalculator.Calculate(height, request.WeightKg) : null;
        profile.BmiCategory = profile.Bmi is decimal bmi ? BmiCalculator.Classify(bmi) : null;
        profile.UpdatedAt = DateTime.UtcNow;
        db.WeightRecords.Add(record);
        await db.SaveChangesAsync(ct);
        return new WeightRecordResponse(record.Id, record.WeightKg, record.RecordedAt);
    }

    public async Task<ProgressSummaryResponse> GetProgressAsync(Guid userId, CancellationToken ct)
    {
        var profile = await db.UserProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct);
        var completed = await db.WorkoutSessions.AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == WorkoutSessionStatus.Completed)
            .Select(x => new { x.CompletedAt, x.DurationSeconds }).ToListAsync(ct);
        var weights = await db.WeightRecords.AsNoTracking().Where(x => x.UserId == userId)
            .OrderBy(x => x.RecordedAt).ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysFromMonday = ((int)today.DayOfWeek + 6) % 7;
        var weekStart = today.AddDays(-daysFromMonday);
        var workoutDates = completed.Where(x => x.CompletedAt.HasValue)
            .Select(x => DateOnly.FromDateTime(x.CompletedAt!.Value)).ToHashSet();
        var cursor = workoutDates.Contains(today) ? today : today.AddDays(-1);
        var streak = 0;
        while (workoutDates.Contains(cursor)) { streak++; cursor = cursor.AddDays(-1); }

        return new ProgressSummaryResponse(
            profile?.WeightKg, profile?.TargetWeightKg, completed.Count,
            completed.Count(x => x.CompletedAt.HasValue && DateOnly.FromDateTime(x.CompletedAt.Value) >= weekStart),
            (int)Math.Round(completed.Sum(x => x.DurationSeconds) / 60d), streak,
            weights.Select(x => new WeightHistoryPoint(DateOnly.FromDateTime(x.RecordedAt), x.WeightKg)).ToList());
    }
}
