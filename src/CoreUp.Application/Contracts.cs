using System.ComponentModel.DataAnnotations;
using CoreUp.Domain;

namespace CoreUp.Application;

public sealed class RegisterRequest
{
    [Required, RegularExpression(@"[A-Za-z0-9_]{3,24}", ErrorMessage = "Tên tài khoản cần 3–24 ký tự Latin, số hoặc dấu gạch dưới.")]
    public string Username { get; init; } = string.Empty;
    [Required, EmailAddress, MaxLength(320)] public string Email { get; init; } = string.Empty;
    [Required, MinLength(8), MaxLength(128)] public string Password { get; init; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required, MaxLength(24)] public string Username { get; init; } = string.Empty;
    [Required, MaxLength(128)] public string Password { get; init; } = string.Empty;
}

public sealed record AuthResponse(Guid UserId, string Email, string AccessToken, bool NeedsProfile);

public sealed class ProfileRequest
{
    [Required, MaxLength(150)] public string FullName { get; init; } = string.Empty;
    public DateOnly? DateOfBirth { get; init; }
    public Gender? Gender { get; init; }
    [Range(100, 250)] public decimal HeightCm { get; init; }
    [Range(30, 300)] public decimal WeightKg { get; init; }
    [Range(30, 300)] public decimal? TargetWeightKg { get; init; }
    public FitnessLevel FitnessLevel { get; init; }
    public FitnessGoal Goal { get; init; }
    [Range(1, 7)] public int TrainingDaysPerWeek { get; init; }
    [Range(10, 180)] public int PreferredWorkoutDurationMinutes { get; init; }
}

public sealed record ProfileResponse(
    Guid Id, string FullName, DateOnly? DateOfBirth, Gender? Gender,
    decimal HeightCm, decimal WeightKg, decimal? TargetWeightKg,
    FitnessLevel FitnessLevel, FitnessGoal Goal, int TrainingDaysPerWeek,
    int PreferredWorkoutDurationMinutes, decimal Bmi, BmiCategory BmiCategory,
    WorkoutPlanSummaryResponse? GeneratedPlan = null);

public sealed record ExerciseResponse(
    Guid Id, string Name, string Slug, string? Description, string? Instructions,
    string MuscleGroup, FitnessLevel Difficulty, ExerciseType ExerciseType,
    int? DefaultSets, int? DefaultReps, int? DefaultDurationSeconds,
    int DefaultRestSeconds, string? ImageUrl);

public sealed record WorkoutPlanSummaryResponse(
    Guid Id, string Name, FitnessGoal Goal, FitnessLevel Difficulty,
    int EstimatedDurationMinutes, bool IsActive, bool IsGenerated, int ExerciseCount);

public sealed record WorkoutPlanExerciseResponse(
    Guid Id, int Order, ExerciseResponse Exercise, int Sets, int? Reps,
    int? DurationSeconds, int RestSeconds, string? Notes);

public sealed record WorkoutPlanResponse(
    Guid Id, string Name, string? Description, FitnessGoal Goal,
    FitnessLevel Difficulty, int EstimatedDurationMinutes, bool IsActive,
    bool IsGenerated, IReadOnlyList<WorkoutPlanExerciseResponse> Exercises);

public sealed class StartWorkoutRequest
{
    public Guid WorkoutPlanId { get; init; }
}

public sealed class UpdateWorkoutExerciseRequest
{
    [Range(0, 100)] public int CompletedSets { get; init; }
    [Range(0, 10000)] public int CompletedReps { get; init; }
    [Range(0, 86400)] public int CompletedDurationSeconds { get; init; }
    public bool IsCompleted { get; init; }
}

public sealed record WorkoutSessionExerciseResponse(
    Guid Id, int Order, ExerciseResponse Exercise, int TargetSets,
    int CompletedSets, int? TargetReps, int CompletedReps,
    int? TargetDurationSeconds, int CompletedDurationSeconds,
    int RestSeconds, bool IsCompleted);

public sealed record WorkoutSessionResponse(
    Guid Id, Guid? WorkoutPlanId, string? WorkoutName, DateTime StartedAt,
    DateTime? CompletedAt, WorkoutSessionStatus Status, int DurationSeconds,
    int TotalExercises, int CompletedExercises, int TotalSets,
    int CompletedSets, int TotalReps,
    IReadOnlyList<WorkoutSessionExerciseResponse> Exercises);

public sealed record WorkoutHistoryResponse(
    Guid Id, string? WorkoutName, DateTime StartedAt, DateTime? CompletedAt,
    int DurationSeconds, int CompletedExercises, int TotalExercises,
    int CompletedSets, int TotalReps);

public sealed class AddWeightRecordRequest
{
    [Range(30, 300)] public decimal WeightKg { get; init; }
}

public sealed record WeightRecordResponse(Guid Id, decimal WeightKg, DateTime RecordedAt);
public sealed record WeightHistoryPoint(DateOnly Date, decimal WeightKg);
public sealed record ProgressSummaryResponse(
    decimal? CurrentWeightKg, decimal? TargetWeightKg, int TotalWorkouts,
    int WorkoutsThisWeek, int TotalTrainingMinutes, int CurrentStreakDays,
    IReadOnlyList<WeightHistoryPoint> WeightHistory);
