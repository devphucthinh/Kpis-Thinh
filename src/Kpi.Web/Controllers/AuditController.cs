using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Auditing;
using Kpi.Web.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Kpi.Web.Controllers;

public sealed class AuditController(KpiWebReadModelService readModels, ICurrentActor actor) : Controller
{
    [HttpGet]
    public IActionResult Index(string? entityType = null, Guid? entityId = null, Guid? actorId = null, AuditEventType? eventType = null, DateTimeOffset? from = null, DateTimeOffset? to = null)
    {
        if (!actor.Current.Can(KpiCapability.AuditRead) && !actor.Current.Can(KpiCapability.Administrator)) return Forbid();
        ViewData["ActiveNav"] = "audit";
        return View(readModels.GetAudit(actor.Current, entityType, entityId, actorId, eventType, from, to));
    }
}
