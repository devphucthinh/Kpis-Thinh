using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Kpi.IntegrationTests.Web;

public sealed class KpiPeriodPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public KpiPeriodPageTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Period_index_exposes_guided_plan_and_details_route()
    {
        var response = await client.GetAsync("/KpiPeriods?persona=planner", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/KpiPeriods/Create", html, StringComparison.Ordinal);
        Assert.Contains("Kỳ KPI", html, StringComparison.Ordinal);
        Assert.Contains("data-period-stepper", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Period_create_page_exposes_cadence_and_selection_guidance()
    {
        var response = await client.GetAsync("/KpiPeriods/Create?persona=planner", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Cadence", html, StringComparison.Ordinal);
        Assert.Contains("KPI Version", html, StringComparison.Ordinal);
        Assert.Contains("KPI Period Approver", html, StringComparison.Ordinal);
    }
}
