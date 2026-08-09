using Kpi.Application;
using Kpi.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Kpi.Web.Errors;

namespace Kpi.Web.Api.V1;

[ApiController]
[Route("api/v1/audit")]
public sealed class AuditController(KpiOperations operations, ICurrentActor actor) : ControllerBase
{
    [HttpGet]
    public IActionResult List([FromQuery] string? entityType = null, [FromQuery] Guid? entityId = null, [FromQuery] DateTimeOffset? from = null, [FromQuery] DateTimeOffset? to = null)
    {
        if (!(actor.Current.Can(KpiCapability.AuditRead) || actor.Current.Can(KpiCapability.Administrator))) return ProblemDetailsMapper.ToResult(new ApplicationError("AUTHORIZATION_DENIED", "Audit read capability is required.", 403), actor.Current.CorrelationId);
        return Ok(operations.Audit(actor.Current.OrganizationId, entityType, entityId, from, to));
    }
}
