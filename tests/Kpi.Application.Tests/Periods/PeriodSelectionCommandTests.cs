using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Formula;
using Kpi.Domain.Kpis;
using Xunit;

namespace Kpi.Application.Tests.Periods;

public sealed class PeriodSelectionCommandTests
{
    [Fact]
    public void SelectMany_is_atomic_when_one_selected_version_is_ineligible()
    {
        var store = new InMemoryKpiStore();
        var clock = new FixedClock();
        var kpis = new KpiOperations(store, clock);
        var creator = ActorContext.Demo("creator");
        var approver = ActorContext.Demo("approver");
        var planner = ActorContext.Demo("planner");
        var first = CreatePublishedKpi(kpis, creator, approver, "ELIGIBLE_ONE");
        var second = CreatePublishedKpi(kpis, creator, approver, "ELIGIBLE_TWO");
        var draft = AssertSuccess(kpis.CreateVersion(creator, second.Id, "Draft v2", "Not published", "revenue", [FormulaVariableDefinition.Create("revenue", "Revenue", FormulaValueType.Decimal)], FormulaResultType.Decimal, "Draft"));
        var periods = new PeriodOperations(store, clock);
        var period = AssertSuccess(periods.Create(planner, "PERIOD_2026_08", "August 2026", "Monthly KPI period", Kpi.Domain.Periods.KpiCadence.Monthly, new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero)));

        var result = periods.SelectMany(planner, period.Id, new Dictionary<Guid, Guid>
        {
            [first.Id] = first.Versions.Single(x => x.Status == KpiVersionStatus.Published).Id,
            [second.Id] = draft.Id
        });

        Assert.False(result.IsSuccess);
        Assert.Equal("PERIOD_ELIGIBILITY_CONFLICT", result.Error!.Code);
        Assert.Empty(period.SelectedVersions);
    }

    private static Kpi.Domain.Kpis.KpiDefinition CreatePublishedKpi(KpiOperations operations, ActorContext creator, ActorContext approver, string code)
    {
        var definition = AssertSuccess(operations.CreateDefinition(creator, code, code, "Published KPI for period selection."));
        var version = AssertSuccess(operations.CreateVersion(creator, definition.Id, "Version 1", "Published", "revenue", [FormulaVariableDefinition.Create("revenue", "Revenue", FormulaValueType.Decimal)], FormulaResultType.Decimal, "Initial"));
        AssertSuccess(operations.SubmitVersion(creator, definition.Id, version.Id));
        AssertSuccess(operations.ReviewVersion(approver, definition.Id, version.Id, true, "Approved for period."));
        AssertSuccess(operations.PublishVersion(approver, definition.Id, version.Id, new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero)));
        return definition;
    }

    private static T AssertSuccess<T>(ApplicationResult<T> result)
    {
        Assert.True(result.IsSuccess, result.Error?.Message);
        return result.Value!;
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 8, 9, 0, 0, 0, TimeSpan.Zero);
    }
}
