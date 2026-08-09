using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Formula;
using Xunit;

namespace Kpi.IntegrationTests.Persistence;

public sealed class DevelopmentSeedDataTests
{
    [Fact]
    public void Development_composition_can_seed_an_example_idempotently()
    {
        var store = new InMemoryKpiStore(); var ops = new KpiOperations(store, new Clock()); var actor = ActorContext.Demo("creator");
        var created = ops.CreateDefinition(actor, "SEED", "Seed", "Development"); ops.CreateVersion(actor, created.Value!.Id, "v1", "Seed", "1", [], FormulaResultType.Decimal, "seed");
        Assert.Single(store.Definitions); Assert.Single(store.Definitions[0].Versions);
    }
    private sealed class Clock : IClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }
}
