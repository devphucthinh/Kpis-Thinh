using Kpi.Application;
using Kpi.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Kpi.Web.Controllers;

public sealed class KpiPeriodsController(PeriodOperations operations, ICurrentActor actor) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(operations.List());
    [HttpGet]
    public IActionResult Create() => View(new CreatePeriodModel { StartsAt = DateTimeOffset.Now.Date, EndsAt = DateTimeOffset.Now.Date.AddMonths(1).AddDays(-1) });
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(CreatePeriodModel model)
    {
        var result = operations.Create(actor.Current, model.Code, model.StartsAt, model.EndsAt);
        if (!result.IsSuccess) ModelState.AddModelError(string.Empty, result.Error!.Message);
        return result.IsSuccess ? RedirectToAction(nameof(Index)) : View(model);
    }
}

public sealed class CreatePeriodModel { public string Code { get; set; } = string.Empty; public DateTimeOffset StartsAt { get; set; } public DateTimeOffset EndsAt { get; set; } }
