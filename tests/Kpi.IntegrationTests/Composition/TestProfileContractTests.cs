using System.Text.Json;
using Xunit;

namespace Kpi.IntegrationTests.Composition;

public sealed class TestProfileContractTests
{
    [Fact(DisplayName = "FR-048 Thinh-KPI-TEST selects the explicit PostgreSQL runtime profile")]
    public void Thinh_kpi_test_profile_is_postgres_and_does_not_embed_credentials()
    {
        var path = FindRepositoryFile(Path.Combine("src", "Kpi.Web", "Properties", "launchSettings.json"));
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var profile = document.RootElement.GetProperty("profiles").GetProperty("Thinh-KPI-TEST");
        Assert.Equal("Postgres", profile.GetProperty("environmentVariables").GetProperty("Kpi__PersistenceProfile").GetString());
        Assert.DoesNotContain("Password=", File.ReadAllText(path), StringComparison.OrdinalIgnoreCase);
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
