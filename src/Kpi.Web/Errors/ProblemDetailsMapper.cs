using Kpi.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Kpi.Web.Errors;

/// <summary>Maps stable Application errors to localized-safe Problem Details.</summary>
public static class ProblemDetailsMapper
{
    public static ObjectResult ToResult(ApplicationError error, string? correlationId = null) => new ProblemDetails
    {
        Status = error.Status,
        Title = error.Code,
        Detail = error.Message,
        Extensions = { ["code"] = error.Code, ["correlationId"] = correlationId }
    } is var problem ? new ObjectResult(problem) { StatusCode = error.Status } : throw new InvalidOperationException();
}
