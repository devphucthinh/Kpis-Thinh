using Kpi.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Kpi.Web.Api.V1;

/// <summary>Creates stable, correlation-aware Problem Details for API and MVC adapters.</summary>
public static class ProblemDetailsFactory
{
    public static ObjectResult Create(
        ApplicationError error,
        string? correlationId = null,
        string? currentConcurrencyToken = null,
        Guid? currentBaselineId = null,
        Guid? currentSegmentId = null)
    {
        ArgumentNullException.ThrowIfNull(error);
        var problem = new ProblemDetails
        {
            Status = error.Status,
            Title = error.Code,
            Detail = error.Message
        };
        problem.Extensions["code"] = error.Code;
        if (!string.IsNullOrWhiteSpace(correlationId))
            problem.Extensions["correlationId"] = correlationId;
        if (!string.IsNullOrWhiteSpace(currentConcurrencyToken))
            problem.Extensions["currentConcurrencyToken"] = currentConcurrencyToken;
        if (currentBaselineId is not null)
            problem.Extensions["currentBaselineId"] = currentBaselineId.Value.ToString("D");
        if (currentSegmentId is not null)
            problem.Extensions["currentSegmentId"] = currentSegmentId.Value.ToString("D");

        return new ObjectResult(problem) { StatusCode = error.Status };
    }
}
