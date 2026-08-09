using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Formula;
using Kpi.Domain.Kpis;
using Kpi.Web.Queries;
using Kpi.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Kpi.Web.Controllers;

/// <summary>Vietnamese-first server-rendered KPI authoring and governance screens.</summary>
public sealed class KpisController(KpiOperations operations, KpiWebReadModelService readModels, ICurrentActor actor) : Controller
{
    [HttpGet]
    public IActionResult Index(string? query = null, KpiVersionStatus? status = null)
    {
        ViewData["ActiveNav"] = "kpis";
        return View(readModels.GetKpiIndex(actor.Current, query, status));
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["ActiveNav"] = "kpis";
        return View(new CreateKpiModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(CreateKpiModel model)
    {
        var result = operations.CreateDefinition(actor.Current, model.Code, model.Name, model.Description);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!.Message);
            ViewData["ActiveNav"] = "kpis";
            return View(model);
        }

        return RedirectToAction(nameof(Edit), new { id = result.Value!.Id });
    }

    [HttpGet]
    public IActionResult Edit(Guid id, string? notice = null)
    {
        var workbench = readModels.GetWorkbench(actor.Current, id, notice);
        if (workbench is null) return NotFound();
        ViewData["ActiveNav"] = "kpis";
        return View(new KpiEditPageVm(workbench, FormFrom(workbench)));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddVersion(EditKpiModel model)
    {
        IReadOnlyList<FormulaVariableDefinition> variables;
        try
        {
            variables = ParseVariables(model);
        }
        catch (Exception ex) when (ex is ArgumentException or JsonException or FormatException)
        {
            ModelState.AddModelError(nameof(model.VariablesJson), ex.Message);
            return ReturnEdit(model.DefinitionId, model);
        }

        var result = operations.CreateVersion(actor.Current, model.DefinitionId, model.VersionName, model.VersionDescription, model.Source, variables, FormulaResultType.Decimal, model.ChangeSummary);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!.Message);
            return ReturnEdit(model.DefinitionId, model);
        }

        return RedirectToAction(nameof(Edit), new { id = model.DefinitionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateDraft(EditKpiModel model)
    {
        try
        {
            var result = operations.UpdateDraft(actor.Current, model.DefinitionId, model.VersionId, model.VersionName, model.VersionDescription, model.Source, ParseVariables(model), new ConcurrencyToken(model.ConcurrencyToken));
            if (!result.IsSuccess) ModelState.AddModelError(string.Empty, result.Error!.Message);
            else return RedirectToAction(nameof(Edit), new { id = model.DefinitionId });
        }
        catch (Exception ex) when (ex is ArgumentException or JsonException or FormatException)
        {
            ModelState.AddModelError(nameof(model.VariablesJson), ex.Message);
        }

        return ReturnEdit(model.DefinitionId, model);
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

    private IActionResult ReturnEdit(Guid definitionId, EditKpiModel form)
    {
        var workbench = readModels.GetWorkbench(actor.Current, definitionId);
        if (workbench is null) return NotFound();
        ViewData["ActiveNav"] = "kpis";
        return View(nameof(Edit), new KpiEditPageVm(workbench, form));
    }

    private IActionResult RedirectToEdit<T>(Guid definitionId, ApplicationResult<T> result) => RedirectToAction(nameof(Edit), new { id = definitionId, notice = result.Error?.Message });

    private static EditKpiModel FormFrom(KpiWorkbenchVm workbench)
    {
        var draft = workbench.Draft;
        var rows = draft?.Variables ?? [];
        return new EditKpiModel
        {
            DefinitionId = workbench.DefinitionId,
            VersionId = draft?.VersionId ?? Guid.Empty,
            ConcurrencyToken = draft?.ConcurrencyToken ?? "0",
            VersionName = draft?.Name ?? string.Empty,
            VersionDescription = draft?.Description ?? string.Empty,
            VariablesJson = FormulaVariableFormSerializer.Serialize(rows),
            Variables = string.Join(Environment.NewLine, rows.Select(x => x.Code)),
            Source = draft?.Source ?? string.Empty,
            ChangeSummary = draft?.ChangeSummary ?? string.Empty
        };
    }

    private static IReadOnlyList<FormulaVariableDefinition> ParseVariables(EditKpiModel model)
    {
        var rows = FormulaVariableFormSerializer.Deserialize(model.VariablesJson, model.Variables);
        return rows
            .Select(row => FormulaVariableDefinition.Create(row.Code, row.DisplayName, row.Type, row.Required, ParseDefault(row), row.DisplayOrder, row.Description))
            .ToArray();
    }

    private static FormulaValue? ParseDefault(FormulaVariableInputVm row)
    {
        if (string.IsNullOrWhiteSpace(row.DefaultValue)) return null;
        if (row.Type == FormulaValueType.Decimal && decimal.TryParse(row.DefaultValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue)) return FormulaValue.Decimal(decimalValue);
        if (row.Type == FormulaValueType.Boolean && bool.TryParse(row.DefaultValue, out var booleanValue)) return FormulaValue.Boolean(booleanValue);
        throw new FormatException($"Default value for '{row.Code}' is not a valid {row.Type} value.");
    }
}
