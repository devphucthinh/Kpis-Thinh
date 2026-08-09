using Kpi.Domain.Periods;
using Kpi.Domain.Kpis;
using Xunit;

namespace Kpi.Domain.Tests.Periods;

public sealed class KpiPeriodLifecycleTests
{
    [Fact]
    public void Rejected_period_returns_to_draft_only_for_planner()
    {
        var planner = Guid.NewGuid(); var approver = Guid.NewGuid();
        var period = KpiPeriod.Create(Guid.NewGuid(), "2026-08", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMonths(1), planner);
        period.Select(Guid.NewGuid(), Guid.NewGuid()); period.Submit(); period.Reject("Need more context");
        Assert.Throws<KpiDomainException>(() => period.ReturnToDraft(approver));
        period.ReturnToDraft(planner);
        Assert.Equal(KpiPeriodStatus.Draft, period.Status);
    }

    [Fact]
    public void Scheduled_amendment_creates_immutable_effective_revision()
    {
        var planner = Guid.NewGuid(); var approver = Guid.NewGuid(); var definition = Guid.NewGuid(); var firstVersion = Guid.NewGuid(); var secondVersion = Guid.NewGuid();
        var period = KpiPeriod.Create(Guid.NewGuid(), "2026-08", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMonths(1), planner);
        period.Select(definition, firstVersion); period.Submit(); period.Approve(approver); period.Amend(planner, approver, new Dictionary<Guid, Guid> { [definition] = secondVersion }, "Target updated");
        Assert.Equal(1, period.LatestEffectiveRevision);
        Assert.Equal(firstVersion, period.EffectiveRevisions[0].Selections[definition]);
        Assert.Equal(secondVersion, period.EffectiveRevisions[1].Selections[definition]);
    }

    [Fact]
    public void Scheduled_amendment_waits_for_distinct_review()
    {
        var planner = Guid.NewGuid(); var approver = Guid.NewGuid(); var definition = Guid.NewGuid(); var firstVersion = Guid.NewGuid(); var secondVersion = Guid.NewGuid();
        var period = KpiPeriod.Create(Guid.NewGuid(), "2026-09", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMonths(1), planner);
        period.Select(definition, firstVersion); period.Submit(); period.Approve(approver);
        var amendment = period.ProposeAmendment(planner, new Dictionary<Guid, Guid> { [definition] = secondVersion }, period.StartsAt, period.EndsAt, "Target updated");
        Assert.Equal(KpiPeriodAmendmentStatus.InReview, amendment.Status);
        Assert.Equal(0, period.LatestEffectiveRevision);
        period.ReviewAmendment(approver, amendment.Id, true, "Approved");
        Assert.Equal(1, period.LatestEffectiveRevision);
        Assert.Equal(KpiPeriodAmendmentStatus.Approved, amendment.Status);
    }
}
