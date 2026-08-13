using Kpi.Application.Common;
using Kpi.Web.Api.V1;
using Microsoft.AspNetCore.Mvc;

namespace Kpi.Web.Errors;

/// <summary>Maps stable Application errors to localized-safe Problem Details.</summary>
public static class ProblemDetailsMapper
{
    public static ObjectResult ToResult(ApplicationError error, string? correlationId = null) =>
        ProblemDetailsFactory.Create(error, correlationId);
}
