using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Kpi.IntegrationTests.Web;

public sealed class DraftAuthoringPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public DraftAuthoringPageTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();
    [Fact]
    public async Task Draft_page_exposes_formula_editor_and_diagnostics()
    {
        var response = await _client.GetAsync("/Kpis/Create", TestContext.Current.CancellationToken); var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Contains("Tạo KPI", html, StringComparison.Ordinal); Assert.Contains("Code", html, StringComparison.Ordinal);
    }
}
