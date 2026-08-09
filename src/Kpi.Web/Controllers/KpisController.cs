using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Formula;
using Microsoft.AspNetCore.Mvc;

namespace Kpi.Web.Controllers;

/// <summary>Vietnamese-first server-rendered KPI authoring screen.</summary>
public sealed class KpisController(KpiOperations operations, ICurrentActor actor) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(operations.List());

    [HttpGet]
    public IActionResult Create() => View(new CreateKpiModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(CreateKpiModel model)
    {
        var result = operations.CreateDefinition(actor.Current, model.Code, model.Name, model.Description);
        if (!result.IsSuccess) { ModelState.AddModelError(string.Empty, result.Error!.Message); return View(model); }
        return RedirectToAction(nameof(Edit), new { id = result.Value!.Id });
    }

    [HttpGet]
    public IActionResult Edit(Guid id) => View(new EditKpiModel { Definition = operations.List().FirstOrDefault(x => x.Id == id) });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddVersion(EditKpiModel model)
    {
        var variables = ParseVariables(model.Variables);
        var result = operations.CreateVersion(actor.Current, model.DefinitionId, model.VersionName, model.VersionDescription, model.Source, variables, FormulaResultType.Decimal, model.ChangeSummary);
        if (!result.IsSuccess) ModelState.AddModelError(string.Empty, result.Error!.Message);
        return RedirectToAction(nameof(Edit), new { id = model.DefinitionId });
    }

    private static IReadOnlyList<FormulaVariableDefinition> ParseVariables(string text) => text.Split(['\r', '\n', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select((x, i) => FormulaVariableDefinition.Create(x, x, FormulaValueType.Decimal, true, null, i)).ToArray();
}

public sealed class CreateKpiModel { public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; }
public sealed class EditKpiModel { public Guid DefinitionId { get; set; } public Kpi.Domain.Kpis.KpiDefinition? Definition { get; set; } public string VersionName { get; set; } = string.Empty; public string VersionDescription { get; set; } = string.Empty; public string Source { get; set; } = string.Empty; public string Variables { get; set; } = string.Empty; public string ChangeSummary { get; set; } = string.Empty; }
