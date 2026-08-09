using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Formula;
using Xunit;

namespace Kpi.Application.Tests.Kpis;

public sealed class VersionLineageCommandTests
{
    [Fact]
    public void Sequential_versions_keep_change_summary()
    {
        var ops = new KpiOperations(new InMemoryKpiStore(), new Clock()); var actor = ActorContext.Demo("creator"); var definition = ops.CreateDefinition(actor, "LINEAGE", "Lineage", "Test").Value!;
        var first = ops.CreateVersion(actor, definition.Id, "v1", "First", "1", [], FormulaResultType.Decimal, "Initial").Value!; var second = ops.CreateVersion(actor, definition.Id, "v2", "Second", "2", [], FormulaResultType.Decimal, "Updated").Value!;
        Assert.Equal(first.VersionNumber + 1, second.VersionNumber); Assert.Equal("Updated", second.ChangeSummary);
    }
    private sealed class Clock : IClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }
}
