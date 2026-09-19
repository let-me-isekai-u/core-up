using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CoreUp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Instructions = table.Column<string>(type: "text", nullable: true),
                    MuscleGroup = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Difficulty = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExerciseType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DefaultSets = table.Column<int>(type: "integer", nullable: true),
                    DefaultReps = table.Column<int>(type: "integer", nullable: true),
                    DefaultDurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    DefaultRestSeconds = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exercises", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    HeightCm = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    TargetWeightKg = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    FitnessLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Goal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TrainingDaysPerWeek = table.Column<int>(type: "integer", nullable: false),
                    PreferredWorkoutDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    Bmi = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    BmiCategory = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_profiles_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "weight_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    WeightKg = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_weight_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_weight_records_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Goal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Difficulty = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EstimatedDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsGenerated = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workout_plans_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_plan_exercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Sets = table.Column<int>(type: "integer", nullable: false),
                    Reps = table.Column<int>(type: "integer", nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    RestSeconds = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_plan_exercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workout_plan_exercises_exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workout_plan_exercises_workout_plans_WorkoutPlanId",
                        column: x => x.WorkoutPlanId,
                        principalTable: "workout_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    TotalExercises = table.Column<int>(type: "integer", nullable: false),
                    CompletedExercises = table.Column<int>(type: "integer", nullable: false),
                    TotalSets = table.Column<int>(type: "integer", nullable: false),
                    CompletedSets = table.Column<int>(type: "integer", nullable: false),
                    TotalReps = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workout_sessions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_workout_sessions_workout_plans_WorkoutPlanId",
                        column: x => x.WorkoutPlanId,
                        principalTable: "workout_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "workout_session_exercises",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkoutSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    TargetSets = table.Column<int>(type: "integer", nullable: false),
                    CompletedSets = table.Column<int>(type: "integer", nullable: false),
                    TargetReps = table.Column<int>(type: "integer", nullable: true),
                    CompletedReps = table.Column<int>(type: "integer", nullable: false),
                    TargetDurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    CompletedDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    RestSeconds = table.Column<int>(type: "integer", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workout_session_exercises", x => x.Id);
                    table.ForeignKey(
                        name: "FK_workout_session_exercises_exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_workout_session_exercises_workout_sessions_WorkoutSessionId",
                        column: x => x.WorkoutSessionId,
                        principalTable: "workout_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "exercises",
                columns: new[] { "Id", "CreatedAt", "DefaultDurationSeconds", "DefaultReps", "DefaultRestSeconds", "DefaultSets", "Description", "Difficulty", "ExerciseType", "ImageUrl", "Instructions", "IsActive", "MuscleGroup", "Name", "Slug" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 12, 45, 3, "A controlled squat using body weight.", "Beginner", "Repetition", null, "Keep the chest tall and push the knees in line with the toes.", true, "Legs", "Bodyweight Squat", "bodyweight-squat" },
                    { new Guid("00000000-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 10, 45, 3, "Alternating forward lunges for legs and balance.", "Beginner", "Repetition", null, "Step forward, lower both knees, then drive back.", true, "Legs", "Forward Lunge", "forward-lunge" },
                    { new Guid("00000000-0000-0000-0000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 12, 45, 3, "Hip extension exercise performed on the floor.", "Beginner", "Repetition", null, "Brace the core and squeeze the glutes at the top.", true, "Glutes", "Glute Bridge", "glute-bridge" },
                    { new Guid("00000000-0000-0000-0000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 15, 45, 3, "Standing calf strengthening exercise.", "Beginner", "Repetition", null, "Rise onto the balls of the feet and lower slowly.", true, "Calves", "Calf Raise", "calf-raise" },
                    { new Guid("00000000-0000-0000-0000-000000000005"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, null, 45, 3, "Static squat hold against a wall.", "Beginner", "Duration", null, "Keep knees near ninety degrees and the back supported.", true, "Legs", "Wall Sit", "wall-sit" },
                    { new Guid("00000000-0000-0000-0000-000000000006"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 10, 45, 3, "Classic upper-body pushing exercise.", "Beginner", "Repetition", null, "Keep a straight line from head to heels.", true, "Chest", "Push-up", "push-up" },
                    { new Guid("00000000-0000-0000-0000-000000000007"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 10, 45, 3, "Modified push-up supported on the knees.", "Beginner", "Repetition", null, "Brace the core and lower the chest with control.", true, "Chest", "Knee Push-up", "knee-push-up" },
                    { new Guid("00000000-0000-0000-0000-000000000008"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 10, 45, 3, "Push-up using an elevated stable surface.", "Beginner", "Repetition", null, "Keep the body straight and bring the chest toward the surface.", true, "Chest", "Incline Push-up", "incline-push-up" },
                    { new Guid("00000000-0000-0000-0000-000000000009"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 10, 45, 3, "Bodyweight dip emphasizing the triceps.", "Beginner", "Repetition", null, "Use a stable surface and keep shoulders away from ears.", true, "Arms", "Triceps Dip", "triceps-dip" },
                    { new Guid("00000000-0000-0000-0000-000000000010"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 12, 45, 3, "Floor exercise for the posterior chain.", "Beginner", "Repetition", null, "Lift arms and legs gently without forcing the lower back.", true, "Back", "Superman", "superman" },
                    { new Guid("00000000-0000-0000-0000-000000000011"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 10, 45, 3, "Contralateral core stability exercise.", "Beginner", "Repetition", null, "Extend opposite arm and leg while keeping hips level.", true, "Core", "Bird Dog", "bird-dog" },
                    { new Guid("00000000-0000-0000-0000-000000000012"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, null, 45, 3, "Static core bracing exercise.", "Beginner", "Duration", null, "Keep ribs down and the body in a straight line.", true, "Core", "Plank", "plank" },
                    { new Guid("00000000-0000-0000-0000-000000000013"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 25, null, 45, 3, "Lateral core stability hold.", "Beginner", "Duration", null, "Stack the shoulders and keep the hips lifted.", true, "Core", "Side Plank", "side-plank" },
                    { new Guid("00000000-0000-0000-0000-000000000014"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 15, 45, 3, "Short-range abdominal flexion exercise.", "Beginner", "Repetition", null, "Lift the shoulder blades without pulling the neck.", true, "Core", "Crunch", "crunch" },
                    { new Guid("00000000-0000-0000-0000-000000000015"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 16, 45, 3, "Rotational abdominal exercise.", "Beginner", "Repetition", null, "Rotate the torso slowly toward the opposite knee.", true, "Core", "Bicycle Crunch", "bicycle-crunch" },
                    { new Guid("00000000-0000-0000-0000-000000000016"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 12, 45, 3, "Lower abdominal curl performed on the floor.", "Beginner", "Repetition", null, "Roll the pelvis up without using momentum.", true, "Core", "Reverse Crunch", "reverse-crunch" },
                    { new Guid("00000000-0000-0000-0000-000000000017"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 10, 45, 3, "Straight-leg core strengthening exercise.", "Beginner", "Repetition", null, "Keep the lower back controlled against the floor.", true, "Core", "Leg Raise", "leg-raise" },
                    { new Guid("00000000-0000-0000-0000-000000000018"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, null, 45, 3, "Dynamic plank cardio exercise.", "Beginner", "Duration", null, "Drive alternating knees forward while keeping hips stable.", true, "Cardio", "Mountain Climber", "mountain-climber" },
                    { new Guid("00000000-0000-0000-0000-000000000019"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, null, 45, 3, "Full-body rhythmic cardio movement.", "Beginner", "Duration", null, "Land softly and maintain a steady pace.", true, "Cardio", "Jumping Jack", "jumping-jack" },
                    { new Guid("00000000-0000-0000-0000-000000000020"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, null, 45, 3, "Running-in-place cardio drill.", "Beginner", "Duration", null, "Lift knees comfortably and land lightly.", true, "Cardio", "High Knees", "high-knees" },
                    { new Guid("00000000-0000-0000-0000-000000000021"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 10, 45, 3, "Step onto a stable elevated surface.", "Beginner", "Repetition", null, "Drive through the lead foot and control the descent.", true, "Legs", "Step-up", "step-up" },
                    { new Guid("00000000-0000-0000-0000-000000000022"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 8, 45, 3, "High-intensity full-body conditioning movement.", "Intermediate", "Repetition", null, "Move through squat, plank and stand with controlled landings.", true, "Full Body", "Burpee", "burpee" },
                    { new Guid("00000000-0000-0000-0000-000000000023"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 10, 45, 3, "Supine core stability exercise.", "Beginner", "Repetition", null, "Lower opposite arm and leg while keeping the back stable.", true, "Core", "Dead Bug", "dead-bug" },
                    { new Guid("00000000-0000-0000-0000-000000000024"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 10, 45, 3, "Gentle spinal mobility sequence.", "Beginner", "Repetition", null, "Alternate spinal flexion and extension with the breath.", true, "Mobility", "Cat-Cow", "cat-cow" },
                    { new Guid("00000000-0000-0000-0000-000000000025"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 30, null, 45, 3, "Resting mobility pose for back and hips.", "Beginner", "Duration", null, "Sit hips toward heels and reach forward comfortably.", true, "Mobility", "Child's Pose", "childs-pose" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_exercises_Slug",
                table: "exercises",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_profiles_UserId",
                table: "user_profiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_weight_records_UserId_RecordedAt",
                table: "weight_records",
                columns: new[] { "UserId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_workout_plan_exercises_ExerciseId",
                table: "workout_plan_exercises",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_workout_plan_exercises_WorkoutPlanId_Order",
                table: "workout_plan_exercises",
                columns: new[] { "WorkoutPlanId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_plans_UserId_IsActive",
                table: "workout_plans",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_workout_session_exercises_ExerciseId",
                table: "workout_session_exercises",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_workout_session_exercises_WorkoutSessionId_Order",
                table: "workout_session_exercises",
                columns: new[] { "WorkoutSessionId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_UserId_StartedAt",
                table: "workout_sessions",
                columns: new[] { "UserId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_workout_sessions_WorkoutPlanId",
                table: "workout_sessions",
                column: "WorkoutPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_profiles");

            migrationBuilder.DropTable(
                name: "weight_records");

            migrationBuilder.DropTable(
                name: "workout_plan_exercises");

            migrationBuilder.DropTable(
                name: "workout_session_exercises");

            migrationBuilder.DropTable(
                name: "exercises");

            migrationBuilder.DropTable(
                name: "workout_sessions");

            migrationBuilder.DropTable(
                name: "workout_plans");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
