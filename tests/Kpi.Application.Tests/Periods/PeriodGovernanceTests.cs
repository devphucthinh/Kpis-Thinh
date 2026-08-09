using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Formula;
using Kpi.Domain.Periods;
using Xunit;

namespace Kpi.Application.Tests.Periods;

public sealed class PeriodGovernanceTests
{
    [Fact]
    public void Selection_rejects_unpublished_or_wrong_cadence_version()
    {
        var store = new InMemoryKpiStore(); var clock = new Clock(); var kpis = new KpiOperations(store, clock); var periods = new PeriodOperations(store, clock); var creator = ActorContext.Demo("creator");
        var definition = kpis.CreateDefinition(creator, "PERIOD_ELIGIBILITY", "Period", "Test").Value!;
        var version = kpis.CreateVersion(creator, definition.Id, "v1", "Version", "1", [], FormulaResultType.Decimal, "Initial", KpiCadence.Quarterly).Value!;
        var period = periods.Create(ActorContext.Demo("planner"), "Q1", "Quarter", "Test", KpiCadence.Monthly, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMonths(1)).Value!;
        var result = periods.Select(ActorContext.Demo("planner"), period.Id, definition.Id, version.Id);
        Assert.False(result.IsSuccess);
        Assert.Equal("PERIOD_ELIGIBILITY_CONFLICT", result.Error!.Code);
    }

    [Fact]
    public void Same_cadence_period_overlap_is_rejected_on_submit()
    {
        var store = new InMemoryKpiStore(); var clock = new Clock(); var periods = new PeriodOperations(store, clock); var planner = ActorContext.Demo("planner");
        var first = periods.Create(planner, "P1", "P1", "Test", KpiCadence.Monthly, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMonths(1)).Value!;
        var second = periods.Create(planner, "P2", "P2", "Test", KpiCadence.Monthly, DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddMonths(1).AddDays(1)).Value!;
        first.Select(Guid.NewGuid(), Guid.NewGuid());
        second.Select(Guid.NewGuid(), Guid.NewGuid());
        Assert.True(periods.Submit(planner, first.Id).IsSuccess);
        var result = periods.Submit(planner, second.Id);
        Assert.Equal("PERIOD_ELIGIBILITY_CONFLICT", result.Error!.Code);
    }

    [Fact]
    public void Scheduled_amendment_is_in_review_until_a_distinct_approver_reviews_it()
    {
        var store = new InMemoryKpiStore(); var clock = new Clock(); var kpis = new KpiOperations(store, clock); var periods = new PeriodOperations(store, clock);
        var creator = ActorContext.Demo("creator"); var approver = ActorContext.Demo("approver"); var planner = ActorContext.Demo("planner");
        var definition = kpis.CreateDefinition(creator, "AMEND_REVIEW", "Amendment", "Test").Value!;
        var version = kpis.CreateVersion(creator, definition.Id, "v1", "Version", "1", [], FormulaResultType.Decimal, "Initial").Value!;
        Assert.True(kpis.SubmitVersion(creator, definition.Id, version.Id).IsSuccess);
        Assert.True(kpis.ReviewVersion(approver, definition.Id, version.Id, true, "ok").IsSuccess);
        Assert.True(kpis.PublishVersion(approver, definition.Id, version.Id, clock.UtcNow.AddDays(-1)).IsSuccess);
        var period = periods.Create(planner, "AMEND", "Amend", "Test", KpiCadence.Monthly, clock.UtcNow, clock.UtcNow.AddMonths(1)).Value!;
        Assert.True(periods.Select(planner, period.Id, definition.Id, version.Id).IsSuccess);
        Assert.True(periods.Submit(planner, period.Id).IsSuccess);
        Assert.True(periods.Approve(approver, period.Id).IsSuccess);

        var proposed = periods.ProposeAmendment(planner, period.Id, new Dictionary<Guid, Guid> { [definition.Id] = version.Id }, "Sửa mục tiêu");
        Assert.True(proposed.IsSuccess);
        Assert.Equal(KpiPeriodAmendmentStatus.InReview, proposed.Value!.Status);
        Assert.Equal(0, period.LatestEffectiveRevision);
        var reviewed = periods.ReviewAmendment(approver, period.Id, proposed.Value.Id, true, "Duyệt");
        Assert.True(reviewed.IsSuccess);
        Assert.Equal(1, period.LatestEffectiveRevision);
    }

    private sealed class Clock : IClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }
}
