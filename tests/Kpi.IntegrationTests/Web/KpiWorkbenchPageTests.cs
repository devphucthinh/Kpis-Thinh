using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Kpi.IntegrationTests.Web;

public sealed class KpiWorkbenchPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public KpiWorkbenchPageTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task Workbench_renders_typed_variables_version_stepper_and_ast()
    {
        var index = await client.GetStringAsync("/Kpis", TestContext.Current.CancellationToken);
        var definitionId = Regex.Match(index, @"/Kpis/Edit/(?<id>[0-9a-f-]{36})", RegexOptions.IgnoreCase).Groups["id"].Value;
        Assert.NotEmpty(definitionId);

        var response = await client.GetAsync($"/Kpis/Edit/{definitionId}", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("data-version-stepper", html, StringComparison.Ordinal);
        Assert.Contains("data-variable-type=\"Decimal\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"VariablesJson\"", html, StringComparison.Ordinal);
        Assert.Contains("formula-ast", html, StringComparison.Ordinal);
        Assert.Contains("formula-suggestions-panel", html, StringComparison.Ordinal);
        Assert.Contains("formula-syntax-helper", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Formula_editor_contains_variable_rows_and_autocomplete_support()
    {
        var response = await client.GetAsync("/js/formula-editor.js", TestContext.Current.CancellationToken);
        var javascript = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("parseVariableRows", javascript, StringComparison.Ordinal);
        Assert.Contains("autocomplete", javascript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("variablesJson", javascript, StringComparison.Ordinal);
        Assert.Contains("supportedOperations", javascript, StringComparison.Ordinal);
        Assert.Contains("ArrowDown", javascript, StringComparison.Ordinal);
        Assert.Contains("ArrowUp", javascript, StringComparison.Ordinal);
        Assert.Contains("formula-syntax-helper", javascript, StringComparison.Ordinal);
    }
}
