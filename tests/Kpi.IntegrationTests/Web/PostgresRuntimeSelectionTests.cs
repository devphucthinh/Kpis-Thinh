using Kpi.Application;
using Kpi.Application.Authorization;
using Kpi.Application.Persistence;
using Kpi.Infrastructure.Postgres.Persistence;
using Kpi.Web.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kpi.IntegrationTests.Web;

public sealed class PostgresRuntimeSelectionTests
{
    [Fact]
    public void Configured_runtime_profile_does_not_register_inmemory_source_of_truth()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Kpi:PersistenceProfile"] = "Postgres",
                ["ConnectionStrings:KpiRuntime"] = "Host=runtime;Database=kpi_lab;Username=runtime",
                ["ConnectionStrings:KpiMigration"] = "Host=migration;Database=kpi_lab;Username=migration"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddSingleton<Kpi.Application.Common.IClock, Kpi.Application.Common.SystemClock>();
        PostgresRuntimeConfiguration.AddPersistence(services, configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.Null(scope.ServiceProvider.GetService<InMemoryKpiStore>());
        var context = scope.ServiceProvider.GetRequiredService<KpiDbContext>();
        Assert.Equal("Host=runtime;Database=kpi_lab;Username=runtime", context.Database.GetDbConnection().ConnectionString);
        Assert.NotNull(scope.ServiceProvider.GetService<IAuthorizationDecision>());
        Assert.NotNull(scope.ServiceProvider.GetService<IAuditWriter>());
        Assert.NotNull(scope.ServiceProvider.GetService<IOrganizationTransaction>());
    }
}
