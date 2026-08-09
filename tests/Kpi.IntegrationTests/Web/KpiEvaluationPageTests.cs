using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Kpi.IntegrationTests.Web;

public sealed class KpiEvaluationPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public KpiEvaluationPageTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Evaluation_page_renders_typed_inputs_current_result_and_transient_test_copy()
    {
        var index = await client.GetStringAsync("/Kpis?persona=creator", TestContext.Current.CancellationToken);
        var definitionId = Regex.Match(index, @"/Kpis/Edit/(?<id>[0-9a-f-]{36})", RegexOptions.IgnoreCase).Groups["id"].Value;
        var response = await client.GetAsync($"/KpiEvaluations/Create?definitionId={definitionId}&activationId={Guid.NewGuid()}&persona=evaluator", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-evaluation-inputs", html, StringComparison.Ordinal);
        Assert.Contains("data-input-type=\"Decimal\"", html, StringComparison.Ordinal);
        Assert.Contains("Current KPI Evaluation", html, StringComparison.Ordinal);
        Assert.Contains("Tính evaluation mới", html, StringComparison.Ordinal);
        Assert.Contains("không lưu Evaluation", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Evaluation_history_defaults_to_latest_25_and_exposes_correction_path()
    {
        var index = await client.GetStringAsync("/Kpis?persona=creator", TestContext.Current.CancellationToken);
        var definitionId = Regex.Match(index, @"/Kpis/Edit/(?<id>[0-9a-f-]{36})", RegexOptions.IgnoreCase).Groups["id"].Value;
        var response = await client.GetAsync($"/KpiEvaluations/History?definitionId={definitionId}&persona=observer", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-history-limit=\"25\"", html, StringComparison.Ordinal);
        Assert.Contains("Current KPI Evaluation", html, StringComparison.Ordinal);
        Assert.Contains("Superseding Evaluation", html, StringComparison.Ordinal);
    }
}
