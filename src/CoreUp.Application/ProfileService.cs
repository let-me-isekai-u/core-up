using System.Text.Json;
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

    public async Task<ProfileResponse> AssessAsync(Guid userId, FitnessAssessmentRequest request, CancellationToken ct)
    {
        if (request.UserSelectedLevel is not FitnessLevel selected || !Enum.IsDefined(selected))
            throw new AppException(400, "Hãy xác nhận mức tập của bạn.");
        var result = FitnessAssessmentScoring.Evaluate(request.Answers, selected);
        if (selected > result.RecommendedLevel && !request.ConfirmHigherLevel)
            throw new AppException(400, "Mức chọn cao hơn gợi ý. Hãy đọc và xác nhận cảnh báo trước khi tiếp tục.");
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, ct)
            ?? throw new AppException(404, "User was not found.");
        var profile = await db.UserProfiles.SingleOrDefaultAsync(x => x.UserId == userId, ct);
        var now = DateTime.UtcNow;
        if (profile is null)
        {
            profile = new UserProfile
            {
                Id = Guid.NewGuid(), UserId = userId, FullName = user.Username,
                Goal = FitnessGoal.BuildMuscle, TrainingDaysPerWeek = 3,
                PreferredWorkoutDurationMinutes = 20, CreatedAt = now
            };
            db.UserProfiles.Add(profile);
        }
        if (!string.IsNullOrWhiteSpace(request.FullName)) profile.FullName = request.FullName.Trim();
        profile.FitnessLevel = selected;
        profile.QuestionnaireVersion = FitnessAssessmentScoring.Version;
        profile.QuestionnaireAnswers = JsonSerializer.Serialize(request.Answers);
        profile.QuestionnaireCompletedAt = now;
        profile.UpdatedAt = now;
        // Skipping measurements neither writes fake values nor clears existing measurements.
        if (request.ShareMeasurements)
            ApplyMeasurements(profile, request.HeightCm, request.WeightKg, profile.TargetWeightKg);
        // Replacing a plan only deactivates its old version. Existing session targets and history stay intact.
        var plan = await workoutPlans.ApplyStandardAsync(profile, ct);
        return profile.ToResponse(new(plan.Id, plan.Name, plan.Goal, plan.Difficulty,
            plan.EstimatedDurationMinutes, plan.IsActive, plan.IsGenerated, plan.Exercises.Count));
    }

    public async Task<ProfileResponse> UpdateMeasurementsAsync(Guid userId, MeasurementsRequest request, CancellationToken ct)
    {
        var profile = await db.UserProfiles.SingleOrDefaultAsync(x => x.UserId == userId, ct)
            ?? throw new AppException(404, "Profile was not found.");
        ApplyMeasurements(profile, request.HeightCm, request.WeightKg, request.TargetWeightKg);
        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return profile.ToResponse();
    }

    // Legacy profile endpoints remain available; level changes use the questionnaire endpoint.
    public async Task<ProfileResponse> CreateAsync(Guid userId, ProfileRequest request, CancellationToken ct)
    {
        if (await db.UserProfiles.AnyAsync(x => x.UserId == userId, ct))
            throw new AppException(409, "Profile already exists. Use PUT to update it.");
        if (!await db.Users.AnyAsync(x => x.Id == userId, ct))
            throw new AppException(404, "User was not found.");
        var profile = new UserProfile { Id = Guid.NewGuid(), UserId = userId, CreatedAt = DateTime.UtcNow };
        Apply(profile, request);
        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync(ct);
        // No standard plan until the user explicitly completes the assessment.
        return profile.ToResponse();
    }

    public async Task<ProfileResponse> UpdateAsync(Guid userId, ProfileRequest request, CancellationToken ct)
    {
        var profile = await db.UserProfiles.SingleOrDefaultAsync(x => x.UserId == userId, ct)
            ?? throw new AppException(404, "Profile was not found.");
        if (request.FitnessLevel != profile.FitnessLevel)
            throw new AppException(400, "Đổi mức tập qua questionnaire để xem gợi ý và xác nhận.");
        Apply(profile, request);
        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return profile.ToResponse();
    }

    private void ApplyMeasurements(UserProfile profile, decimal? height, decimal? weight, decimal? target)
    {
        if (weight.HasValue && profile.WeightKg != weight)
            db.WeightRecords.Add(new WeightRecord
            {
                Id = Guid.NewGuid(), UserId = profile.UserId, WeightKg = weight.Value, RecordedAt = DateTime.UtcNow
            });
        profile.HeightCm = height;
        profile.WeightKg = weight;
        profile.TargetWeightKg = target;
        profile.Bmi = height is decimal h && weight is decimal w ? BmiCalculator.Calculate(h, w) : null;
        profile.BmiCategory = profile.Bmi is decimal bmi ? BmiCalculator.Classify(bmi) : null;
    }

    private void Apply(UserProfile profile, ProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) || !Enum.IsDefined(request.FitnessLevel) || !Enum.IsDefined(request.Goal))
            throw new AppException(400, "Tên và mức tập/mục tiêu không hợp lệ.");
        profile.FullName = request.FullName.Trim();
        profile.DateOfBirth = request.DateOfBirth;
        profile.Gender = request.Gender;
        ApplyMeasurements(profile, request.HeightCm, request.WeightKg, request.TargetWeightKg);
        profile.FitnessLevel = request.FitnessLevel;
        profile.Goal = request.Goal;
        profile.TrainingDaysPerWeek = request.TrainingDaysPerWeek;
        profile.PreferredWorkoutDurationMinutes = request.PreferredWorkoutDurationMinutes;
    }
}
