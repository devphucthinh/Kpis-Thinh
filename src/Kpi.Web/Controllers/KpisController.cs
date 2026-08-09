using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Formula;
using Microsoft.AspNetCore.Mvc;

namespace Kpi.Web.Controllers;

/// <summary>Vietnamese-first server-rendered KPI authoring and governance screens.</summary>
public sealed class KpisController(KpiOperations operations, ICurrentActor actor) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(operations.List(actor.Current.OrganizationId));

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
    public IActionResult Edit(Guid id)
    {
        var definition = operations.List(actor.Current.OrganizationId).FirstOrDefault(x => x.Id == id);
        return definition is null ? NotFound() : View(new EditKpiModel { DefinitionId = id, Definition = definition });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddVersion(EditKpiModel model)
    {
        IReadOnlyList<FormulaVariableDefinition> variables;
        try
        {
            variables = ParseVariables(model.Variables);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(nameof(model.Variables), ex.Message);
            model.Definition = operations.List(actor.Current.OrganizationId).FirstOrDefault(x => x.Id == model.DefinitionId);
            return View(nameof(Edit), model);
        }

        var result = operations.CreateVersion(actor.Current, model.DefinitionId, model.VersionName, model.VersionDescription, model.Source, variables, FormulaResultType.Decimal, model.ChangeSummary);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!.Message);
            model.Definition = operations.List(actor.Current.OrganizationId).FirstOrDefault(x => x.Id == model.DefinitionId);
            return View(nameof(Edit), model);
        }

        return RedirectToAction(nameof(Edit), new { id = model.DefinitionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateDraft(EditKpiModel model)
    {
        var result = operations.UpdateDraft(actor.Current, model.DefinitionId, model.VersionId, model.VersionName, model.VersionDescription, model.Source, ParseVariables(model.Variables), new ConcurrencyToken(model.ConcurrencyToken));
        return RedirectToAction(nameof(Edit), new { id = model.DefinitionId, notice = result.Error?.Message });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Submit(Guid definitionId, Guid versionId) => RedirectToEdit(definitionId, operations.SubmitVersion(actor.Current, definitionId, versionId));
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Review(Guid definitionId, Guid versionId, bool approve, string comment) => RedirectToEdit(definitionId, operations.ReviewVersion(actor.Current, definitionId, versionId, approve, comment));
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Publish(Guid definitionId, Guid versionId, DateTimeOffset effectiveFrom) => RedirectToEdit(definitionId, operations.PublishVersion(actor.Current, definitionId, versionId, effectiveFrom));
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ReturnToDraft(Guid definitionId, Guid versionId) => RedirectToEdit(definitionId, operations.ReturnVersionToDraft(actor.Current, definitionId, versionId));
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Clone(Guid definitionId, Guid versionId, string changeSummary) => RedirectToEdit(definitionId, operations.CloneVersion(actor.Current, definitionId, versionId, changeSummary));
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Archive(Guid id) => RedirectToAction(nameof(Index), new { notice = operations.Archive(actor.Current, id).Error?.Message });
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Restore(Guid id) => RedirectToAction(nameof(Index), new { notice = operations.Restore(actor.Current, id).Error?.Message });

    private IActionResult RedirectToEdit<T>(Guid definitionId, ApplicationResult<T> result) => RedirectToAction(nameof(Edit), new { id = definitionId, notice = result.Error?.Message });
    private static IReadOnlyList<FormulaVariableDefinition> ParseVariables(string text) => text.Split(['\r', '\n', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select((x, i) => FormulaVariableDefinition.Create(x, x, FormulaValueType.Decimal, true, null, i)).ToArray();
}

public sealed class CreateKpiModel { public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; }
public sealed class EditKpiModel { public Guid DefinitionId { get; set; } public Guid VersionId { get; set; } public string ConcurrencyToken { get; set; } = "0"; public Kpi.Domain.Kpis.KpiDefinition? Definition { get; set; } public string VersionName { get; set; } = string.Empty; public string VersionDescription { get; set; } = string.Empty; public string Variables { get; set; } = string.Empty; public string Source { get; set; } = string.Empty; public string ChangeSummary { get; set; } = string.Empty; }
