using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Kpi.IntegrationTests.Web;

public sealed class OverviewAndAuditPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public OverviewAndAuditPageTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Overview_exposes_operational_counts_and_next_actions()
    {
        var response = await client.GetAsync("/?persona=creator", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Tổng quan", html, StringComparison.Ordinal);
        Assert.Contains("KPI Definitions", html, StringComparison.Ordinal);
        Assert.Contains("Kỳ KPI", html, StringComparison.Ordinal);
        Assert.Contains("Đánh giá gần đây", html, StringComparison.Ordinal);
        Assert.Contains("Việc cần làm", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Audit_exposes_actor_event_and_date_filters()
    {
        var response = await client.GetAsync("/Audit?persona=admin&entityType=KPI_VERSION&eventType=Published&actorId=00000000-0000-0000-0000-000000000001&from=2026-01-01&to=2026-12-31", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Audit timeline", html, StringComparison.Ordinal);
        Assert.Contains("name=\"actorId\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"eventType\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"from\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"to\"", html, StringComparison.Ordinal);
        Assert.Contains("KPI_VERSION", html, StringComparison.Ordinal);
        Assert.Contains("Published", html, StringComparison.Ordinal);
    }
}
