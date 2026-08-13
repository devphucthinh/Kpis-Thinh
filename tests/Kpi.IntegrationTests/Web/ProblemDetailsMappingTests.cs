using Kpi.Application.Common;
using Kpi.Web.Api.V1;
using Kpi.Web.Errors;
using Xunit;

namespace Kpi.IntegrationTests.Web;

public sealed class ProblemDetailsMappingTests
{
    [Fact(DisplayName = "FR-027 application errors expose stable code and correlation context")]
    public void Stable_application_code_and_correlation_are_exposed()
    {
        var result = ProblemDetailsMapper.ToResult(new ApplicationError("VALIDATION", "Invalid formula"), "corr-1");
        var details = Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(result.Value);
        Assert.Equal("VALIDATION", details.Extensions["code"]);
        Assert.Equal("corr-1", details.Extensions["correlationId"]);
    }

    [Fact(DisplayName = "FR-027 FR-036 stable Problem Details preserve concurrency and baseline context")]
    public void Stable_problem_details_expose_safe_concurrency_context()
    {
        var result = ProblemDetailsFactory.Create(
            new ApplicationError("STALE_REVISION", "The submitted revision is stale.", 409),
            "corr-2",
            currentConcurrencyToken: "xmin-7",
            currentBaselineId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            currentSegmentId: Guid.Parse("22222222-2222-2222-2222-222222222222"));

        var details = Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(result.Value);
        Assert.Equal(409, result.StatusCode);
        Assert.Equal("STALE_REVISION", details.Extensions["code"]);
        Assert.Equal("corr-2", details.Extensions["correlationId"]);
        Assert.Equal("xmin-7", details.Extensions["currentConcurrencyToken"]);
        Assert.Equal("11111111-1111-1111-1111-111111111111", details.Extensions["currentBaselineId"]);
        Assert.Equal("22222222-2222-2222-2222-222222222222", details.Extensions["currentSegmentId"]);
    }

    [Theory(DisplayName = "FR-027 explicit stable Problem Details map all supported HTTP classes")]
    [InlineData("MALFORMED_REQUEST", 400)]
    [InlineData("AUTHORIZATION_DENIED", 403)]
    [InlineData("RESOURCE_NOT_FOUND", 404)]
    [InlineData("CONCURRENCY_CONFLICT", 409)]
    [InlineData("VALIDATION", 422)]
    public void Stable_codes_override_an_incorrect_caller_status(string code, int expectedStatus)
    {
        var result = ProblemDetailsFactory.Create(new ApplicationError(code, "stable message", 500), "corr-status");

        Assert.Equal(expectedStatus, result.StatusCode);
        var details = Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(result.Value);
        Assert.Equal(code, details.Extensions["code"]);
    }
}
