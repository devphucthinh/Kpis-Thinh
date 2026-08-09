using System.Text.Json;
using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Formula;
using Microsoft.AspNetCore.Mvc;

namespace Kpi.Web.Api.V1;

[ApiController]
[Route("api/v1/kpi-evaluations")]
public sealed class KpiEvaluationsController(KpiOperations kpis, EvaluationOperations evaluations, ICurrentActor actor) : ControllerBase
{
    [HttpGet("{definitionId:guid}")]
    public IActionResult History(Guid definitionId) => Ok(new { current = evaluations.Current(definitionId), history = evaluations.History(definitionId) });

    [HttpPost("{definitionId:guid}/{versionId:guid}")]
    public IActionResult Evaluate(Guid definitionId, Guid versionId, [FromBody] EvaluateRequest request)
    {
        var definition = kpis.List().FirstOrDefault(x => x.Id == definitionId); var version = definition?.Versions.FirstOrDefault(x => x.Id == versionId);
        if (version is null) return NotFound(new { code = "RESOURCE_NOT_FOUND", message = "KPI Version was not found." });
        var inputs = request.Inputs.ToDictionary(x => x.Key, x => Parse(x.Value));
        var result = evaluations.Evaluate(actor.Current, definitionId, versionId, version.Formula, version.Variables, inputs);
        return result.IsSuccess ? Ok(new { persisted = true, evaluation = result.Value, current = evaluations.Current(definitionId) }) : StatusCode(result.Error!.Status, result.Error);
    }

    [HttpPost("{definitionId:guid}/correct")]
    public IActionResult Correct(Guid definitionId, [FromBody] CorrectEvaluationRequest request)
    {
        var definition = kpis.List().FirstOrDefault(x => x.Id == definitionId); var version = definition?.Versions.FirstOrDefault(x => x.Id == request.VersionId);
        if (version is null) return NotFound(new { code = "RESOURCE_NOT_FOUND", message = "KPI Version was not found." });
        var inputs = request.Inputs.ToDictionary(x => x.Key, x => Parse(x.Value));
        var result = evaluations.Correct(actor.Current, definitionId, request.PredecessorId, request.VersionId, version.Formula, version.Variables, inputs, request.Reason);
        return result.IsSuccess ? Ok(new { persisted = true, evaluation = result.Value, current = evaluations.Current(definitionId) }) : StatusCode(result.Error!.Status, result.Error);
    }

    private static FormulaValue Parse(JsonElement value) => value.ValueKind is JsonValueKind.True or JsonValueKind.False ? FormulaValue.Boolean(value.GetBoolean()) : FormulaValue.Decimal(value.ValueKind == JsonValueKind.String ? decimal.Parse(value.GetString()!, System.Globalization.CultureInfo.InvariantCulture) : value.GetDecimal());
}

public sealed record EvaluateRequest(IReadOnlyDictionary<string, JsonElement> Inputs);
public sealed record CorrectEvaluationRequest(Guid PredecessorId, Guid VersionId, IReadOnlyDictionary<string, JsonElement> Inputs, string Reason);
