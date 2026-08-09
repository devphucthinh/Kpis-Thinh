using Kpi.Application.Common;
using Kpi.Web.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Kpi.Web.Controllers;

public sealed class HomeController(KpiWebReadModelService readModels, ICurrentActor actor) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        ViewData["ActiveNav"] = "overview";
        return View(readModels.GetOverview(actor.Current));
    }
}
