using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Formula;
using Kpi.Domain.Kpis;
using Xunit;

namespace Kpi.Application.Tests.Lifecycle;

public sealed class ReconcileVersionEffectivityTests
{
    [Fact]
    public void Reconciliation_is_repeatable_for_published_versions()
    {
        var store = new InMemoryKpiStore(); var clock = new Clock(); var ops = new KpiOperations(store, clock); var actor = ActorContext.Demo("creator"); var definition = ops.CreateDefinition(actor, "EFFECT", "Effect", "Test").Value!; var version = ops.CreateVersion(actor, definition.Id, "v1", "First", "1", [], FormulaResultType.Decimal, "Initial").Value!; ops.SubmitVersion(actor, definition.Id, version.Id); ops.ReviewVersion(ActorContext.Demo("approver"), definition.Id, version.Id, true, "ok"); ops.PublishVersion(ActorContext.Demo("approver"), definition.Id, version.Id, clock.UtcNow.AddDays(-1));
        var reconcile = new ReconcileKpiLifecycle(store, clock);
        Assert.Equal(0, reconcile.Execute());
    }

    [Fact]
    public void Successor_effective_date_closes_predecessor_range_and_reconciliation_retires_once()
    {
        var store = new InMemoryKpiStore(); var clock = new FixedClock(DateTimeOffset.UtcNow);
        var ops = new KpiOperations(store, clock); var creator = ActorContext.Demo("creator"); var approver = ActorContext.Demo("approver");
        var definition = ops.CreateDefinition(creator, "HANDOFF", "Handoff", "Test").Value!;
        var first = ops.CreateVersion(creator, definition.Id, "v1", "First", "1", [], FormulaResultType.Decimal, "Initial").Value!;
        ops.SubmitVersion(creator, definition.Id, first.Id); ops.ReviewVersion(approver, definition.Id, first.Id, true, "ok");
        ops.PublishVersion(approver, definition.Id, first.Id, clock.UtcNow.AddDays(-2));
        var second = ops.CreateVersion(creator, definition.Id, "v2", "Second", "2", [], FormulaResultType.Decimal, "Successor").Value!;
        ops.SubmitVersion(creator, definition.Id, second.Id); ops.ReviewVersion(approver, definition.Id, second.Id, true, "ok");
        var effective = clock.UtcNow.AddDays(-1); ops.PublishVersion(approver, definition.Id, second.Id, effective);
        Assert.Equal(effective, first.EffectiveTo);
        var reconcile = new ReconcileKpiLifecycle(store, clock); Assert.Equal(1, reconcile.Execute()); Assert.Equal(KpiVersionStatus.Retired, first.Status); Assert.Equal(0, reconcile.Execute());
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock { public DateTimeOffset UtcNow => now; }
    private sealed class Clock : IClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }
}
