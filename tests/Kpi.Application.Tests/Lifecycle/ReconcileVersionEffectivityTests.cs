using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Formula;
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
    private sealed class Clock : IClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }
}
