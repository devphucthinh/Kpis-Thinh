using Kpi.Application;
using Kpi.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Kpi.Web.Controllers;

public sealed class AuditController(KpiOperations operations, ICurrentActor actor) : Controller
{
    [HttpGet]
    public IActionResult Index(string? entityType = null, Guid? entityId = null, DateTimeOffset? from = null, DateTimeOffset? to = null) => actor.Current.Can(KpiCapability.AuditRead) || actor.Current.Can(KpiCapability.Administrator) ? View(operations.Audit(actor.Current.OrganizationId, entityType, entityId, from, to)) : Forbid();
}
