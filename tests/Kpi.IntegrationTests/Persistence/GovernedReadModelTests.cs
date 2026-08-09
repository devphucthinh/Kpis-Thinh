using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Application.Persistence;
using Kpi.Domain.Auditing;
using Kpi.Domain.Evaluations;
using Kpi.Domain.Formula;
using Kpi.Domain.Kpis;
using Kpi.Domain.Periods;
using Xunit;

namespace Kpi.IntegrationTests.Persistence;

public sealed class GovernedReadModelTests
{
    [Fact]
    public void Period_operations_refresh_periods_from_governed_persistence()
    {
        var actor = ActorContext.Demo("planner");
        var period = KpiPeriod.Create(actor.OrganizationId, "DURABLE", "Durable", "Reloaded Period", KpiCadence.Monthly, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1), actor.ActorId);
        var persistence = new FakeGovernedPersistence { Periods = [period] };
        var operations = new PeriodOperations(new InMemoryKpiStore(), new FixedClock(), persistence);

        var loaded = operations.List(actor.OrganizationId);

        Assert.Single(loaded);
        Assert.Equal(period.Id, loaded[0].Id);
    }

    [Fact]
    public void Audit_query_uses_durable_read_seam_after_process_restart()
    {
        var actor = ActorContext.Demo("observer");
        var audit = AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_VERSION", Guid.NewGuid(), AuditEventType.Published, DateTimeOffset.UtcNow, "durable-test", summary: "Loaded from persistence");
        var persistence = new FakeGovernedPersistence { Audit = [audit] };
        var operations = new KpiOperations(new InMemoryKpiStore(), new FixedClock(), governedPersistence: persistence);

        var loaded = operations.Audit(actor.OrganizationId, eventType: AuditEventType.Published);

        Assert.Single(loaded);
        Assert.Equal("Loaded from persistence", loaded[0].Summary);
    }

    private sealed class FakeGovernedPersistence : IKpiGovernedPersistence
    {
        public IReadOnlyList<KpiPeriod> Periods { get; init; } = [];
        public IReadOnlyList<KpiEvaluation> Evaluations { get; init; } = [];
        public IReadOnlyList<AuditRecord> Audit { get; init; } = [];
        public void SavePeriod(KpiPeriod period) { }
        public void SaveEvaluation(Guid organizationId, KpiEvaluation evaluation) { }
        public void SaveAudit(AuditRecord record) { }
        public IReadOnlyList<KpiPeriod> LoadPeriods(Guid organizationId) => Periods.Where(x => x.OrganizationId == organizationId).ToArray();
        public IReadOnlyList<KpiEvaluation> LoadEvaluations(Guid organizationId, Guid definitionId) => Evaluations;
        public IReadOnlyList<AuditRecord> LoadAudit(AuditQuery query) => Audit.Where(x => x.OrganizationId == query.OrganizationId && (query.EventType is null || x.EventType == query.EventType.Value)).ToArray();
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 8, 9, 0, 0, 0, TimeSpan.Zero);
    }
}
