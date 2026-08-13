using Kpi.Migrator.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Kpi.IntegrationTests.Composition;

public sealed class MigratorCompositionTests
{
    [Fact(DisplayName = "FR-001 migration composition reads only KpiMigration connection")]
    public void Migrator_options_do_not_fallback_to_runtime_connection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:KpiRuntime"] = "Host=runtime;Database=runtime",
                ["ConnectionStrings:KpiMigration"] = "Host=migration;Database=kpi_lab_test"
            })
            .Build();

        var options = MigratorOptions.FromConfiguration(configuration);

        Assert.Equal("Host=migration;Database=kpi_lab_test", options.MigrationConnectionString);
        Assert.DoesNotContain("runtime", options.MigrationConnectionString, StringComparison.OrdinalIgnoreCase);
    }
}
