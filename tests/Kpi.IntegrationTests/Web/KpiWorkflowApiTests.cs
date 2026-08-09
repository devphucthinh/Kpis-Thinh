using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Kpi.IntegrationTests.Web;

public sealed class KpiWorkflowApiTests : IClassFixture<Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public KpiWorkflowApiTests(Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Creator_submit_and_separate_approver_publish_version()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var create = await _client.PostAsJsonAsync("/api/v1/kpis?persona=creator", new { code = $"TEST_{suffix}", name = "Test KPI", description = "Integration" }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var definition = await create.Content.ReadFromJsonAsync<DefinitionResponse>(TestContext.Current.CancellationToken);
        var version = await _client.PostAsJsonAsync($"/api/v1/kpis/{definition!.Id}/versions?persona=creator", new { name = "v1", description = "First", source = "revenue", resultType = "Decimal", changeSummary = "Initial", variables = new[] { new { code = "revenue", displayName = "Revenue", type = "Decimal", required = true } } }, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, version.StatusCode);
        var versionBody = await version.Content.ReadFromJsonAsync<VersionResponse>(TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync($"/api/v1/kpis/{definition.Id}/versions/{versionBody!.Id}/submit?persona=creator", null, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync($"/api/v1/kpis/{definition.Id}/versions/{versionBody.Id}/review?persona=approver", new { approve = true, comment = "Approved" }, TestContext.Current.CancellationToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync($"/api/v1/kpis/{definition.Id}/versions/{versionBody.Id}/publish?persona=approver", new { effectiveFrom = DateTimeOffset.UtcNow }, TestContext.Current.CancellationToken)).StatusCode);
    }

    private sealed record DefinitionResponse(Guid Id);
    private sealed record VersionResponse(Guid Id);
}
