using Kpi.Application;
using Kpi.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Kpi.Web.Api.V1;

[ApiController]
[Route("api/v1/audit")]
public sealed class AuditController(KpiOperations operations, ICurrentActor actor) : ControllerBase
{
    [HttpGet]
    public IActionResult List() => actor.Current.Can(KpiCapability.AuditRead) || actor.Current.Can(KpiCapability.Administrator) ? Ok(operations.Audit()) : StatusCode(403, new { code = "AUTHORIZATION_DENIED", message = "Audit read capability is required." });
}
