using Kpi.Application.Common;
using Kpi.Domain.Auditing;
using Kpi.Domain.Formula;
using Kpi.Domain.Kpis;
using Kpi.Application.Persistence;

namespace Kpi.Application;

/// <summary>Governed Definition/Version operations used by HTTP and MVC.</summary>
public sealed class KpiOperations
{
    private readonly InMemoryKpiStore _store;
    private readonly IClock _clock;
    private readonly IKpiDefinitionPersistence? _persistence;
    public KpiOperations(InMemoryKpiStore store, IClock clock, IKpiDefinitionPersistence? persistence = null) { _store = store; _clock = clock; _persistence = persistence; }

    public ApplicationResult<KpiDefinition> CreateDefinition(ActorContext actor, string code, string name, string description)
    {
        if (!actor.Can(KpiCapability.CreateKpi)) return ApplicationResult<KpiDefinition>.Failure("AUTHORIZATION_DENIED", "Actor cannot create KPI.", 403);
        if (_store.FindByCode(code) is not null) return ApplicationResult<KpiDefinition>.Failure("KPI_CODE_CONFLICT", "KPI code already exists.", 409);
        var definition = KpiDefinition.Create(actor.OrganizationId, code, name, description, actor.ActorId);
        _store.AddDefinition(definition, AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_DEFINITION", definition.Id, AuditEventType.Created, _clock.UtcNow, actor.CorrelationId, summary: definition.Code.Value));
        _persistence?.Save(definition);
        return ApplicationResult<KpiDefinition>.Success(definition);
    }

    public ApplicationResult<KpiVersion> CreateVersion(ActorContext actor, Guid definitionId, string name, string description, string source, IReadOnlyList<FormulaVariableDefinition> variables, FormulaResultType resultType, string changeSummary)
    {
        if (!actor.Can(KpiCapability.EditDraft)) return ApplicationResult<KpiVersion>.Failure("AUTHORIZATION_DENIED", "Actor cannot edit KPI drafts.", 403);
        var definition = _store.Find(definitionId); if (definition is null) return ApplicationResult<KpiVersion>.Failure("RESOURCE_NOT_FOUND", "KPI was not found.", 404);
        if (definition.OwnerId != actor.ActorId && !actor.Can(KpiCapability.Administrator)) return ApplicationResult<KpiVersion>.Failure("AUTHORIZATION_DENIED", "Only the owner can edit this draft.", 403);
        try { var version = definition.CreateVersion(name, description, source, variables, resultType, changeSummary); _store.AddAudit(AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_VERSION", version.Id, AuditEventType.Created, _clock.UtcNow, actor.CorrelationId)); _persistence?.Save(definition); return ApplicationResult<KpiVersion>.Success(version); }
        catch (Exception ex) when (ex is ArgumentException or KpiDomainException) { return ApplicationResult<KpiVersion>.Failure("VALIDATION", ex.Message); }
    }

    public ApplicationResult<FormulaCompilation> Validate(ActorContext actor, string source, IReadOnlyList<FormulaVariableDefinition> variables, FormulaResultType resultType) => ApplicationResult<FormulaCompilation>.Success(FormulaEngine.Compile(source, variables, resultType));
    public IReadOnlyList<KpiDefinition> List() => _store.Definitions;
    public IReadOnlyList<AuditRecord> Audit() => _store.Audit;
    public ApplicationResult<KpiVersion> SubmitVersion(ActorContext actor, Guid definitionId, Guid versionId)
    {
        var found = FindVersion(definitionId, versionId); if (found is null) return ApplicationResult<KpiVersion>.Failure("RESOURCE_NOT_FOUND", "KPI Version was not found.", 404);
        if (found.Value.definition.OwnerId != actor.ActorId) return ApplicationResult<KpiVersion>.Failure("AUTHORIZATION_DENIED", "Only the Creator can submit this Version.", 403);
        try { found.Value.version.Submit(); _store.AddAudit(AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_VERSION", versionId, AuditEventType.Submitted, _clock.UtcNow, actor.CorrelationId)); return ApplicationResult<KpiVersion>.Success(found.Value.version); } catch (KpiDomainException ex) { return ApplicationResult<KpiVersion>.Failure("LIFECYCLE_CONFLICT", ex.Message, 409); }
    }
    public ApplicationResult<KpiVersion> ReviewVersion(ActorContext actor, Guid definitionId, Guid versionId, bool approve, string comment)
    {
        var found = FindVersion(definitionId, versionId); if (found is null) return ApplicationResult<KpiVersion>.Failure("RESOURCE_NOT_FOUND", "KPI Version was not found.", 404);
        if (!actor.Can(KpiCapability.ReviewKpi) || actor.ActorId == found.Value.definition.OwnerId) return ApplicationResult<KpiVersion>.Failure("SELF_APPROVAL_FORBIDDEN", "A Creator cannot review their own Version.", 403);
        try { if (approve) found.Value.version.Approve(comment); else found.Value.version.Reject(comment); _store.AddAudit(AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_VERSION", versionId, approve ? AuditEventType.Approved : AuditEventType.Rejected, _clock.UtcNow, actor.CorrelationId, reason: comment)); return ApplicationResult<KpiVersion>.Success(found.Value.version); } catch (KpiDomainException ex) { return ApplicationResult<KpiVersion>.Failure("LIFECYCLE_CONFLICT", ex.Message, 409); }
    }
    public ApplicationResult<KpiVersion> PublishVersion(ActorContext actor, Guid definitionId, Guid versionId, DateTimeOffset effectiveFrom)
    {
        var found = FindVersion(definitionId, versionId); if (found is null) return ApplicationResult<KpiVersion>.Failure("RESOURCE_NOT_FOUND", "KPI Version was not found.", 404);
        if (!actor.Can(KpiCapability.ReviewKpi)) return ApplicationResult<KpiVersion>.Failure("AUTHORIZATION_DENIED", "Only the Policy Approver can publish a Version.", 403);
        try { found.Value.version.Publish(effectiveFrom); _store.AddAudit(AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_VERSION", versionId, AuditEventType.Published, _clock.UtcNow, actor.CorrelationId)); return ApplicationResult<KpiVersion>.Success(found.Value.version); } catch (KpiDomainException ex) { return ApplicationResult<KpiVersion>.Failure("LIFECYCLE_CONFLICT", ex.Message, 409); }
    }
    private (KpiDefinition definition, KpiVersion version)? FindVersion(Guid definitionId, Guid versionId) { var definition = _store.Find(definitionId); var version = definition?.Versions.FirstOrDefault(x => x.Id == versionId); return definition is null || version is null ? null : (definition, version); }
    public ApplicationResult<KpiDefinition> Archive(ActorContext actor, Guid definitionId)
    { var definition = _store.Find(definitionId); if (definition is null) return ApplicationResult<KpiDefinition>.Failure("RESOURCE_NOT_FOUND", "KPI was not found.", 404); if (!actor.Can(KpiCapability.Administrator) && definition.OwnerId != actor.ActorId) return ApplicationResult<KpiDefinition>.Failure("AUTHORIZATION_DENIED", "Actor cannot archive this KPI.", 403); definition.Archive(); _store.AddAudit(AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_DEFINITION", definition.Id, AuditEventType.Archived, _clock.UtcNow, actor.CorrelationId)); _persistence?.Save(definition); return ApplicationResult<KpiDefinition>.Success(definition); }
    public ApplicationResult<KpiDefinition> Restore(ActorContext actor, Guid definitionId)
    { var definition = _store.Find(definitionId); if (definition is null) return ApplicationResult<KpiDefinition>.Failure("RESOURCE_NOT_FOUND", "KPI was not found.", 404); if (!actor.Can(KpiCapability.Administrator)) return ApplicationResult<KpiDefinition>.Failure("AUTHORIZATION_DENIED", "Only an administrator can restore a KPI.", 403); definition.Restore(); _store.AddAudit(AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_DEFINITION", definition.Id, AuditEventType.Restored, _clock.UtcNow, actor.CorrelationId)); _persistence?.Save(definition); return ApplicationResult<KpiDefinition>.Success(definition); }
    public ApplicationResult<KpiDefinition> TransferOwnership(ActorContext actor, Guid definitionId, Guid newOwnerId, string reason)
    { var definition = _store.Find(definitionId); if (definition is null) return ApplicationResult<KpiDefinition>.Failure("RESOURCE_NOT_FOUND", "KPI was not found.", 404); if (!actor.Can(KpiCapability.ReviewKpi) || string.IsNullOrWhiteSpace(reason)) return ApplicationResult<KpiDefinition>.Failure("AUTHORIZATION_DENIED", "Policy Approver and a reason are required.", 403); definition.TransferOwnership(newOwnerId); _store.AddAudit(AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_DEFINITION", definition.Id, AuditEventType.DraftUpdated, _clock.UtcNow, actor.CorrelationId, reason: reason)); return ApplicationResult<KpiDefinition>.Success(definition); }
}
