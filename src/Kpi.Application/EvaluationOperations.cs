using Kpi.Application.Common;
using Kpi.Domain.Auditing;
using Kpi.Domain.Evaluations;
using Kpi.Domain.Formula;
using Kpi.Domain.Kpis;
using Kpi.Domain.Periods;

namespace Kpi.Application;

/// <summary>Official Evaluation and correction commands; Test Run remains transient in FormulaService.</summary>
public sealed class EvaluationOperations(InMemoryKpiStore store, IClock clock, Persistence.IKpiGovernedPersistence? persistence = null)
{
    private readonly Persistence.IKpiGovernedPersistence? _persistence = persistence;

    public ApplicationResult<KpiEvaluation> Evaluate(ActorContext actor, Guid definitionId, Guid versionId, Guid activationId, FormulaDocument formula, IReadOnlyList<FormulaVariableDefinition> variables, IReadOnlyDictionary<string, FormulaValue> inputs)
    {
        if (!actor.Can(KpiCapability.Evaluate)) return ApplicationResult<KpiEvaluation>.Failure("AUTHORIZATION_DENIED", "Actor cannot evaluate KPI.", 403);
        var activation = store.FindActivation(activationId);
        if (activation is null || activation.Value.Period.OrganizationId != actor.OrganizationId || activation.Value.Activation.DefinitionId != definitionId || activation.Value.Activation.VersionId != versionId)
            return ApplicationResult<KpiEvaluation>.Failure("ACTIVATION_REQUIRED", "Evaluation requires a matching Active Period Activation.", 409);
        if (activation.Value.Period.Status == KpiPeriodStatus.Closed)
            return ApplicationResult<KpiEvaluation>.Failure("PERIOD_CLOSED", "Ordinary Evaluation is not allowed after the Period is closed.", 409);
        if (activation.Value.Period.Status != KpiPeriodStatus.Active || activation.Value.Activation.ClosedAt is not null)
            return ApplicationResult<KpiEvaluation>.Failure("ACTIVATION_REQUIRED", "Evaluation requires a matching Active Period Activation.", 409);
        var definition = store.Find(definitionId);
        if (definition is null || definition.OrganizationId != actor.OrganizationId) return ApplicationResult<KpiEvaluation>.Failure("RESOURCE_NOT_FOUND", "KPI was not found.", 404);
        var version = definition.Versions.FirstOrDefault(x => x.Id == versionId);
        if (version is null || !string.Equals(version.Formula.Checksum, formula.Checksum, StringComparison.Ordinal)) return ApplicationResult<KpiEvaluation>.Failure("FORMULA_SNAPSHOT_CONFLICT", "Evaluation must use the exact published Formula Document.", 409);
        var evaluation = store.Stream(definitionId).Evaluate(definitionId, versionId, version.Formula, version.Variables, inputs, clock.UtcNow, activationId, actor.ActorId);
        Save(evaluation, AuditEventType.Evaluated, actor);
        return ApplicationResult<KpiEvaluation>.Success(evaluation);
    }

    public IReadOnlyList<KpiEvaluation> History(Guid definitionId, Guid? organizationId = null)
    {
        RefreshFromPersistence(definitionId, organizationId);
        return organizationId is null || store.Find(definitionId)?.OrganizationId == organizationId.Value ? store.Stream(definitionId).Attempts : [];
    }

    public KpiEvaluation? Current(Guid definitionId, Guid? organizationId = null)
    {
        RefreshFromPersistence(definitionId, organizationId);
        return organizationId is null || store.Find(definitionId)?.OrganizationId == organizationId.Value ? store.Stream(definitionId).Current : null;
    }

    public ApplicationResult<KpiEvaluation> Correct(ActorContext actor, Guid definitionId, Guid activationId, Guid predecessorId, Guid versionId, FormulaDocument formula, IReadOnlyList<FormulaVariableDefinition> variables, IReadOnlyDictionary<string, FormulaValue> inputs, string reason)
    {
        if (!actor.Can(KpiCapability.Evaluate)) return ApplicationResult<KpiEvaluation>.Failure("AUTHORIZATION_DENIED", "Actor cannot correct KPI evaluation.", 403);
        var activation = store.FindActivation(activationId);
        if (activation is null || activation.Value.Period.OrganizationId != actor.OrganizationId) return ApplicationResult<KpiEvaluation>.Failure("ACTIVATION_REQUIRED", "Correction requires the original Period Activation.", 409);
        if (activation.Value.Period.Status != KpiPeriodStatus.Closed) return ApplicationResult<KpiEvaluation>.Failure("PERIOD_NOT_CLOSED", "Corrections are only allowed after a Period is closed.", 409);
        if (activation.Value.Activation.DefinitionId != definitionId || activation.Value.Activation.VersionId != versionId) return ApplicationResult<KpiEvaluation>.Failure("CORRECTION_CONFLICT", "Correction must use the Activation's Version.", 409);
        var predecessor = store.Stream(definitionId).Attempts.FirstOrDefault(x => x.Id == predecessorId);
        if (predecessor is null || predecessor.VersionId != versionId || predecessor.ActivationId != activationId) return ApplicationResult<KpiEvaluation>.Failure("CORRECTION_CONFLICT", "Predecessor, Activation or Version is invalid.", 409);
        var definition = store.Find(definitionId);
        if (definition is null || definition.OrganizationId != actor.OrganizationId) return ApplicationResult<KpiEvaluation>.Failure("RESOURCE_NOT_FOUND", "KPI was not found.", 404);
        var version = definition.Versions.FirstOrDefault(x => x.Id == versionId);
        if (version is null || !string.Equals(version.Formula.Checksum, formula.Checksum, StringComparison.Ordinal)) return ApplicationResult<KpiEvaluation>.Failure("FORMULA_SNAPSHOT_CONFLICT", "Correction must use the exact published Formula Document.", 409);
        try
        {
            var evaluation = store.Stream(definitionId).Correct(predecessor, version.Formula, version.Variables, inputs, reason, clock.UtcNow, actor.ActorId);
            Save(evaluation, AuditEventType.Corrected, actor, reason);
            return ApplicationResult<KpiEvaluation>.Success(evaluation);
        }
        catch (KpiDomainException ex) { return ApplicationResult<KpiEvaluation>.Failure("CORRECTION_CONFLICT", ex.Message, 409); }
    }

    private void Save(KpiEvaluation evaluation, AuditEventType eventType, ActorContext actor, string? reason = null)
    {
        var audit = AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_EVALUATION", evaluation.Id, eventType, clock.UtcNow, actor.CorrelationId, reason: reason);
        if (_persistence is null) { store.AddAudit(audit); return; }
        _persistence.ExecuteInTransaction(() => { _persistence.SaveEvaluation(actor.OrganizationId, evaluation); _persistence.SaveAudit(audit); });
        store.AddAudit(audit);
    }

    private void RefreshFromPersistence(Guid definitionId, Guid? organizationId)
    {
        if (_persistence is null || organizationId is null) return;
        var loaded = _persistence.LoadEvaluations(organizationId.Value, definitionId);
        if (loaded.Count > 0) store.ReplaceEvaluations(definitionId, loaded);
    }
}
