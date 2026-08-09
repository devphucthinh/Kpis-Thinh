using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Formula;
using Kpi.Web.Errors;
using Microsoft.AspNetCore.Mvc;

namespace Kpi.Web.Api.V1;

[ApiController]
[Route("api/v1/kpis")]
public sealed class KpiDefinitionsController(KpiOperations operations, ICurrentActor actor) : ControllerBase
{
    [HttpGet]
    public IActionResult List() => Ok(operations.List().Select(x => new { id = x.Id, code = x.Code.Value, name = x.Name, description = x.Description, archived = x.Archived, versions = x.Versions.Select(v => new { id = v.Id, number = v.VersionNumber, status = v.Status.ToString(), formula = new { source = v.Formula.Source, ast = v.Formula.Ast }, formulaLanguageVersion = v.Formula.LanguageVersion, astSchemaVersion = v.Formula.AstSchemaVersion }) }));

    [HttpPost]
    public IActionResult Create([FromBody] CreateKpiRequest request)
    {
        var result = operations.CreateDefinition(actor.Current, request.Code, request.Name, request.Description);
        return result.IsSuccess ? Created($"/api/v1/kpis/{result.Value!.Id}", result.Value) : ProblemDetailsMapper.ToResult(result.Error!, actor.Current.CorrelationId);
    }

    [HttpPost("{id:guid}/versions")]
    public IActionResult CreateVersion(Guid id, [FromBody] CreateVersionRequest request)
    {
        var variables = request.Variables.Select((v, i) => FormulaVariableDefinition.Create(v.Code, v.DisplayName ?? v.Code, ParseType(v.Type), v.Required, v.DefaultValue, i, v.Description)).ToArray();
        var result = operations.CreateVersion(actor.Current, id, request.Name, request.Description, request.Source, variables, ParseResultType(request.ResultType), request.ChangeSummary);
        return result.IsSuccess ? Ok(result.Value) : ProblemDetailsMapper.ToResult(result.Error!, actor.Current.CorrelationId);
    }

    [HttpPut("{id:guid}/versions/{versionId:guid}/draft")]
    public IActionResult UpdateDraft(Guid id, Guid versionId, [FromBody] UpdateDraftRequest request)
    {
        var variables = request.Variables.Select((v, i) => FormulaVariableDefinition.Create(v.Code, v.DisplayName ?? v.Code, ParseType(v.Type), v.Required, v.DefaultValue, i, v.Description)).ToArray();
        return ToAction(operations.UpdateDraft(actor.Current, id, versionId, request.Name, request.Description, request.Source, variables, new ConcurrencyToken(request.ConcurrencyToken)));
    }

    [HttpPost("{id:guid}/versions/{versionId:guid}/submit")]
    public IActionResult Submit(Guid id, Guid versionId) => ToAction(operations.SubmitVersion(actor.Current, id, versionId));
    [HttpPost("{id:guid}/versions/{versionId:guid}/review")]
    public IActionResult Review(Guid id, Guid versionId, [FromBody] ReviewRequest request) => ToAction(operations.ReviewVersion(actor.Current, id, versionId, request.Approve, request.Comment));
    [HttpPost("{id:guid}/versions/{versionId:guid}/publish")]
    public IActionResult Publish(Guid id, Guid versionId, [FromBody] PublishRequest request) => ToAction(operations.PublishVersion(actor.Current, id, versionId, request.EffectiveFrom));
    [HttpPost("{id:guid}/archive")]
    public IActionResult Archive(Guid id) => ToAction(operations.Archive(actor.Current, id));
    [HttpPost("{id:guid}/restore")]
    public IActionResult Restore(Guid id) => ToAction(operations.Restore(actor.Current, id));
    [HttpPost("{id:guid}/transfer")]
    public IActionResult Transfer(Guid id, [FromBody] TransferRequest request) => ToAction(operations.TransferOwnership(actor.Current, id, request.NewOwnerId, request.Reason));
    [HttpPost("{id:guid}/versions/{versionId:guid}/return-to-draft")]
    public IActionResult ReturnToDraft(Guid id, Guid versionId) => ToAction(operations.ReturnVersionToDraft(actor.Current, id, versionId));
    [HttpPost("{id:guid}/versions/{versionId:guid}/clone")]
    public IActionResult Clone(Guid id, Guid versionId, [FromBody] CloneVersionRequest request) => ToAction(operations.CloneVersion(actor.Current, id, versionId, request.ChangeSummary));
    [HttpDelete("{id:guid}/versions/{versionId:guid}")]
    public IActionResult DeleteDraft(Guid id, Guid versionId, [FromQuery] string concurrencyToken) => ToAction(operations.DeleteDraft(actor.Current, id, versionId, new ConcurrencyToken(concurrencyToken)));
    private IActionResult ToAction<T>(ApplicationResult<T> result) => result.IsSuccess ? Ok(result.Value) : ProblemDetailsMapper.ToResult(result.Error!, actor.Current.CorrelationId);
    private static FormulaValueType ParseType(string type) => string.Equals(type, "Boolean", StringComparison.OrdinalIgnoreCase) ? FormulaValueType.Boolean : FormulaValueType.Decimal;
    private static FormulaResultType ParseResultType(string type) => string.Equals(type, "Boolean", StringComparison.OrdinalIgnoreCase) ? FormulaResultType.Boolean : FormulaResultType.Decimal;
}

public sealed record CreateKpiRequest(string Code, string Name, string Description);
public sealed record CreateVersionRequest(string Name, string Description, string Source, IReadOnlyList<FormulaVariableInput> Variables, string ResultType, string ChangeSummary);
public sealed record FormulaVariableInput(string Code, string? DisplayName, string Type, bool Required, FormulaValue? DefaultValue, string? Description);
public sealed record ReviewRequest(bool Approve, string Comment);
public sealed record PublishRequest(DateTimeOffset EffectiveFrom);
public sealed record TransferRequest(Guid NewOwnerId, string Reason);
public sealed record UpdateDraftRequest(string Name, string Description, string Source, IReadOnlyList<FormulaVariableInput> Variables, string ConcurrencyToken);
public sealed record CloneVersionRequest(string ChangeSummary);
