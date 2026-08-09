using Kpi.Application.Common;
using Kpi.Domain.Auditing;
using Kpi.Domain.Evaluations;
using Kpi.Domain.Formula;
using Kpi.Domain.Periods;
using Kpi.Domain.Kpis;

namespace Kpi.Application;

/// <summary>Official Evaluation and correction commands; Test Run remains transient in FormulaService.</summary>
public sealed class EvaluationOperations(InMemoryKpiStore store, IClock clock)
{
    public ApplicationResult<KpiEvaluation> Evaluate(ActorContext actor, Guid definitionId, Guid versionId, FormulaDocument formula, IReadOnlyList<FormulaVariableDefinition> variables, IReadOnlyDictionary<string, FormulaValue> inputs)
    {
        if (!actor.Can(KpiCapability.Evaluate)) return ApplicationResult<KpiEvaluation>.Failure("AUTHORIZATION_DENIED", "Actor cannot evaluate KPI.", 403);
        var evaluation = store.Stream(definitionId).Evaluate(definitionId, versionId, formula, variables, inputs, clock.UtcNow);
        store.AddAudit(AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_EVALUATION", evaluation.Id, AuditEventType.Evaluated, clock.UtcNow, actor.CorrelationId));
        return ApplicationResult<KpiEvaluation>.Success(evaluation);
    }
    public IReadOnlyList<KpiEvaluation> History(Guid definitionId) => store.Stream(definitionId).Attempts;
    public KpiEvaluation? Current(Guid definitionId) => store.Stream(definitionId).Current;
    public ApplicationResult<KpiEvaluation> Correct(ActorContext actor, Guid definitionId, Guid predecessorId, Guid versionId, FormulaDocument formula, IReadOnlyList<FormulaVariableDefinition> variables, IReadOnlyDictionary<string, FormulaValue> inputs, string reason)
    {
        if (!actor.Can(KpiCapability.Evaluate)) return ApplicationResult<KpiEvaluation>.Failure("AUTHORIZATION_DENIED", "Actor cannot correct KPI evaluation.", 403);
        var predecessor = store.Stream(definitionId).Attempts.FirstOrDefault(x => x.Id == predecessorId);
        if (predecessor is null || predecessor.VersionId != versionId) return ApplicationResult<KpiEvaluation>.Failure("CORRECTION_CONFLICT", "Predecessor or Version is invalid.", 409);
        try { var evaluation = store.Stream(definitionId).Correct(predecessor, formula, variables, inputs, reason, clock.UtcNow); store.AddAudit(AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_EVALUATION", evaluation.Id, AuditEventType.Corrected, clock.UtcNow, actor.CorrelationId, reason: reason)); return ApplicationResult<KpiEvaluation>.Success(evaluation); } catch (KpiDomainException ex) { return ApplicationResult<KpiEvaluation>.Failure("CORRECTION_CONFLICT", ex.Message, 409); }
    }
}
