using System.Text.Json;
using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Periods;
using Kpi.Web.Queries;
using Kpi.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Kpi.Web.Controllers;

public sealed class KpiPeriodsController(PeriodOperations operations, KpiWebReadModelService readModels, ICurrentActor actor) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        ViewData["ActiveNav"] = "periods";
        return View(readModels.GetPeriodIndex(actor.Current));
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewData["ActiveNav"] = "periods";
        return View(new CreatePeriodModel { StartsAt = DateTimeOffset.Now.Date, EndsAt = DateTimeOffset.Now.Date.AddMonths(1).AddDays(-1) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(CreatePeriodModel model)
    {
        var result = operations.Create(actor.Current, model.Code, model.Name, model.Description, model.Cadence, model.StartsAt, model.EndsAt);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!.Message);
            ViewData["ActiveNav"] = "periods";
            return View(model);
        }
        return RedirectToAction(nameof(Details), new { id = result.Value!.Id });
    }

    [HttpGet]
    public IActionResult Details(Guid id, string? notice = null)
    {
        var page = readModels.GetPeriodDetails(actor.Current, id, notice);
        if (page is null) return NotFound();
        ViewData["ActiveNav"] = "periods";
        return View(page);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SaveSelections(Guid periodId, string selectionsJson, string concurrencyToken)
    {
        try
        {
            var selections = JsonSerializer.Deserialize<Dictionary<Guid, Guid>>(selectionsJson) ?? [];
            var result = operations.SelectMany(actor.Current, periodId, selections, new ConcurrencyToken(concurrencyToken));
            return RedirectToAction(nameof(Details), new { id = periodId, notice = result.Error?.Message });
        }
        catch (JsonException ex)
        {
            return RedirectToAction(nameof(Details), new { id = periodId, notice = $"Selection JSON không hợp lệ: {ex.Message}" });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Select(Guid periodId, Guid definitionId, Guid versionId, string? concurrencyToken = null) => RedirectToAction(nameof(Details), new { id = periodId, notice = operations.Select(actor.Current, periodId, definitionId, versionId, ParseToken(concurrencyToken)).Error?.Message });

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Submit(Guid periodId, string? concurrencyToken = null) => RedirectToAction(nameof(Details), new { id = periodId, notice = operations.Submit(actor.Current, periodId, ParseToken(concurrencyToken)).Error?.Message });

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Approve(Guid periodId) => RedirectToAction(nameof(Details), new { id = periodId, notice = operations.Approve(actor.Current, periodId).Error?.Message });

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Reject(Guid periodId, string comment) => RedirectToAction(nameof(Details), new { id = periodId, notice = operations.Reject(actor.Current, periodId, comment).Error?.Message });

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ReturnToDraft(Guid periodId) => RedirectToAction(nameof(Details), new { id = periodId, notice = operations.ReturnToDraft(actor.Current, periodId).Error?.Message });

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ProposeAmendment(Guid periodId, IReadOnlyDictionary<Guid, Guid> selections, string reason, DateTimeOffset? startsAt = null, DateTimeOffset? endsAt = null) => RedirectToAction(nameof(Details), new { id = periodId, notice = operations.ProposeAmendment(actor.Current, periodId, selections, reason, startsAt, endsAt).Error?.Message });

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ReviewAmendment(Guid periodId, Guid amendmentId, bool approve, string comment) => RedirectToAction(nameof(Details), new { id = periodId, notice = operations.ReviewAmendment(actor.Current, periodId, amendmentId, approve, comment).Error?.Message });

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Activate(Guid periodId) => RedirectToAction(nameof(Details), new { id = periodId, notice = operations.Activate(actor.Current, periodId).Error?.Message });

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Close(Guid periodId) => RedirectToAction(nameof(Details), new { id = periodId, notice = operations.Close(actor.Current, periodId).Error?.Message });

    private static ConcurrencyToken? ParseToken(string? value) => string.IsNullOrWhiteSpace(value) ? null : new ConcurrencyToken(value);
}
