using CoreUp.Application;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreUp.Api.Controllers;

[Authorize]
[Route("api/weight-records")]
public sealed class WeightRecordsController(WeightProgressService service) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WeightRecordResponse>>> Get(CancellationToken ct) =>
        Ok(await service.GetWeightsAsync(CurrentUserId, ct));

    [HttpPost]
    public async Task<ActionResult<WeightRecordResponse>> Add(AddWeightRecordRequest request, CancellationToken ct) =>
        Ok(await service.AddWeightAsync(CurrentUserId, request, ct));
}
