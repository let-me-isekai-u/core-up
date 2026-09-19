using CoreUp.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreUp.Api.Controllers;

[Authorize]
[Route("api/workout-plans")]
public sealed class WorkoutPlansController(WorkoutPlanService plans) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkoutPlanSummaryResponse>>> Get(CancellationToken ct) =>
        Ok(await plans.GetAllAsync(CurrentUserId, ct));

    [HttpGet("active")]
    public async Task<ActionResult<WorkoutPlanResponse>> GetActive(CancellationToken ct) =>
        Ok(await plans.GetActiveAsync(CurrentUserId, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkoutPlanResponse>> GetById(Guid id, CancellationToken ct) =>
        Ok(await plans.GetByIdAsync(CurrentUserId, id, ct));

    [HttpPost("generate")]
    public async Task<ActionResult<WorkoutPlanResponse>> Generate(CancellationToken ct) =>
        Ok(await plans.GenerateAsync(CurrentUserId, ct));
}
