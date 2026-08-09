using Kpi.Application.Common;
using Kpi.Web.Errors;
using Xunit;

namespace Kpi.IntegrationTests.Web;

public sealed class ProblemDetailsMappingTests
{
    [Fact]
    public void Stable_application_code_and_correlation_are_exposed()
    {
        var result = ProblemDetailsMapper.ToResult(new ApplicationError("VALIDATION", "Invalid formula"), "corr-1");
        var details = Assert.IsType<Microsoft.AspNetCore.Mvc.ProblemDetails>(result.Value);
        Assert.Equal("VALIDATION", details.Extensions["code"]);
        Assert.Equal("corr-1", details.Extensions["correlationId"]);
    }
}
