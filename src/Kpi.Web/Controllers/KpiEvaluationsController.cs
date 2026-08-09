using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Formula;
using Microsoft.AspNetCore.Mvc;

namespace Kpi.Web.Controllers;

public sealed class KpiEvaluationsController(KpiOperations kpis, EvaluationOperations evaluations, ICurrentActor actor) : Controller
{
    [HttpGet]
    public IActionResult History(Guid definitionId) => View(new EvaluationHistoryModel { Definition = kpis.List(actor.Current.OrganizationId).FirstOrDefault(x => x.Id == definitionId), Current = evaluations.Current(definitionId), Attempts = evaluations.History(definitionId) });

    [HttpGet]
    public IActionResult Create(Guid definitionId, Guid activationId) => View(new EvaluationInputModel { DefinitionId = definitionId, ActivationId = activationId, Definition = kpis.List(actor.Current.OrganizationId).FirstOrDefault(x => x.Id == definitionId) });

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(EvaluationInputModel model)
    {
        var version = model.Definition?.Versions.FirstOrDefault(x => x.Id == model.VersionId) ?? kpis.List(actor.Current.OrganizationId).SelectMany(x => x.Versions).FirstOrDefault(x => x.Id == model.VersionId);
        if (version is null) return RedirectToAction(nameof(History), new { definitionId = model.DefinitionId });
        var result = evaluations.Evaluate(actor.Current, model.DefinitionId, model.VersionId, model.ActivationId, version.Formula, version.Variables, ParseInputs(model.Inputs));
        return RedirectToAction(nameof(History), new { definitionId = model.DefinitionId, notice = result.Error?.Message });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Correct(CorrectionInputModel model)
    {
        var definition = kpis.List(actor.Current.OrganizationId).FirstOrDefault(x => x.Id == model.DefinitionId);
        var version = definition?.Versions.FirstOrDefault(x => x.Id == model.VersionId);
        if (version is null) return RedirectToAction(nameof(History), new { definitionId = model.DefinitionId });
        var result = evaluations.Correct(actor.Current, model.DefinitionId, model.ActivationId, model.PredecessorId, model.VersionId, version.Formula, version.Variables, ParseInputs(model.Inputs), model.Reason);
        return RedirectToAction(nameof(History), new { definitionId = model.DefinitionId, notice = result.Error?.Message });
    }

    private static IReadOnlyDictionary<string, FormulaValue> ParseInputs(IReadOnlyDictionary<string, string> inputs) => inputs.ToDictionary<KeyValuePair<string, string>, string, FormulaValue>(x => x.Key, x => FormulaValue.Decimal(decimal.Parse(x.Value, System.Globalization.CultureInfo.InvariantCulture)));
}

public sealed class EvaluationHistoryModel { public Kpi.Domain.Kpis.KpiDefinition? Definition { get; set; } public Kpi.Domain.Evaluations.KpiEvaluation? Current { get; set; } public IReadOnlyList<Kpi.Domain.Evaluations.KpiEvaluation> Attempts { get; set; } = []; }
public sealed class EvaluationInputModel { public Guid DefinitionId { get; set; } public Guid VersionId { get; set; } public Guid ActivationId { get; set; } public Kpi.Domain.Kpis.KpiDefinition? Definition { get; set; } public Dictionary<string, string> Inputs { get; set; } = []; }
public sealed class CorrectionInputModel { public Guid DefinitionId { get; set; } public Guid VersionId { get; set; } public Guid ActivationId { get; set; } public Guid PredecessorId { get; set; } public Dictionary<string, string> Inputs { get; set; } = []; public string Reason { get; set; } = string.Empty; }
