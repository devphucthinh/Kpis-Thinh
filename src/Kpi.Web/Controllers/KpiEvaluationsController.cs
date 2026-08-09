using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Formula;
using Kpi.Web.Queries;
using Kpi.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Kpi.Web.Controllers;

public sealed class KpiEvaluationsController(KpiOperations kpis, EvaluationOperations evaluations, KpiWebReadModelService readModels, ICurrentActor actor) : Controller
{
    [HttpGet]
    public IActionResult History(Guid definitionId, string? notice = null)
    {
        var page = readModels.GetEvaluationHistory(actor.Current, definitionId, notice);
        if (page is null) return NotFound();
        ViewData["ActiveNav"] = "evaluations";
        return View(page);
    }

    [HttpGet]
    public IActionResult Create(Guid definitionId, Guid activationId, string? notice = null)
    {
        var page = readModels.GetEvaluationPage(actor.Current, definitionId, activationId, notice);
        if (page is null) return NotFound();
        ViewData["ActiveNav"] = "evaluations";
        return View(page);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(EvaluationInputModel model)
    {
        var definition = kpis.List(actor.Current.OrganizationId).FirstOrDefault(x => x.Id == model.DefinitionId);
        var version = definition?.Versions.FirstOrDefault(x => x.Id == model.VersionId);
        if (version is null) return RedirectToAction(nameof(History), new { definitionId = model.DefinitionId, notice = "KPI Version không tồn tại." });
        var variables = version.Variables.OrderBy(x => x.DisplayOrder).Select(variable => new FormulaVariableInputVm(variable.Code, variable.DisplayName, variable.Type, variable.Required, null, variable.Description, variable.DisplayOrder)).ToArray();
        var parsed = FormulaInputParser.Parse(variables, model.Inputs);
        if (!parsed.IsSuccess) return RedirectToAction(nameof(Create), new { definitionId = model.DefinitionId, activationId = model.ActivationId, notice = parsed.Error!.Message });
        var result = evaluations.Evaluate(actor.Current, model.DefinitionId, model.VersionId, model.ActivationId, version.Formula, version.Variables, parsed.Value!);
        return RedirectToAction(nameof(History), new { definitionId = model.DefinitionId, notice = result.Error?.Message ?? "Evaluation đã được lưu." });
    }

    [HttpGet]
    public IActionResult Correct(Guid definitionId, Guid predecessorId, string? notice = null)
    {
        var page = readModels.GetCorrectionPage(actor.Current, definitionId, predecessorId, notice);
        if (page is null) return NotFound();
        ViewData["ActiveNav"] = "evaluations";
        return View(page);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Correct(CorrectionInputModel model)
    {
        var definition = kpis.List(actor.Current.OrganizationId).FirstOrDefault(x => x.Id == model.DefinitionId);
        var version = definition?.Versions.FirstOrDefault(x => x.Id == model.VersionId);
        if (version is null) return RedirectToAction(nameof(History), new { definitionId = model.DefinitionId, notice = "KPI Version không tồn tại." });
        var variables = version.Variables.OrderBy(x => x.DisplayOrder).Select(variable => new FormulaVariableInputVm(variable.Code, variable.DisplayName, variable.Type, variable.Required, null, variable.Description, variable.DisplayOrder)).ToArray();
        var parsed = FormulaInputParser.Parse(variables, model.Inputs);
        if (!parsed.IsSuccess) return RedirectToAction(nameof(Correct), new { definitionId = model.DefinitionId, predecessorId = model.PredecessorId, notice = parsed.Error!.Message });
        var result = evaluations.Correct(actor.Current, model.DefinitionId, model.ActivationId, model.PredecessorId, model.VersionId, version.Formula, version.Variables, parsed.Value!, model.Reason);
        return RedirectToAction(nameof(History), new { definitionId = model.DefinitionId, notice = result.Error?.Message ?? "Superseding Evaluation đã được lưu." });
    }
}
