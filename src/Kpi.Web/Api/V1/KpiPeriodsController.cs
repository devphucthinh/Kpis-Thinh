using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Periods;
using Microsoft.AspNetCore.Mvc;
using Kpi.Web.Errors;

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
        var result = operations.Create(actor.Current, request.Code, request.Name ?? request.Code, request.Description ?? request.Code, request.Cadence, request.StartsAt, request.EndsAt);
        return result.IsSuccess ? Created($"/api/v1/kpi-periods/{result.Value!.Id}", result.Value) : ProblemDetailsMapper.ToResult(result.Error!, actor.Current.CorrelationId);
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
    [HttpPost("{id:guid}/amendments")]
    public IActionResult ProposeAmendment(Guid id, [FromBody] ProposeAmendmentRequest request) => ToAction(operations.ProposeAmendment(actor.Current, id, request.Selections, request.Reason, request.StartsAt, request.EndsAt));
    [HttpPost("{id:guid}/amendments/{amendmentId:guid}/review")]
    public IActionResult ReviewAmendment(Guid id, Guid amendmentId, [FromBody] ReviewAmendmentRequest request) => ToAction(operations.ReviewAmendment(actor.Current, id, amendmentId, request.Approve, request.Comment));
    [HttpPost("{id:guid}/activate")]
    public IActionResult Activate(Guid id) => ToAction(operations.Activate(actor.Current, id));
    [HttpPost("{id:guid}/close")]
    public IActionResult Close(Guid id) => ToAction(operations.Close(actor.Current, id));
    private IActionResult ToAction<T>(ApplicationResult<T> result) => result.IsSuccess ? Ok(result.Value) : ProblemDetailsMapper.ToResult(result.Error!, actor.Current.CorrelationId);
}

public sealed record CreatePeriodRequest(string Code, DateTimeOffset StartsAt, DateTimeOffset EndsAt, string? Name = null, string? Description = null, KpiCadence Cadence = KpiCadence.Monthly);
public sealed record SelectVersionRequest(Guid DefinitionId, Guid VersionId);
public sealed record RejectPeriodRequest(string Comment);
public sealed record AmendPeriodRequest(Guid ApproverId, IReadOnlyDictionary<Guid, Guid> Selections, string Reason);
public sealed record ProposeAmendmentRequest(IReadOnlyDictionary<Guid, Guid> Selections, string Reason, DateTimeOffset? StartsAt = null, DateTimeOffset? EndsAt = null);
public sealed record ReviewAmendmentRequest(bool Approve, string Comment);
