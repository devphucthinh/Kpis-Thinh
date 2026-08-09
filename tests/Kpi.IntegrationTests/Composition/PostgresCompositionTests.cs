using Kpi.Infrastructure.Postgres;
using Kpi.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kpi.IntegrationTests.Composition;

public sealed class PostgresCompositionTests
{
    [Fact]
    public void Runtime_persistence_uses_KpiRuntime_and_ignores_legacy_or_migration_connections()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Kpi"] = "Host=legacy;Database=legacy;Username=legacy",
                ["ConnectionStrings:KpiRuntime"] = "Host=runtime;Database=kpi_lab;Username=runtime",
                ["ConnectionStrings:KpiMigration"] = "Host=migration;Database=kpi_lab;Username=migration"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddKpiPostgres(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<KpiDbContext>();

        Assert.Equal("Host=runtime;Database=kpi_lab;Username=runtime", context.Database.GetDbConnection().ConnectionString);
    }

    [Fact]
    public void Migration_only_configuration_does_not_register_runtime_persistence()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:KpiMigration"] = "Host=migration;Database=kpi_lab;Username=migration"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddKpiPostgres(configuration);

        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(KpiDbContext));
    }
}
