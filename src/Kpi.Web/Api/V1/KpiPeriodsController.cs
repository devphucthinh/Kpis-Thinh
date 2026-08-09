using Kpi.Application;
using Kpi.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Kpi.Web.Api.V1;

[ApiController]
[Route("api/v1/kpi-periods")]
public sealed class KpiPeriodsController(PeriodOperations operations, ICurrentActor actor) : ControllerBase
{
    [HttpGet]
    public IActionResult List() => Ok(operations.List().Select(x => new { id = x.Id, code = x.Code, startsAt = x.StartsAt, endsAt = x.EndsAt, status = x.Status.ToString(), latestEffectiveRevision = x.LatestEffectiveRevision, selections = x.SelectedVersions }));

    [HttpPost]
    public IActionResult Create([FromBody] CreatePeriodRequest request)
    {
        var result = operations.Create(actor.Current, request.Code, request.StartsAt, request.EndsAt);
        return result.IsSuccess ? Created($"/api/v1/kpi-periods/{result.Value!.Id}", result.Value) : StatusCode(result.Error!.Status, result.Error);
    }

    [HttpPost("{id:guid}/selections")]
    public IActionResult Select(Guid id, [FromBody] SelectVersionRequest request) => ToAction(operations.Select(actor.Current, id, request.DefinitionId, request.VersionId));
    [HttpPost("{id:guid}/submit")]
    public IActionResult Submit(Guid id) => ToAction(operations.Submit(actor.Current, id));
    [HttpPost("{id:guid}/approve")]
    public IActionResult Approve(Guid id) => ToAction(operations.Approve(actor.Current, id));
    [HttpPost("{id:guid}/reject")]
    public IActionResult Reject(Guid id, [FromBody] RejectPeriodRequest request) => ToAction(operations.Reject(actor.Current, id, request.Comment));
    [HttpPost("{id:guid}/return-to-draft")]
    public IActionResult ReturnToDraft(Guid id) => ToAction(operations.ReturnToDraft(actor.Current, id));
    [HttpPost("{id:guid}/amend")]
    public IActionResult Amend(Guid id, [FromBody] AmendPeriodRequest request) => ToAction(operations.Amend(actor.Current, id, request.ApproverId, request.Selections, request.Reason));
    [HttpPost("{id:guid}/activate")]
    public IActionResult Activate(Guid id) => ToAction(operations.Activate(actor.Current, id));
    [HttpPost("{id:guid}/close")]
    public IActionResult Close(Guid id) => ToAction(operations.Close(actor.Current, id));
    private IActionResult ToAction<T>(ApplicationResult<T> result) => result.IsSuccess ? Ok(result.Value) : StatusCode(result.Error!.Status, result.Error);
}

public sealed record CreatePeriodRequest(string Code, DateTimeOffset StartsAt, DateTimeOffset EndsAt);
public sealed record SelectVersionRequest(Guid DefinitionId, Guid VersionId);
public sealed record RejectPeriodRequest(string Comment);
public sealed record AmendPeriodRequest(Guid ApproverId, IReadOnlyDictionary<Guid, Guid> Selections, string Reason);
