using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Kpi.IntegrationTests.Web;

public sealed class KpiApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public KpiApiSmokeTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Formula_validation_returns_server_generated_ast()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/formulas/validate", new
        {
            source = "revenue / target * 100",
            declaredResultType = "Decimal",
            variables = new[]
            {
                new { code = "revenue", displayName = "Revenue", type = "Decimal", required = true },
                new { code = "target", displayName = "Target", type = "Decimal", required = true }
            }
        }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<FormulaResponse>(TestContext.Current.CancellationToken);
        Assert.True(body!.Valid);
        Assert.Equal("binary", body.Formula!.Ast.NodeType);
    }

    private sealed record FormulaResponse(bool Valid, FormulaDocumentResponse? Formula);
    private sealed record FormulaDocumentResponse(string Source, AstResponse Ast);
    private sealed record AstResponse(string NodeType);
}
