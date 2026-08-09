using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Periods;
using Microsoft.AspNetCore.Mvc;

namespace Kpi.Web.Controllers;

public sealed class KpiPeriodsController(PeriodOperations operations, ICurrentActor actor) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(operations.List(actor.Current.OrganizationId));
    [HttpGet]
    public IActionResult Create() => View(new CreatePeriodModel { StartsAt = DateTimeOffset.Now.Date, EndsAt = DateTimeOffset.Now.Date.AddMonths(1).AddDays(-1) });
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Create(CreatePeriodModel model)
    {
        var result = operations.Create(actor.Current, model.Code, model.Name ?? model.Code, model.Description ?? model.Code, model.Cadence, model.StartsAt, model.EndsAt);
        if (!result.IsSuccess) { ModelState.AddModelError(string.Empty, result.Error!.Message); return View(model); }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Select(Guid periodId, Guid definitionId, Guid versionId) => RedirectToAction(nameof(Index), new { notice = operations.Select(actor.Current, periodId, definitionId, versionId).Error?.Message });
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Submit(Guid periodId) => RedirectToAction(nameof(Index), new { notice = operations.Submit(actor.Current, periodId).Error?.Message });
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Approve(Guid periodId) => RedirectToAction(nameof(Index), new { notice = operations.Approve(actor.Current, periodId).Error?.Message });
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Reject(Guid periodId, string comment) => RedirectToAction(nameof(Index), new { notice = operations.Reject(actor.Current, periodId, comment).Error?.Message });
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ReturnToDraft(Guid periodId) => RedirectToAction(nameof(Index), new { notice = operations.ReturnToDraft(actor.Current, periodId).Error?.Message });
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ProposeAmendment(Guid periodId, IReadOnlyDictionary<Guid, Guid> selections, string reason, DateTimeOffset? startsAt = null, DateTimeOffset? endsAt = null) => RedirectToAction(nameof(Index), new { notice = operations.ProposeAmendment(actor.Current, periodId, selections, reason, startsAt, endsAt).Error?.Message });
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ReviewAmendment(Guid periodId, Guid amendmentId, bool approve, string comment) => RedirectToAction(nameof(Index), new { notice = operations.ReviewAmendment(actor.Current, periodId, amendmentId, approve, comment).Error?.Message });
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Activate(Guid periodId) => RedirectToAction(nameof(Index), new { notice = operations.Activate(actor.Current, periodId).Error?.Message });
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Close(Guid periodId) => RedirectToAction(nameof(Index), new { notice = operations.Close(actor.Current, periodId).Error?.Message });
}

public sealed class CreatePeriodModel { public string Code { get; set; } = string.Empty; public string? Name { get; set; } public string? Description { get; set; } public KpiCadence Cadence { get; set; } = KpiCadence.Monthly; public DateTimeOffset StartsAt { get; set; } public DateTimeOffset EndsAt { get; set; } }
