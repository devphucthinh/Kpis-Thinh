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
    IReadOnlyList<KpiPeriod> LoadPeriods(Guid organizationId) => [];
    IReadOnlyList<KpiEvaluation> LoadEvaluations(Guid organizationId, Guid definitionId) => [];
    IReadOnlyList<AuditRecord> LoadAudit(AuditQuery query) => [];
}

public sealed record AuditQuery(Guid OrganizationId, string? EntityType = null, Guid? EntityId = null, Guid? ActorId = null, AuditEventType? EventType = null, DateTimeOffset? From = null, DateTimeOffset? To = null);
