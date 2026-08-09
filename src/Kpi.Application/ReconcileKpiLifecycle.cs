using Kpi.Application.Common;
using Kpi.Domain.Auditing;
using Kpi.Domain.Kpis;

namespace Kpi.Application;

/// <summary>Single idempotent lifecycle seam for due Version/Period transitions.</summary>
public sealed class ReconcileKpiLifecycle(InMemoryKpiStore store, IClock clock)
{
    public int Execute()
    {
        var changed = 0; var actor = ActorContext.Demo("admin");
        foreach (var definition in store.Definitions)
            foreach (var version in definition.Versions.Where(x => x.Status == KpiVersionStatus.Published && x.EffectiveFrom <= clock.UtcNow))
            {
                foreach (var predecessor in definition.Versions.Where(x => x != version && x.Status == KpiVersionStatus.Published && x.EffectiveFrom < version.EffectiveFrom))
                { predecessor.Retire(clock.UtcNow); store.AddAudit(AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_VERSION", predecessor.Id, AuditEventType.Retired, clock.UtcNow, actor.CorrelationId)); changed++; }
            }
        foreach (var period in store.Periods)
        {
            if (period.Status == Kpi.Domain.Periods.KpiPeriodStatus.Scheduled && period.StartsAt <= clock.UtcNow) { period.Activate(clock.UtcNow); store.AddAudit(AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_PERIOD", period.Id, AuditEventType.PeriodChanged, clock.UtcNow, actor.CorrelationId, summary: "Activated")); changed++; }
            if (period.Status == Kpi.Domain.Periods.KpiPeriodStatus.Active && period.EndsAt <= clock.UtcNow) { period.Close(clock.UtcNow); store.AddAudit(AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_PERIOD", period.Id, AuditEventType.PeriodChanged, clock.UtcNow, actor.CorrelationId, summary: "Closed")); changed++; }
        }
        return changed;
    }
}
