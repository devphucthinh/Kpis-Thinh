using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Kpi.IntegrationTests.Web;

public sealed class KpiGovernancePageTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public KpiGovernancePageTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task In_review_page_shows_read_only_review_and_requires_comment()
    {
        var definitionId = await SubmitSeedDraft();
        var reviewPage = await client.GetAsync($"/Kpis/Edit/{definitionId}?persona=approver", TestContext.Current.CancellationToken);
        var html = await reviewPage.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, reviewPage.StatusCode);
        Assert.Contains("data-review-panel", html, StringComparison.Ordinal);
        Assert.Contains("KPI Policy Approver", html, StringComparison.Ordinal);
        Assert.Contains("name=\"comment\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"Source\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Approved_page_exposes_publish_effective_date_without_editing_source()
    {
        var definitionId = await SubmitSeedDraft();
        var reviewPage = await client.GetAsync($"/Kpis/Edit/{definitionId}?persona=approver", TestContext.Current.CancellationToken);
        var reviewHtml = await reviewPage.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var versionId = Regex.Match(reviewHtml, "name=\"VersionId\" value=\"(?<id>[0-9a-f-]{36})\"", RegexOptions.IgnoreCase).Groups["id"].Value;
        var token = Regex.Match(reviewHtml, "name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"").Groups["token"].Value;
        Assert.NotEmpty(versionId);
        Assert.NotEmpty(token);

        using var reviewForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["definitionId"] = definitionId,
            ["versionId"] = versionId,
            ["approve"] = "true",
            ["comment"] = "Reviewed by policy approver"
        });
        var review = await client.PostAsync("/Kpis/Review?persona=approver", reviewForm, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, review.StatusCode);

        var approved = await client.GetAsync($"/Kpis/Edit/{definitionId}?persona=approver", TestContext.Current.CancellationToken);
        var approvedHtml = await approved.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("data-publish-panel", approvedHtml, StringComparison.Ordinal);
        Assert.Contains("name=\"effectiveFrom\"", approvedHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("name=\"Source\"", approvedHtml, StringComparison.Ordinal);
    }

    private async Task<string> SubmitSeedDraft()
    {
        var create = await client.GetAsync("/Kpis/Create?persona=creator", TestContext.Current.CancellationToken);
        var createHtml = await create.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var createToken = Regex.Match(createHtml, "name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"").Groups["token"].Value;
        var code = $"GOV_{Guid.NewGuid():N}"[..18].ToUpperInvariant();
        using var createForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = createToken,
            ["Code"] = code,
            ["Name"] = "Governed UI KPI",
            ["Description"] = "KPI created for governance page contract."
        });
        var created = await client.PostAsync("/Kpis/Create?persona=creator", createForm, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        var definitionId = Regex.Match(created.Headers.Location?.OriginalString ?? string.Empty, @"/Kpis/Edit/(?<id>[0-9a-f-]{36})", RegexOptions.IgnoreCase).Groups["id"].Value;
        Assert.NotEmpty(definitionId);

        var edit = await client.GetAsync($"/Kpis/Edit/{definitionId}?persona=creator", TestContext.Current.CancellationToken);
        var html = await edit.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"").Groups["token"].Value;
        using var addVersionForm = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["definitionId"] = definitionId,
            ["VersionName"] = "Governed version",
            ["VersionDescription"] = "Governed version contract.",
            ["Variables"] = "revenue",
            ["Source"] = "revenue",
            ["ChangeSummary"] = "Initial governed version"
        });
        var added = await client.PostAsync("/Kpis/AddVersion?persona=creator", addVersionForm, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, added.StatusCode);

        edit = await client.GetAsync($"/Kpis/Edit/{definitionId}?persona=creator", TestContext.Current.CancellationToken);
        html = await edit.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var versionId = Regex.Match(html, "name=\"VersionId\"[^>]*value=\"(?<id>[0-9a-f-]{36})\"", RegexOptions.IgnoreCase).Groups["id"].Value;
        token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"").Groups["token"].Value;
        Assert.NotEmpty(versionId);
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["definitionId"] = definitionId,
            ["versionId"] = versionId
        });
        var response = await client.PostAsync("/Kpis/Submit?persona=creator", form, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return definitionId;
    }
}
