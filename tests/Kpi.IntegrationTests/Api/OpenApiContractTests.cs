using System.Text.RegularExpressions;
using Xunit;

namespace Kpi.IntegrationTests.Api;

public sealed class OpenApiContractTests
{
    [Fact(DisplayName = "FR-048 OpenAPI operation IDs and local references are unique")]
    public void Openapi_contract_has_unique_operation_ids_and_resolvable_local_refs()
    {
        var path = FindRepositoryFile(Path.Combine("specs", "002-organization-authorization", "contracts", "openapi.yaml"));
        var yaml = File.ReadAllText(path);
        var operationIds = Regex.Matches(yaml, @"(?m)^\s+operationId:\s*(?<id>[A-Za-z0-9_.-]+)\s*$")
            .Select(match => match.Groups["id"].Value)
            .ToArray();
        Assert.NotEmpty(operationIds);
        Assert.Equal(operationIds.Length, operationIds.Distinct(StringComparer.Ordinal).Count());

        foreach (Match reference in Regex.Matches(yaml, @"#/components/(?:schemas|responses|parameters)/(?<name>[A-Za-z0-9_.-]+)"))
            Assert.Contains($"{reference.Groups["name"].Value}:", yaml, StringComparison.Ordinal);
    }

    private static string FindRepositoryFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }

        throw new FileNotFoundException(relativePath);
    }
}
