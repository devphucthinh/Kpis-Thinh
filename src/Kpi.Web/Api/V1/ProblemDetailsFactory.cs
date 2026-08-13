using Kpi.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Kpi.Web.Api.V1;

/// <summary>Creates stable, correlation-aware Problem Details for API and MVC adapters.</summary>
public static class ProblemDetailsFactory
{
    private static readonly IReadOnlyDictionary<string, int> StableStatusByCode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["MALFORMED_REQUEST"] = 400,
        ["BAD_REQUEST"] = 400,
        ["AUTHORIZATION_DENIED"] = 403,
        ["ACCOUNT_DISABLED"] = 403,
        ["ACCOUNT_UNLINKED"] = 403,
        ["EMPLOYMENT_INACTIVE"] = 403,
        ["MISSING_CAPABILITY"] = 403,
        ["SCOPE_MISMATCH"] = 403,
        ["AUTHORITY_NOT_EFFECTIVE"] = 403,
        ["DELEGATION_NOT_EFFECTIVE"] = 403,
        ["DELEGATION_SCOPE_MISMATCH"] = 403,
        ["SEPARATION_OF_DUTY"] = 403,
        ["RESOURCE_NOT_FOUND"] = 404,
        ["ORGANIZATION_MISMATCH"] = 404,
        ["CONCURRENCY_CONFLICT"] = 409,
        ["STALE_REVISION"] = 409,
        ["LIFECYCLE_CONFLICT"] = 409,
        ["ACTIVATION_REQUIRED"] = 409,
        ["VALIDATION"] = 422,
        ["BASELINE_MISSING"] = 422,
        ["APPROVER_UNRESOLVED"] = 422
    };

    public static ObjectResult Create(
        ApplicationError error,
        string? correlationId = null,
        string? currentConcurrencyToken = null,
        Guid? currentBaselineId = null,
        Guid? currentSegmentId = null)
    {
        ArgumentNullException.ThrowIfNull(error);
        var status = StableStatusByCode.TryGetValue(error.Code, out var stableStatus)
            ? stableStatus
            : error.Status is 400 or 403 or 404 or 409 or 422 ? error.Status : 422;
        var problem = new ProblemDetails
        {
            Status = status,
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

        return new ObjectResult(problem) { StatusCode = status };
    }
}
