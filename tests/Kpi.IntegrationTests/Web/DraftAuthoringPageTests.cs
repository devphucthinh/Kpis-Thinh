using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Kpi.IntegrationTests.Web;

public sealed class DraftAuthoringPageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public DraftAuthoringPageTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    [Fact]
    public async Task Draft_page_exposes_formula_editor_and_diagnostics()
    {
        var response = await _client.GetAsync("/Kpis/Create", TestContext.Current.CancellationToken); var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Contains("Tạo KPI", html, StringComparison.Ordinal); Assert.Contains("Code", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Add_version_form_includes_antiforgery_token_for_protected_post()
    {
        var index = await _client.GetStringAsync("/Kpis", TestContext.Current.CancellationToken);
        var definitionId = Regex.Match(index, @"/Kpis/Edit/(?<id>[0-9a-f-]{36})", RegexOptions.IgnoreCase).Groups["id"].Value;
        Assert.NotEmpty(definitionId);

        var response = await _client.GetAsync($"/Kpis/Edit/{definitionId}", TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("action=\"/Kpis/AddVersion\"", html, StringComparison.Ordinal);
        Assert.Contains("name=\"__RequestVerificationToken\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Add_version_post_with_form_token_redirects_instead_of_returning_bad_request()
    {
        var (definitionId, token) = await GetDefinitionAndToken();
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["DefinitionId"] = definitionId,
            ["VersionName"] = "Web test version",
            ["VersionDescription"] = "Version created through the authoring page.",
            ["Variables"] = "revenue\ntarget",
            ["Source"] = "revenue + target",
            ["ChangeSummary"] = "Web form regression test"
        });

        var response = await _client.PostAsync("/Kpis/AddVersion", form, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/Kpis/Edit/{definitionId}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Add_version_post_with_invalid_variable_shows_validation_instead_of_server_error()
    {
        var (definitionId, token) = await GetDefinitionAndToken();
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["DefinitionId"] = definitionId,
            ["VersionName"] = "Invalid version",
            ["VersionDescription"] = "Invalid variable regression test.",
            ["Variables"] = "Invalid Code",
            ["Source"] = "revenue",
            ["ChangeSummary"] = "Validation regression test"
        });

        var response = await _client.PostAsync("/Kpis/AddVersion", form, TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("lower snake case", html, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<(string DefinitionId, string Token)> GetDefinitionAndToken()
    {
        var index = await _client.GetStringAsync("/Kpis", TestContext.Current.CancellationToken);
        var definitionId = Regex.Match(index, @"/Kpis/Edit/(?<id>[0-9a-f-]{36})", RegexOptions.IgnoreCase).Groups["id"].Value;
        Assert.NotEmpty(definitionId);
        var edit = await _client.GetAsync($"/Kpis/Edit/{definitionId}", TestContext.Current.CancellationToken);
        var html = await edit.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        return (definitionId, token);
    }
}
