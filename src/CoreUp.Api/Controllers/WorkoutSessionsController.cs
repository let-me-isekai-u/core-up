using CoreUp.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreUp.Api.Controllers;

[Authorize]
[Route("api/workout-sessions")]
public sealed class WorkoutSessionsController(WorkoutSessionService sessions) : ApiControllerBase
{
    [HttpPost("start")]
    public async Task<ActionResult<WorkoutSessionResponse>> Start(StartWorkoutRequest request, CancellationToken ct) =>
        Ok(await sessions.StartAsync(CurrentUserId, request, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WorkoutSessionResponse>> Get(Guid id, CancellationToken ct) =>
        Ok(await sessions.GetAsync(CurrentUserId, id, ct));

    [HttpPut("{sessionId:guid}/exercises/{sessionExerciseId:guid}")]
    public async Task<ActionResult<WorkoutSessionResponse>> UpdateExercise(
        Guid sessionId, Guid sessionExerciseId, UpdateWorkoutExerciseRequest request, CancellationToken ct) =>
        Ok(await sessions.UpdateExerciseAsync(CurrentUserId, sessionId, sessionExerciseId, request, ct));

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<WorkoutSessionResponse>> Complete(Guid id, CancellationToken ct) =>
        Ok(await sessions.CompleteAsync(CurrentUserId, id, ct));

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<WorkoutSessionResponse>> Cancel(Guid id, CancellationToken ct) =>
        Ok(await sessions.CancelAsync(CurrentUserId, id, ct));

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<WorkoutHistoryResponse>>> History(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await sessions.HistoryAsync(CurrentUserId, page, pageSize, ct));
}
