using Kpi.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kpi.IntegrationTests.Database;

public sealed class PersistenceModelTests
{
    [Fact]
    public async Task Formula_and_evaluation_snapshots_are_reloadable()
    {
        var options = new DbContextOptionsBuilder<KpiDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var definition = new KpiDefinitionRow { Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), Code = "REVENUE", Name = "Revenue", Description = "Demo" };
        await using (var context = new KpiDbContext(options)) { context.Definitions.Add(definition); context.Versions.Add(new KpiVersionRow { Id = Guid.NewGuid(), DefinitionId = definition.Id, VersionNumber = 1, FormulaJson = "{\"source\":\"revenue / target\",\"ast\":{}}" }); await context.SaveChangesAsync(TestContext.Current.CancellationToken); }
        await using (var context = new KpiDbContext(options)) { var loaded = await context.Versions.SingleAsync(TestContext.Current.CancellationToken); Assert.Contains("revenue / target", loaded.FormulaJson, StringComparison.Ordinal); }
    }
}
