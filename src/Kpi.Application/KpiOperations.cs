using Kpi.Application.Common;
using Kpi.Application.Authorization;
using Kpi.Domain.Auditing;
using Kpi.Domain.Formula;
using Kpi.Domain.Kpis;
using Kpi.Application.Persistence;
using System.Globalization;

namespace Kpi.Application;

/// <summary>Governed Definition/Version operations used by HTTP and MVC.</summary>
public sealed class KpiOperations
{
    private readonly InMemoryKpiStore _store;
    private readonly IClock _clock;
    private readonly IKpiDefinitionPersistence? _persistence;
    private readonly IKpiGovernedPersistence? _governedPersistence;
    private readonly IAuthorizationDecision? _authorization;
    public KpiOperations(InMemoryKpiStore store, IClock clock, IKpiDefinitionPersistence? persistence = null, IKpiGovernedPersistence? governedPersistence = null, IAuthorizationDecision? authorization = null) { _store = store; _clock = clock; _persistence = persistence; _governedPersistence = governedPersistence; _authorization = authorization; }

    public ApplicationResult<KpiDefinition> CreateDefinition(ActorContext actor, string code, string name, string description)
    {
        if (!Authorize(actor, KpiCapability.CreateKpi, new AuthorizationResource(actor.OrganizationId, "Organization", actor.OrganizationId, 0))) return ApplicationResult<KpiDefinition>.Failure("AUTHORIZATION_DENIED", "Actor cannot create KPI.", 403);
        RefreshFromPersistence(actor.OrganizationId);
        if (_store.FindByCode(code, actor.OrganizationId) is not null) return ApplicationResult<KpiDefinition>.Failure("KPI_CODE_CONFLICT", "KPI code already exists.", 409);
        var definition = KpiDefinition.Create(actor.OrganizationId, code, name, description, actor.ActorId);
        var audit = AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_DEFINITION", definition.Id, AuditEventType.Created, _clock.UtcNow, actor.CorrelationId, summary: definition.Code.Value);
        CommitDefinition(definition, audit, addAuditToStore: false);
        _store.AddDefinition(definition, audit);
        return ApplicationResult<KpiDefinition>.Success(definition);
    }

    public ApplicationResult<KpiVersion> CreateVersion(ActorContext actor, Guid definitionId, string name, string description, string source, IReadOnlyList<FormulaVariableDefinition> variables, FormulaResultType resultType, string changeSummary)
        => CreateVersion(actor, definitionId, name, description, source, variables, resultType, changeSummary, Kpi.Domain.Periods.KpiCadence.Monthly);

    public ApplicationResult<KpiVersion> CreateVersion(ActorContext actor, Guid definitionId, string name, string description, string source, IReadOnlyList<FormulaVariableDefinition> variables, FormulaResultType resultType, string changeSummary, Kpi.Domain.Periods.KpiCadence cadence)
    {
        RefreshFromPersistence(actor.OrganizationId);
        var definition = _store.Find(definitionId); if (definition is null) return ApplicationResult<KpiVersion>.Failure("RESOURCE_NOT_FOUND", "KPI was not found.", 404);
        if (definition.OrganizationId != actor.OrganizationId) return ApplicationResult<KpiVersion>.Failure("ORGANIZATION_SCOPE_CONFLICT", "KPI belongs to another company.", 403);
        if (!Authorize(actor, KpiCapability.EditDraft, ResourceForDefinition(definition))) return ApplicationResult<KpiVersion>.Failure("AUTHORIZATION_DENIED", "Actor cannot edit KPI drafts.", 403);
        if (definition.OwnerId != actor.ActorId) return ApplicationResult<KpiVersion>.Failure("AUTHORIZATION_DENIED", "Only the owner can edit this draft.", 403);
        try { var version = definition.CreateVersion(name, description, source, variables, resultType, changeSummary, cadence); CommitDefinition(definition, AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_VERSION", version.Id, AuditEventType.Created, _clock.UtcNow, actor.CorrelationId)); return ApplicationResult<KpiVersion>.Success(version); }
        catch (Exception ex) when (ex is ArgumentException or KpiDomainException) { return ApplicationResult<KpiVersion>.Failure("VALIDATION", ex.Message); }
    }

    public ConcurrencyToken DefinitionConcurrencyToken(KpiDefinition definition) => new(definition.Revision.ToString(CultureInfo.InvariantCulture));

    public ApplicationResult<KpiDefinition> UpdateDefinition(ActorContext actor, Guid definitionId, string name, string description, ConcurrencyToken token)
    {
        RefreshFromPersistence(actor.OrganizationId);
        var definition = _store.Find(definitionId);
        if (definition is null) return ApplicationResult<KpiDefinition>.Failure("RESOURCE_NOT_FOUND", "KPI was not found.", 404);
        if (definition.OrganizationId != actor.OrganizationId) return ApplicationResult<KpiDefinition>.Failure("ORGANIZATION_SCOPE_CONFLICT", "KPI belongs to another company.", 403);
        if (!Authorize(actor, KpiCapability.EditDraft, ResourceForDefinition(definition)) || definition.OwnerId != actor.ActorId) return ApplicationResult<KpiDefinition>.Failure("AUTHORIZATION_DENIED", "Only the owner can edit KPI metadata.", 403);
        if (!string.Equals(token.Value, definition.Revision.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)) return ApplicationResult<KpiDefinition>.Failure("CONCURRENCY_CONFLICT", "The KPI changed; reload before editing.", 409);
        try { definition.UpdateMetadata(name, description); CommitDefinition(definition, AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_DEFINITION", definition.Id, AuditEventType.DraftUpdated, _clock.UtcNow, actor.CorrelationId)); return ApplicationResult<KpiDefinition>.Success(definition); }
        catch (Exception ex) when (ex is ArgumentException or KpiDomainException) { return ApplicationResult<KpiDefinition>.Failure("VALIDATION", ex.Message); }
    }

    public ApplicationResult<FormulaCompilation> Validate(ActorContext actor, string source, IReadOnlyList<FormulaVariableDefinition> variables, FormulaResultType resultType) => ApplicationResult<FormulaCompilation>.Success(FormulaEngine.Compile(source, variables, resultType));
    public IReadOnlyList<KpiDefinition> List(Guid? organizationId = null)
    {
        RefreshFromPersistence(organizationId);
        return organizationId is null ? _store.Definitions : _store.Definitions.Where(x => x.OrganizationId == organizationId.Value).ToArray();
    }
    public IReadOnlyList<AuditRecord> Audit(Guid? organizationId = null, string? entityType = null, Guid? entityId = null, DateTimeOffset? from = null, DateTimeOffset? to = null, Guid? actorId = null, AuditEventType? eventType = null)
    {
        if (organizationId is not null && _governedPersistence is not null)
        {
            var persisted = _governedPersistence.LoadAudit(new Persistence.AuditQuery(organizationId.Value, entityType, entityId, actorId, eventType, from, to));
            if (persisted.Count > 0) return persisted;
        }
        return _store.Audit.Where(x => (organizationId is null || x.OrganizationId == organizationId.Value) && (entityType is null || string.Equals(x.EntityType, entityType, StringComparison.OrdinalIgnoreCase)) && (entityId is null || x.EntityId == entityId.Value) && (actorId is null || x.ActorId == actorId.Value) && (eventType is null || x.EventType == eventType.Value) && (from is null || x.OccurredAt >= from.Value) && (to is null || x.OccurredAt <= to.Value)).ToArray();
    }
    public ApplicationResult<KpiVersion> SubmitVersion(ActorContext actor, Guid definitionId, Guid versionId)
    {
        RefreshFromPersistence(actor.OrganizationId);
        var found = FindVersion(definitionId, versionId); if (found is null) return ApplicationResult<KpiVersion>.Failure("RESOURCE_NOT_FOUND", "KPI Version was not found.", 404);
        if (found.Value.definition.OrganizationId != actor.OrganizationId) return ApplicationResult<KpiVersion>.Failure("ORGANIZATION_SCOPE_CONFLICT", "KPI belongs to another company.", 403);
        if (!AuthorizeCapability(actor, KpiAuthorizationCapabilities.VersionSubmit, ResourceForVersion(found.Value.definition, found.Value.version)) || found.Value.definition.OwnerId != actor.ActorId) return ApplicationResult<KpiVersion>.Failure("AUTHORIZATION_DENIED", "Only the Creator can submit this Version.", 403);
        try { found.Value.version.Submit(); CommitDefinition(found.Value.definition, AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_VERSION", versionId, AuditEventType.Submitted, _clock.UtcNow, actor.CorrelationId)); return ApplicationResult<KpiVersion>.Success(found.Value.version); } catch (KpiDomainException ex) { return ApplicationResult<KpiVersion>.Failure("LIFECYCLE_CONFLICT", ex.Message, 409); }
    }
    public ApplicationResult<KpiVersion> ReviewVersion(ActorContext actor, Guid definitionId, Guid versionId, bool approve, string comment)
    {
        RefreshFromPersistence(actor.OrganizationId);
        var found = FindVersion(definitionId, versionId); if (found is null) return ApplicationResult<KpiVersion>.Failure("RESOURCE_NOT_FOUND", "KPI Version was not found.", 404);
        if (found.Value.definition.OrganizationId != actor.OrganizationId) return ApplicationResult<KpiVersion>.Failure("ORGANIZATION_SCOPE_CONFLICT", "KPI belongs to another company.", 403);
        if (!Authorize(actor, KpiCapability.ReviewKpi, ResourceForVersion(found.Value.definition, found.Value.version)) || actor.ActorId == found.Value.definition.OwnerId) return ApplicationResult<KpiVersion>.Failure("SELF_APPROVAL_FORBIDDEN", "A Creator cannot review their own Version.", 403);
        try { if (approve) found.Value.version.Approve(comment); else found.Value.version.Reject(comment); CommitDefinition(found.Value.definition, AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_VERSION", versionId, approve ? AuditEventType.Approved : AuditEventType.Rejected, _clock.UtcNow, actor.CorrelationId, reason: comment)); return ApplicationResult<KpiVersion>.Success(found.Value.version); } catch (KpiDomainException ex) { return ApplicationResult<KpiVersion>.Failure("LIFECYCLE_CONFLICT", ex.Message, 409); }
    }
    public ApplicationResult<KpiVersion> PublishVersion(ActorContext actor, Guid definitionId, Guid versionId, DateTimeOffset effectiveFrom)
    {
        RefreshFromPersistence(actor.OrganizationId);
        var found = FindVersion(definitionId, versionId); if (found is null) return ApplicationResult<KpiVersion>.Failure("RESOURCE_NOT_FOUND", "KPI Version was not found.", 404);
        if (found.Value.definition.OrganizationId != actor.OrganizationId) return ApplicationResult<KpiVersion>.Failure("ORGANIZATION_SCOPE_CONFLICT", "KPI belongs to another company.", 403);
        if (!Authorize(actor, KpiCapability.ReviewKpi, ResourceForVersion(found.Value.definition, found.Value.version))) return ApplicationResult<KpiVersion>.Failure("AUTHORIZATION_DENIED", "Only the Policy Approver can publish a Version.", 403);
        try
        {
            var definition = found.Value.definition;
            var existing = definition.Versions.Where(x => x.Id != versionId && x.EffectiveFrom is not null && (x.Status is KpiVersionStatus.Published or KpiVersionStatus.Retired)).OrderBy(x => x.EffectiveFrom).ToArray();
            var previous = existing.LastOrDefault(x => x.EffectiveFrom <= effectiveFrom && (x.EffectiveTo is null || effectiveFrom < x.EffectiveTo));
            if (previous is not null)
            {
                if (previous.EffectiveFrom == effectiveFrom) return ApplicationResult<KpiVersion>.Failure("EFFECTIVE_RANGE_CONFLICT", "Another Version already starts at this effective time.", 409);
                if (previous.Status == KpiVersionStatus.Retired || previous.EffectiveTo is not null && effectiveFrom >= previous.EffectiveTo) return ApplicationResult<KpiVersion>.Failure("EFFECTIVE_RANGE_CONFLICT", "The effective range overlaps an existing Version.", 409);
                previous.SetEffectiveTo(effectiveFrom);
            }
            var next = existing.FirstOrDefault(x => x.EffectiveFrom > effectiveFrom);
            found.Value.version.Publish(effectiveFrom);
            if (next?.EffectiveFrom is not null) found.Value.version.SetEffectiveTo(next.EffectiveFrom.Value);
            CommitDefinition(definition, AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_VERSION", versionId, AuditEventType.Published, _clock.UtcNow, actor.CorrelationId));
            return ApplicationResult<KpiVersion>.Success(found.Value.version);
        }
        catch (KpiDomainException ex) { return ApplicationResult<KpiVersion>.Failure("LIFECYCLE_CONFLICT", ex.Message, 409); }
    }
    private (KpiDefinition definition, KpiVersion version)? FindVersion(Guid definitionId, Guid versionId) { var definition = _store.Find(definitionId); var version = definition?.Versions.FirstOrDefault(x => x.Id == versionId); return definition is null || version is null ? null : (definition, version); }

    private void RefreshFromPersistence(Guid? organizationId = null)
    {
        if (_persistence is null) return;
        var definitions = _persistence.LoadAll(organizationId);
        if (definitions.Count > 0 || organizationId is null) _store.ReplaceDefinitions(definitions);
    }
    public ApplicationResult<KpiDefinition> Archive(ActorContext actor, Guid definitionId)
    {
        RefreshFromPersistence(actor.OrganizationId);
        var definition = _store.Find(definitionId);
        if (definition is null) return ApplicationResult<KpiDefinition>.Failure("RESOURCE_NOT_FOUND", "KPI was not found.", 404);
        if (definition.OrganizationId != actor.OrganizationId) return ApplicationResult<KpiDefinition>.Failure("ORGANIZATION_SCOPE_CONFLICT", "KPI belongs to another company.", 403);
        var resource = ResourceForDefinition(definition);
        var administrator = Authorize(actor, KpiCapability.Administrator, resource);
        var ownerEditor = !administrator && definition.OwnerId == actor.ActorId && Authorize(actor, KpiCapability.EditDraft, resource);
        if (!administrator && !ownerEditor) return ApplicationResult<KpiDefinition>.Failure("AUTHORIZATION_DENIED", "Actor cannot archive this KPI.", 403);
        definition.Archive();
        CommitDefinition(definition, AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_DEFINITION", definition.Id, AuditEventType.Archived, _clock.UtcNow, actor.CorrelationId));
        return ApplicationResult<KpiDefinition>.Success(definition);
    }
    public ApplicationResult<KpiDefinition> Restore(ActorContext actor, Guid definitionId)
    { RefreshFromPersistence(actor.OrganizationId); var definition = _store.Find(definitionId); if (definition is null) return ApplicationResult<KpiDefinition>.Failure("RESOURCE_NOT_FOUND", "KPI was not found.", 404); if (definition.OrganizationId != actor.OrganizationId) return ApplicationResult<KpiDefinition>.Failure("ORGANIZATION_SCOPE_CONFLICT", "KPI belongs to another company.", 403); if (!Authorize(actor, KpiCapability.Administrator, ResourceForDefinition(definition))) return ApplicationResult<KpiDefinition>.Failure("AUTHORIZATION_DENIED", "Only an administrator can restore a KPI.", 403); definition.Restore(); CommitDefinition(definition, AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_DEFINITION", definition.Id, AuditEventType.Restored, _clock.UtcNow, actor.CorrelationId)); return ApplicationResult<KpiDefinition>.Success(definition); }
    public ApplicationResult<KpiDefinition> TransferOwnership(ActorContext actor, Guid definitionId, Guid newOwnerId, string reason)
    { RefreshFromPersistence(actor.OrganizationId); var definition = _store.Find(definitionId); if (definition is null) return ApplicationResult<KpiDefinition>.Failure("RESOURCE_NOT_FOUND", "KPI was not found.", 404); if (definition.OrganizationId != actor.OrganizationId) return ApplicationResult<KpiDefinition>.Failure("ORGANIZATION_SCOPE_CONFLICT", "KPI belongs to another company.", 403); if (!Authorize(actor, KpiCapability.ReviewKpi, ResourceForDefinition(definition)) || string.IsNullOrWhiteSpace(reason)) return ApplicationResult<KpiDefinition>.Failure("AUTHORIZATION_DENIED", "Policy Approver and a reason are required.", 403); definition.TransferOwnership(newOwnerId); CommitDefinition(definition, AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_DEFINITION", definition.Id, AuditEventType.DraftUpdated, _clock.UtcNow, actor.CorrelationId, reason: reason)); return ApplicationResult<KpiDefinition>.Success(definition); }

    private void CommitDefinition(KpiDefinition definition, AuditRecord audit, bool addAuditToStore = true)
    {
        if (_persistence is null)
        {
            _governedPersistence?.SaveAudit(audit);
            if (addAuditToStore) _store.AddAudit(audit);
            return;
        }

        if (_governedPersistence is null)
        {
            _persistence.Save(definition);
            if (addAuditToStore) _store.AddAudit(audit);
            return;
        }

        _governedPersistence.ExecuteInTransaction(() =>
        {
            _persistence.Save(definition);
            _governedPersistence.SaveAudit(audit);
        });
        if (addAuditToStore) _store.AddAudit(audit);
    }

    public ConcurrencyToken ConcurrencyToken(KpiVersion version) => new(version.Revision.ToString(CultureInfo.InvariantCulture));

    public ApplicationResult<KpiVersion> UpdateDraft(ActorContext actor, Guid definitionId, Guid versionId, string name, string description, string source, IReadOnlyList<FormulaVariableDefinition> variables, ConcurrencyToken token)
    {
        var found = FindVersion(definitionId, versionId);
        if (found is null) return ApplicationResult<KpiVersion>.Failure("RESOURCE_NOT_FOUND", "KPI Version was not found.", 404);
        if (found.Value.definition.OrganizationId != actor.OrganizationId) return ApplicationResult<KpiVersion>.Failure("ORGANIZATION_SCOPE_CONFLICT", "KPI belongs to another company.", 403);
        if (!Authorize(actor, KpiCapability.EditDraft, ResourceForVersion(found.Value.definition, found.Value.version)) || found.Value.definition.OwnerId != actor.ActorId) return ApplicationResult<KpiVersion>.Failure("AUTHORIZATION_DENIED", "Only the owner can edit this draft.", 403);
        if (!Matches(found.Value.version, token)) return ApplicationResult<KpiVersion>.Failure("CONCURRENCY_CONFLICT", "The KPI Version changed; reload before editing.", 409);
        try { found.Value.version.UpdateDraft(name, description, source, variables); CommitDefinition(found.Value.definition, AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_VERSION", versionId, AuditEventType.DraftUpdated, _clock.UtcNow, actor.CorrelationId)); return ApplicationResult<KpiVersion>.Success(found.Value.version); }
        catch (Exception ex) when (ex is ArgumentException or KpiDomainException) { return ApplicationResult<KpiVersion>.Failure("VALIDATION", ex.Message); }
    }

    public ApplicationResult<KpiVersion> ReturnVersionToDraft(ActorContext actor, Guid definitionId, Guid versionId)
    {
        var found = FindVersion(definitionId, versionId);
        if (found is null) return ApplicationResult<KpiVersion>.Failure("RESOURCE_NOT_FOUND", "KPI Version was not found.", 404);
        if (found.Value.definition.OrganizationId != actor.OrganizationId) return ApplicationResult<KpiVersion>.Failure("ORGANIZATION_SCOPE_CONFLICT", "KPI belongs to another company.", 403);
        if (!Authorize(actor, KpiCapability.EditDraft, ResourceForVersion(found.Value.definition, found.Value.version)) || found.Value.definition.OwnerId != actor.ActorId) return ApplicationResult<KpiVersion>.Failure("AUTHORIZATION_DENIED", "Only the owner can return a rejected Version to Draft.", 403);
        try { found.Value.version.ReturnToDraft(); CommitDefinition(found.Value.definition, AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_VERSION", versionId, AuditEventType.DraftUpdated, _clock.UtcNow, actor.CorrelationId, summary: "Returned to Draft")); return ApplicationResult<KpiVersion>.Success(found.Value.version); }
        catch (KpiDomainException ex) { return ApplicationResult<KpiVersion>.Failure("LIFECYCLE_CONFLICT", ex.Message, 409); }
    }

    public ApplicationResult<KpiVersion> CloneVersion(ActorContext actor, Guid definitionId, Guid versionId, string changeSummary)
    {
        var found = FindVersion(definitionId, versionId);
        if (found is null) return ApplicationResult<KpiVersion>.Failure("RESOURCE_NOT_FOUND", "KPI Version was not found.", 404);
        if (found.Value.definition.OrganizationId != actor.OrganizationId) return ApplicationResult<KpiVersion>.Failure("ORGANIZATION_SCOPE_CONFLICT", "KPI belongs to another company.", 403);
        if (!Authorize(actor, KpiCapability.EditDraft, ResourceForVersion(found.Value.definition, found.Value.version)) || found.Value.definition.OwnerId != actor.ActorId) return ApplicationResult<KpiVersion>.Failure("AUTHORIZATION_DENIED", "Only the owner can clone a Version.", 403);
        try { var clone = found.Value.definition.CloneVersion(found.Value.version, changeSummary); CommitDefinition(found.Value.definition, AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_VERSION", clone.Id, AuditEventType.Created, _clock.UtcNow, actor.CorrelationId, summary: $"Cloned from {versionId}")); return ApplicationResult<KpiVersion>.Success(clone); }
        catch (Exception ex) when (ex is ArgumentException or KpiDomainException) { return ApplicationResult<KpiVersion>.Failure("VALIDATION", ex.Message); }
    }

    public ApplicationResult<KpiDefinition> DeleteDraft(ActorContext actor, Guid definitionId, Guid versionId, ConcurrencyToken token)
    {
        var found = FindVersion(definitionId, versionId);
        if (found is null) return ApplicationResult<KpiDefinition>.Failure("RESOURCE_NOT_FOUND", "KPI Version was not found.", 404);
        if (found.Value.definition.OrganizationId != actor.OrganizationId) return ApplicationResult<KpiDefinition>.Failure("ORGANIZATION_SCOPE_CONFLICT", "KPI belongs to another company.", 403);
        if (!Authorize(actor, KpiCapability.EditDraft, ResourceForVersion(found.Value.definition, found.Value.version)) || found.Value.definition.OwnerId != actor.ActorId) return ApplicationResult<KpiDefinition>.Failure("AUTHORIZATION_DENIED", "Only the owner can delete a Draft Version.", 403);
        if (!Matches(found.Value.version, token)) return ApplicationResult<KpiDefinition>.Failure("CONCURRENCY_CONFLICT", "The KPI Version changed; reload before deleting.", 409);
        try { found.Value.definition.DeleteEligibleDraft(found.Value.version); CommitDefinition(found.Value.definition, AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_VERSION", versionId, AuditEventType.Deleted, _clock.UtcNow, actor.CorrelationId, summary: "Draft deletion tombstone")); return ApplicationResult<KpiDefinition>.Success(found.Value.definition); }
        catch (KpiDomainException ex) { return ApplicationResult<KpiDefinition>.Failure("LIFECYCLE_CONFLICT", ex.Message, 409); }
    }

    private static bool Matches(KpiVersion version, ConcurrencyToken token) => string.Equals(version.Revision.ToString(CultureInfo.InvariantCulture), token.Value, StringComparison.Ordinal);

    private bool Authorize(ActorContext actor, KpiCapability capability, AuthorizationResource resource)
    {
        if (_authorization is null) return LegacyKpiAuthorizationAdapter.Can(actor, capability);
        var capabilityId = capability switch
        {
            KpiCapability.CreateKpi => KpiAuthorizationCapabilities.DefinitionCreate,
            KpiCapability.EditDraft => KpiAuthorizationCapabilities.DefinitionEdit,
            KpiCapability.ReviewKpi => KpiAuthorizationCapabilities.VersionReview,
            KpiCapability.Administrator => KpiAuthorizationCapabilities.DefinitionAdmin,
            _ => throw new ArgumentOutOfRangeException(nameof(capability), capability, "No authorization mapping exists for this KPI capability.")
        };
        return AuthorizeCapability(actor, capabilityId, resource);
    }

    private bool AuthorizeCapability(ActorContext actor, KpiCapabilityId capability, AuthorizationResource resource)
    {
        if (_authorization is null)
        {
            var legacyCapability = capability.Value switch
            {
                "kpi.definition.create" or "kpi.version.submit" => KpiCapability.CreateKpi,
                "kpi.definition.edit" => KpiCapability.EditDraft,
                "kpi.definition.admin" => KpiCapability.Administrator,
                "kpi.version.review" or "kpi.version.activate" => KpiCapability.ReviewKpi,
                _ => KpiCapability.None
            };
            return legacyCapability != KpiCapability.None && LegacyKpiAuthorizationAdapter.Can(actor, legacyCapability);
        }
        var identity = new ActorIdentity(actor.ActorId.ToString("D"), actor.ActorId, actor.OrganizationId);
        var decision = new AuthorizationActionContext(_authorization)
            .DecideAsync(identity, capability, resource, _clock.UtcNow, null, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        return decision.Outcome == AuthorizationOutcome.Allow;
    }

    private static AuthorizationResource ResourceForDefinition(KpiDefinition definition) =>
        new(definition.OrganizationId, "KpiDefinition", definition.Id, definition.Revision, OwnerId: definition.OwnerId);

    private static AuthorizationResource ResourceForVersion(KpiDefinition definition, KpiVersion version) =>
        new(definition.OrganizationId, "KpiVersion", version.Id, version.Revision, OwnerId: definition.OwnerId);
}
