using CoreUp.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreUp.Application;

public sealed class ProfileService(IApplicationDbContext db, WorkoutPlanService workoutPlans)
{
    public async Task<ProfileResponse> GetAsync(Guid userId, CancellationToken ct)
    {
        var profile = await db.UserProfiles.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId, ct)
            ?? throw new AppException(404, "Profile was not found.");
        return profile.ToResponse();
    }

    public async Task<ProfileResponse> CreateAsync(Guid userId, ProfileRequest request, CancellationToken ct)
    {
        if (await db.UserProfiles.AnyAsync(x => x.UserId == userId, ct))
            throw new AppException(409, "Profile already exists. Use PUT to update it.");
        if (!await db.Users.AnyAsync(x => x.Id == userId, ct))
            throw new AppException(404, "User was not found.");

        var now = DateTime.UtcNow;
        var profile = new UserProfile { Id = Guid.NewGuid(), UserId = userId, CreatedAt = now };
        Apply(profile, request);
        db.UserProfiles.Add(profile);
        db.WeightRecords.Add(new WeightRecord
        {
            Id = Guid.NewGuid(), UserId = userId, WeightKg = request.WeightKg, RecordedAt = now
        });
        await db.SaveChangesAsync(ct);

        var generated = await workoutPlans.GenerateAsync(userId, ct);
        var summary = new WorkoutPlanSummaryResponse(generated.Id, generated.Name, generated.Goal,
            generated.Difficulty, generated.EstimatedDurationMinutes, generated.IsActive,
            generated.IsGenerated, generated.Exercises.Count);
        return profile.ToResponse(summary);
    }

    public async Task<ProfileResponse> UpdateAsync(Guid userId, ProfileRequest request, CancellationToken ct)
    {
        var profile = await db.UserProfiles.SingleOrDefaultAsync(x => x.UserId == userId, ct)
            ?? throw new AppException(404, "Profile was not found.");
        var weightChanged = profile.WeightKg != request.WeightKg;
        Apply(profile, request);
        profile.UpdatedAt = DateTime.UtcNow;
        if (weightChanged)
        {
            db.WeightRecords.Add(new WeightRecord
            {
                Id = Guid.NewGuid(), UserId = userId, WeightKg = request.WeightKg, RecordedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync(ct);
        return profile.ToResponse();
    }

    private static void Apply(UserProfile profile, ProfileRequest request)
    {
        var bmi = BmiCalculator.Calculate(request.HeightCm, request.WeightKg);
        profile.FullName = request.FullName.Trim();
        profile.DateOfBirth = request.DateOfBirth;
        profile.Gender = request.Gender;
        profile.HeightCm = request.HeightCm;
        profile.WeightKg = request.WeightKg;
        profile.TargetWeightKg = request.TargetWeightKg;
        profile.FitnessLevel = request.FitnessLevel;
        profile.Goal = request.Goal;
        profile.TrainingDaysPerWeek = request.TrainingDaysPerWeek;
        profile.PreferredWorkoutDurationMinutes = request.PreferredWorkoutDurationMinutes;
        profile.Bmi = bmi;
        profile.BmiCategory = BmiCalculator.Classify(bmi);
    }
}
