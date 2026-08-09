using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Formula;
using Kpi.Domain.Kpis;
using Kpi.Domain.Periods;
using Xunit;

namespace Kpi.Application.Tests.Evaluations;

public sealed class OfficialEvaluationGovernanceTests
{
    [Fact]
    public void Official_evaluation_requires_an_active_activation_and_snapshots_it()
    {
        var fixture = Fixture.Create();
        var beforeActivation = fixture.Evaluations.Evaluate(fixture.Evaluator, fixture.Definition.Id, fixture.Version.Id, Guid.NewGuid(), fixture.Version.Formula, fixture.Version.Variables, Inputs(10));
        Assert.False(beforeActivation.IsSuccess);
        Assert.Equal("ACTIVATION_REQUIRED", beforeActivation.Error!.Code);

        fixture.PeriodOperations.Activate(fixture.Planner, fixture.Period.Id);
        var activation = Assert.Single(fixture.Period.Activations);
        var result = fixture.Evaluations.Evaluate(fixture.Evaluator, fixture.Definition.Id, fixture.Version.Id, activation.Id, fixture.Version.Formula, fixture.Version.Variables, Inputs(10));

        Assert.True(result.IsSuccess, result.Error?.Message);
        var evaluation = result.Value!;
        Assert.Equal(activation.Id, evaluation.ActivationId);
        Assert.Equal(fixture.Version.Id, evaluation.VersionId);
        Assert.Equal(fixture.Version.Formula, evaluation.FormulaSnapshot);
        Assert.Equal(10m, Assert.IsType<DecimalFormulaValue>(Assert.IsType<EvaluationSuccess>(evaluation.Outcome).Value).Value);
    }

    [Fact]
    public void Closed_period_allows_reasoned_same_version_correction_but_not_ordinary_evaluation()
    {
        var fixture = Fixture.Create();
        fixture.PeriodOperations.Activate(fixture.Planner, fixture.Period.Id);
        var activation = Assert.Single(fixture.Period.Activations);
        var originalResult = fixture.Evaluations.Evaluate(fixture.Evaluator, fixture.Definition.Id, fixture.Version.Id, activation.Id, fixture.Version.Formula, fixture.Version.Variables, Inputs(25));
        Assert.True(originalResult.IsSuccess);
        var original = originalResult.Value!;
        fixture.PeriodOperations.Close(fixture.Planner, fixture.Period.Id);

        var ordinary = fixture.Evaluations.Evaluate(fixture.Evaluator, fixture.Definition.Id, fixture.Version.Id, activation.Id, fixture.Version.Formula, fixture.Version.Variables, Inputs(30));
        Assert.Equal("PERIOD_CLOSED", ordinary.Error!.Code);

        var correction = fixture.Evaluations.Correct(fixture.Evaluator, fixture.Definition.Id, activation.Id, original.Id, fixture.Version.Id, fixture.Version.Formula, fixture.Version.Variables, Inputs(30), "Sửa dữ liệu nguồn");
        Assert.True(correction.IsSuccess, correction.Error?.Message);
        var corrected = correction.Value!;
        Assert.Equal(original.Id, corrected.SupersedesId);
        Assert.Contains("25", corrected.CorrectionDiff!.OldResult);
        Assert.Contains("30", corrected.CorrectionDiff.NewResult);
        Assert.Equal(corrected.Id, fixture.Evaluations.Current(fixture.Definition.Id)!.Id);
    }

    [Fact]
    public void Failed_evaluation_is_retained_but_never_replaces_current_success()
    {
        var fixture = Fixture.Create();
        fixture.PeriodOperations.Activate(fixture.Planner, fixture.Period.Id);
        var activation = Assert.Single(fixture.Period.Activations);
        var successResult = fixture.Evaluations.Evaluate(fixture.Evaluator, fixture.Definition.Id, fixture.Version.Id, activation.Id, fixture.Version.Formula, fixture.Version.Variables, Inputs(25));
        Assert.True(successResult.IsSuccess);
        var success = successResult.Value!;
        var failed = fixture.Evaluations.Evaluate(fixture.Evaluator, fixture.Definition.Id, fixture.Version.Id, activation.Id, fixture.Version.Formula, fixture.Version.Variables, new Dictionary<string, FormulaValue>());

        Assert.IsType<EvaluationFailure>(failed.Value!.Outcome);
        Assert.Equal(success.Id, fixture.Evaluations.Current(fixture.Definition.Id)!.Id);
        Assert.Equal(2, fixture.Evaluations.History(fixture.Definition.Id).Count);
    }

    private static IReadOnlyDictionary<string, FormulaValue> Inputs(decimal value) => new Dictionary<string, FormulaValue> { ["value"] = FormulaValue.Decimal(value) };

    private sealed class Fixture
    {
        public required InMemoryKpiStore Store { get; init; }
        public required KpiOperations Kpis { get; init; }
        public required PeriodOperations PeriodOperations { get; init; }
        public required EvaluationOperations Evaluations { get; init; }
        public required KpiDefinition Definition { get; init; }
        public required KpiVersion Version { get; init; }
        public required KpiPeriod Period { get; init; }
        public required ActorContext Planner { get; init; }
        public required ActorContext Evaluator { get; init; }

        public static Fixture Create()
        {
            var store = new InMemoryKpiStore();
            var clock = new FixedClock(new DateTimeOffset(2026, 8, 9, 0, 0, 0, TimeSpan.Zero));
            var kpis = new KpiOperations(store, clock);
            var creator = ActorContext.Demo("creator");
            var approver = ActorContext.Demo("approver");
            var planner = ActorContext.Demo("planner");
            var evaluator = ActorContext.Demo("evaluator");
            var definition = kpis.CreateDefinition(creator, "OFFICIAL_EVAL", "Official Evaluation", "Governance test").Value!;
            var variable = FormulaVariableDefinition.Create("value", "Value", FormulaValueType.Decimal);
            var version = kpis.CreateVersion(creator, definition.Id, "v1", "Initial", "value", [variable], FormulaResultType.Decimal, "Initial").Value!;
            Assert.True(kpis.SubmitVersion(creator, definition.Id, version.Id).IsSuccess);
            Assert.True(kpis.ReviewVersion(approver, definition.Id, version.Id, true, "Approved").IsSuccess);
            Assert.True(kpis.PublishVersion(approver, definition.Id, version.Id, clock.UtcNow.AddDays(-1)).IsSuccess);
            var periods = new PeriodOperations(store, clock);
            var period = periods.Create(planner, "AUG-2026", "August 2026", "Evaluation period", KpiCadence.Monthly, clock.UtcNow.AddDays(-1), clock.UtcNow).Value!;
            Assert.True(periods.Select(planner, period.Id, definition.Id, version.Id).IsSuccess);
            Assert.True(periods.Submit(planner, period.Id).IsSuccess);
            Assert.True(periods.Approve(approver, period.Id).IsSuccess);
            return new() { Store = store, Kpis = kpis, PeriodOperations = periods, Evaluations = new EvaluationOperations(store, clock), Definition = definition, Version = version, Period = period, Planner = planner, Evaluator = evaluator };
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock { public DateTimeOffset UtcNow => now; }
}
