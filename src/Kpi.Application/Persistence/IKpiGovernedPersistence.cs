using Kpi.Domain.Auditing;
using Kpi.Domain.Evaluations;
using Kpi.Domain.Periods;

namespace Kpi.Application.Persistence;

/// <summary>Persistence port for Period, Evaluation and append-only Audit snapshots.</summary>
public interface IKpiGovernedPersistence
{
    /// <summary>Executes all governed writes in one durable transaction; in-memory adapters execute directly.</summary>
    void ExecuteInTransaction(Action mutation) => mutation();
    void SavePeriod(KpiPeriod period);
    void SaveEvaluation(Guid organizationId, KpiEvaluation evaluation);
    void SaveAudit(AuditRecord record);
}
