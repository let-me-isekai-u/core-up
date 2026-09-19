using CoreUp.Domain;
using Microsoft.EntityFrameworkCore;

namespace CoreUp.Application;

public sealed class ExerciseService(IApplicationDbContext db)
{
    public async Task<IReadOnlyList<ExerciseResponse>> GetAsync(
        FitnessLevel? difficulty, string? muscleGroup, CancellationToken ct)
    {
        var query = db.Exercises.AsNoTracking().Where(x => x.IsActive);
        if (difficulty.HasValue) query = query.Where(x => x.Difficulty == difficulty.Value);
        if (!string.IsNullOrWhiteSpace(muscleGroup))
        {
            var value = muscleGroup.Trim().ToLower();
            query = query.Where(x => x.MuscleGroup.ToLower() == value);
        }

        return await query.OrderBy(x => x.Name).Select(x => x.ToResponse()).ToListAsync(ct);
    }

    public async Task<ExerciseResponse> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var exercise = await db.Exercises.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.IsActive, ct)
            ?? throw new AppException(404, "Exercise was not found.");
        return exercise.ToResponse();
    }
}
