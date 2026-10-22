namespace CoreUp.Domain;

public sealed class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string NormalizedUsername { get; private set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public UserProfile? Profile { get; set; }
    public ICollection<WorkoutPlan> WorkoutPlans { get; set; } = [];
    public ICollection<WorkoutSession> WorkoutSessions { get; set; } = [];
    public ICollection<WeightRecord> WeightRecords { get; set; } = [];
}

public sealed class UserProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public decimal HeightCm { get; set; }
    public decimal WeightKg { get; set; }
    public decimal? TargetWeightKg { get; set; }
    public FitnessLevel FitnessLevel { get; set; }
    public FitnessGoal Goal { get; set; }
    public int TrainingDaysPerWeek { get; set; }
    public int PreferredWorkoutDurationMinutes { get; set; }
    public decimal Bmi { get; set; }
    public BmiCategory BmiCategory { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public User User { get; set; } = null!;
}

public sealed class Exercise
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public string MuscleGroup { get; set; } = string.Empty;
    public FitnessLevel Difficulty { get; set; }
    public ExerciseType ExerciseType { get; set; }
    public int? DefaultSets { get; set; }
    public int? DefaultReps { get; set; }
    public int? DefaultDurationSeconds { get; set; }
    public int DefaultRestSeconds { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class WorkoutPlan
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public FitnessGoal Goal { get; set; }
    public FitnessLevel Difficulty { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public bool IsActive { get; set; }
    public bool IsGenerated { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public User User { get; set; } = null!;
    public ICollection<WorkoutPlanExercise> Exercises { get; set; } = [];
}

public sealed class WorkoutPlanExercise
{
    public Guid Id { get; set; }
    public Guid WorkoutPlanId { get; set; }
    public Guid ExerciseId { get; set; }
    public int Order { get; set; }
    public int Sets { get; set; }
    public int? Reps { get; set; }
    public int? DurationSeconds { get; set; }
    public int RestSeconds { get; set; }
    public string? Notes { get; set; }
    public WorkoutPlan WorkoutPlan { get; set; } = null!;
    public Exercise Exercise { get; set; } = null!;
}

public sealed class WorkoutSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? WorkoutPlanId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public WorkoutSessionStatus Status { get; set; }
    public int DurationSeconds { get; set; }
    public int TotalExercises { get; set; }
    public int CompletedExercises { get; set; }
    public int TotalSets { get; set; }
    public int CompletedSets { get; set; }
    public int TotalReps { get; set; }
    public User User { get; set; } = null!;
    public WorkoutPlan? WorkoutPlan { get; set; }
    public ICollection<WorkoutSessionExercise> Exercises { get; set; } = [];
}

public sealed class WorkoutSessionExercise
{
    public Guid Id { get; set; }
    public Guid WorkoutSessionId { get; set; }
    public Guid ExerciseId { get; set; }
    public int Order { get; set; }
    public int TargetSets { get; set; }
    public int CompletedSets { get; set; }
    public int? TargetReps { get; set; }
    public int CompletedReps { get; set; }
    public int? TargetDurationSeconds { get; set; }
    public int CompletedDurationSeconds { get; set; }
    public int RestSeconds { get; set; }
    public bool IsCompleted { get; set; }
    public WorkoutSession WorkoutSession { get; set; } = null!;
    public Exercise Exercise { get; set; } = null!;
}

public sealed class WeightRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal WeightKg { get; set; }
    public DateTime RecordedAt { get; set; }
    public User User { get; set; } = null!;
}
