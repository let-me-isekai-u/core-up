using CoreUp.Application;
using CoreUp.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreUp.Infrastructure.Persistence;

public sealed class CoreUpDbContext(DbContextOptions<CoreUpDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<UserExternalLogin> ExternalLogins => Set<UserExternalLogin>();
    public DbSet<PremiumEntitlement> PremiumEntitlements => Set<PremiumEntitlement>();
    public DbSet<AdminAuditEvent> AdminAuditEvents => Set<AdminAuditEvent>();

    public DbSet<EmailChallenge> EmailChallenges => Set<EmailChallenge>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<WorkoutPlan> WorkoutPlans => Set<WorkoutPlan>();
    public DbSet<WorkoutPlanExercise> WorkoutPlanExercises => Set<WorkoutPlanExercise>();
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
    public DbSet<WorkoutSessionExercise> WorkoutSessionExercises => Set<WorkoutSessionExercise>();
    public DbSet<WeightRecord> WeightRecords => Set<WeightRecord>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (ChangeTracker.Entries<AdminAuditEvent>().Any(e => e.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("Audit events are append-only.");
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        AccountAccessModel.Configure(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users", t => t.HasCheckConstraint("CK_users_role", "\"Role\" IN ('User','Admin')"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenVersion).IsConcurrencyToken();
            entity.Property(x => x.Username).HasMaxLength(24).IsRequired();
            entity.Property(x => x.NormalizedUsername).HasMaxLength(24)
                .HasComputedColumnSql("lower(\"Username\")", stored: true);
            entity.HasIndex(x => x.NormalizedUsername).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(1000);
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(16).HasDefaultValue(ApplicationRole.User);
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<EmailChallenge>(entity =>
        {
            entity.ToTable("email_challenges");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Purpose).HasMaxLength(20).IsRequired();
            entity.Property(x => x.CodeHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ResetGrantHash).HasMaxLength(64);
            entity.Property(x => x.Revision).IsConcurrencyToken();
            entity.HasIndex(x => new { x.UserId, x.Purpose }).IsUnique();
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("user_profiles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            entity.Property(x => x.HeightCm).HasPrecision(6, 2);
            entity.Property(x => x.WeightKg).HasPrecision(6, 2);
            entity.Property(x => x.TargetWeightKg).HasPrecision(6, 2);
            entity.Property(x => x.Bmi).HasPrecision(5, 2);
            entity.Property(x => x.Gender).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.FitnessLevel).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Goal).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.BmiCategory).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.HasOne(x => x.User).WithOne(x => x.Profile).HasForeignKey<UserProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.ToTable("exercises");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(160).IsRequired();
            entity.Property(x => x.MuscleGroup).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Difficulty).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.ExerciseType).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.ImageUrl).HasMaxLength(1000);
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasData(ExerciseSeed.All);
        });

        modelBuilder.Entity<WorkoutPlan>(entity =>
        {
            entity.ToTable("workout_plans");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Goal).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Difficulty).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => new { x.UserId, x.IsActive });
            entity.HasOne(x => x.User).WithMany(x => x.WorkoutPlans).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkoutPlanExercise>(entity =>
        {
            entity.ToTable("workout_plan_exercises");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Notes).HasMaxLength(500);
            entity.HasIndex(x => new { x.WorkoutPlanId, x.Order }).IsUnique();
            entity.HasOne(x => x.WorkoutPlan).WithMany(x => x.Exercises).HasForeignKey(x => x.WorkoutPlanId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Exercise).WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkoutSession>(entity =>
        {
            entity.ToTable("workout_sessions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => new { x.UserId, x.StartedAt });
            entity.HasOne(x => x.User).WithMany(x => x.WorkoutSessions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.WorkoutPlan).WithMany().HasForeignKey(x => x.WorkoutPlanId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<WorkoutSessionExercise>(entity =>
        {
            entity.ToTable("workout_session_exercises");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.WorkoutSessionId, x.Order }).IsUnique();
            entity.HasOne(x => x.WorkoutSession).WithMany(x => x.Exercises).HasForeignKey(x => x.WorkoutSessionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Exercise).WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WeightRecord>(entity =>
        {
            entity.ToTable("weight_records");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.WeightKg).HasPrecision(6, 2);
            entity.HasIndex(x => new { x.UserId, x.RecordedAt });
            entity.HasOne(x => x.User).WithMany(x => x.WeightRecords).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
