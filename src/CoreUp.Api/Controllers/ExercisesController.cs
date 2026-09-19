using CoreUp.Application;
using CoreUp.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreUp.Api.Controllers;

[Authorize]
[Route("api/exercises")]
public sealed class ExercisesController(ExerciseService exercises) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExerciseResponse>>> Get(
        [FromQuery] FitnessLevel? difficulty, [FromQuery] string? muscleGroup, CancellationToken ct) =>
        Ok(await exercises.GetAsync(difficulty, muscleGroup, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ExerciseResponse>> GetById(Guid id, CancellationToken ct) =>
        Ok(await exercises.GetByIdAsync(id, ct));
}
