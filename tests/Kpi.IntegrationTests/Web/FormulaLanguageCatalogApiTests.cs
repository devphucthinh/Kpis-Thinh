using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Kpi.IntegrationTests.Web;

public sealed class FormulaLanguageCatalogApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public FormulaLanguageCatalogApiTests(WebApplicationFactory<Program> factory) => client = factory.CreateClient();

    [Fact]
    public async Task Capabilities_lists_supported_operations_signatures_examples_and_versions()
    {
        var response = await client.GetAsync("/api/v1/formulas/capabilities?persona=creator", TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var root = document.RootElement;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(root.GetProperty("formulaLanguageVersion").GetInt32() > 0);
        Assert.True(root.GetProperty("astSchemaVersion").GetInt32() > 0);
        Assert.Contains("+", Names(root.GetProperty("operators")));
        Assert.Contains("MOD", Names(root.GetProperty("operators")));
        Assert.Contains("AND", Names(root.GetProperty("operators")));
        Assert.Contains("OR", Names(root.GetProperty("operators")));
        Assert.Contains("NOT", Names(root.GetProperty("operators")));
        Assert.Contains("IF", Names(root.GetProperty("functions")));
        Assert.Contains("ROUND", Names(root.GetProperty("functions")));
        Assert.Contains("ABS", Names(root.GetProperty("functions")));
        Assert.All(root.GetProperty("functions").EnumerateArray(), item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("signature").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(item.GetProperty("example").GetString()));
        });
    }

    [Fact]
    public async Task Validate_and_test_run_include_the_supported_operation_contract()
    {
        var request = new
        {
            source = "ROUND(value, 2)",
            declaredResultType = "Decimal",
            variables = new[] { new { code = "value", displayName = "Value", type = "Decimal", required = true } }
        };
        var validate = await client.PostAsJsonAsync("/api/v1/formulas/validate?persona=creator", request, TestContext.Current.CancellationToken);
        var testRun = await client.PostAsJsonAsync("/api/v1/formulas/test-run?persona=creator", new { request.source, request.declaredResultType, request.variables, inputs = new { value = 5 } }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, validate.StatusCode);
        Assert.Equal(HttpStatusCode.OK, testRun.StatusCode);
        using var validateDocument = JsonDocument.Parse(await validate.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        using var testRunDocument = JsonDocument.Parse(await testRun.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.True(validateDocument.RootElement.TryGetProperty("supportedOperations", out var validateCatalog));
        Assert.True(testRunDocument.RootElement.TryGetProperty("supportedOperations", out var testRunCatalog));
        Assert.Contains("ROUND", Names(validateCatalog.GetProperty("functions")));
        Assert.Contains("ROUND", Names(testRunCatalog.GetProperty("functions")));
    }

    private static IReadOnlyList<string?> Names(JsonElement array) => array.EnumerateArray().Select(item => item.GetProperty("name").GetString()).ToArray();
}
