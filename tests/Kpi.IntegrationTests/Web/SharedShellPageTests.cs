using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Kpi.IntegrationTests.Web;

public sealed class SharedShellPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public SharedShellPageTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task Shared_shell_exposes_navigation_and_theme_controls()
    {
        var response = await client.GetAsync("/Kpis", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/KpiPeriods", html, StringComparison.Ordinal);
        Assert.Contains("/KpiEvaluations/History", html, StringComparison.Ordinal);
        Assert.Contains("/Audit", html, StringComparison.Ordinal);
        Assert.Contains("data-theme-toggle", html, StringComparison.Ordinal);
        Assert.Contains("theme.js", html, StringComparison.Ordinal);
        Assert.Contains("aria-current", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Theme_script_supports_system_preference_and_local_override()
    {
        var response = await client.GetAsync("/js/theme.js", TestContext.Current.CancellationToken);
        var javascript = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("prefers-color-scheme", javascript, StringComparison.Ordinal);
        Assert.Contains("localStorage", javascript, StringComparison.Ordinal);
        Assert.Contains("data-theme", javascript, StringComparison.Ordinal);
    }
}
