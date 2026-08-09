using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Formula;
using Xunit;

namespace Kpi.Application.Tests;

public sealed class KpiOperationsTests
{
    [Fact]
    public void Creator_cannot_self_approve_and_audit_is_written_for_create()
    {
        var store = new InMemoryKpiStore(); var clock = new FixedClock(); var operations = new KpiOperations(store, clock); var creator = ActorContext.Demo("creator");
        var created = operations.CreateDefinition(creator, "OPERATIONS", "Operations", "Test");
        Assert.True(created.IsSuccess);
        var version = operations.CreateVersion(creator, created.Value!.Id, "v1", "First", "1", [], FormulaResultType.Decimal, "Initial");
        Assert.True(version.IsSuccess);
        Assert.True(operations.SubmitVersion(creator, created.Value.Id, version.Value!.Id).IsSuccess);
        var selfReview = operations.ReviewVersion(creator, created.Value.Id, version.Value.Id, true, "No");
        Assert.Equal("SELF_APPROVAL_FORBIDDEN", selfReview.Error!.Code);
        Assert.Equal(3, store.Audit.Count);
    }

    private sealed class FixedClock : IClock { public DateTimeOffset UtcNow => new(2026, 8, 9, 0, 0, 0, TimeSpan.Zero); }
}
