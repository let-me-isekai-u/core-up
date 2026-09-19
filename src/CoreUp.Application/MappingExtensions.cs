using CoreUp.Domain;

namespace CoreUp.Application;

internal static class MappingExtensions
{
    public static ExerciseResponse ToResponse(this Exercise value) => new(
        value.Id, value.Name, value.Slug, value.Description, value.Instructions,
        value.MuscleGroup, value.Difficulty, value.ExerciseType,
        value.DefaultSets, value.DefaultReps, value.DefaultDurationSeconds,
        value.DefaultRestSeconds, value.ImageUrl);

    public static ProfileResponse ToResponse(this UserProfile value, WorkoutPlanSummaryResponse? plan = null) => new(
        value.Id, value.FullName, value.DateOfBirth, value.Gender,
        value.HeightCm, value.WeightKg, value.TargetWeightKg,
        value.FitnessLevel, value.Goal, value.TrainingDaysPerWeek,
        value.PreferredWorkoutDurationMinutes, value.Bmi, value.BmiCategory, plan);
}
