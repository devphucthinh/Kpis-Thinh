using Kpi.Application.Common;
using Kpi.Domain.Auditing;
using Kpi.Domain.Kpis;
using Kpi.Domain.Periods;

namespace Kpi.Application;

/// <summary>Governed Period planning, amendment and activation operations.</summary>
public sealed class PeriodOperations(InMemoryKpiStore store, IClock clock, Persistence.IKpiGovernedPersistence? persistence = null)
{
    private readonly Persistence.IKpiGovernedPersistence? _persistence = persistence;
    public ConcurrencyToken ConcurrencyToken(KpiPeriod period) => new(period.Revision.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public ApplicationResult<KpiPeriod> Create(ActorContext actor, string code, DateTimeOffset starts, DateTimeOffset ends)
        => Create(actor, code, code, code, KpiCadence.Monthly, starts, ends);

    public ApplicationResult<KpiPeriod> Create(ActorContext actor, string code, string name, string description, KpiCadence cadence, DateTimeOffset starts, DateTimeOffset ends)
    {
        if (!actor.Can(KpiCapability.PlanPeriod)) return ApplicationResult<KpiPeriod>.Failure("AUTHORIZATION_DENIED", "Actor cannot plan a Period.", 403);
        try
        {
            var period = KpiPeriod.Create(actor.OrganizationId, code, name, description, cadence, starts, ends, actor.ActorId);
            var audit = AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_PERIOD", period.Id, AuditEventType.PeriodChanged, clock.UtcNow, actor.CorrelationId, summary: "Created");
            Save(period, audit);
            return ApplicationResult<KpiPeriod>.Success(store.AddPeriod(period, audit));
        }
        catch (Exception ex) when (ex is ArgumentException or KpiDomainException) { return ApplicationResult<KpiPeriod>.Failure("VALIDATION", ex.Message); }
    }

    public ApplicationResult<KpiPeriod> Select(ActorContext actor, Guid periodId, Guid definitionId, Guid versionId, ConcurrencyToken? token = null)
    {
        var period = FindOwned(actor, periodId, KpiCapability.PlanPeriod, "Only the Planner can edit this Period.");
        if (!period.IsSuccess) return period;
        var definition = store.Find(definitionId);
        var version = definition?.Versions.FirstOrDefault(x => x.Id == versionId);
        if (definition is null || definition.OrganizationId != actor.OrganizationId || definition.OrganizationId != period.Value!.OrganizationId || version is null)
            return ApplicationResult<KpiPeriod>.Failure("PERIOD_ELIGIBILITY_CONFLICT", "Definition and Version must belong to the same company.", 409);
        if (!IsEligible(period.Value, version)) return ApplicationResult<KpiPeriod>.Failure("PERIOD_ELIGIBILITY_CONFLICT", "Version is not eligible for this Period.", 409);
        if (!Matches(period.Value, token)) return ApplicationResult<KpiPeriod>.Failure("CONCURRENCY_CONFLICT", "The Period changed; reload before selecting a Version.", 409);
        try { period.Value.Select(definitionId, versionId); _persistence?.SavePeriod(period.Value); return period; }
        catch (KpiDomainException ex) { return ApplicationResult<KpiPeriod>.Failure("LIFECYCLE_CONFLICT", ex.Message, 409); }
    }

    /// <summary>Validates and applies a complete Draft selection set without partial mutation.</summary>
    public ApplicationResult<KpiPeriod> SelectMany(ActorContext actor, Guid periodId, IReadOnlyDictionary<Guid, Guid> selections, ConcurrencyToken? token = null)
    {
        var period = FindOwned(actor, periodId, KpiCapability.PlanPeriod, "Only the Planner can edit this Period.");
        if (!period.IsSuccess) return period;
        var planned = period.Value!;
        if (!Matches(planned, token)) return ApplicationResult<KpiPeriod>.Failure("CONCURRENCY_CONFLICT", "The Period changed; reload before selecting Versions.", 409);
        if (selections.Count == 0) return ApplicationResult<KpiPeriod>.Failure("PERIOD_SELECTION_REQUIRED", "Select at least one KPI Version.");

        foreach (var selection in selections)
        {
            var definition = store.Find(selection.Key);
            var version = definition?.Versions.FirstOrDefault(x => x.Id == selection.Value);
            if (definition is null || definition.OrganizationId != actor.OrganizationId || definition.OrganizationId != planned.OrganizationId || version is null || !IsEligible(planned, version))
                return ApplicationResult<KpiPeriod>.Failure("PERIOD_ELIGIBILITY_CONFLICT", "Every selected Version must be Published, same-cadence, same-company and effective for this Period.", 409);
        }

        try
        {
            foreach (var selection in selections) planned.Select(selection.Key, selection.Value);
            Save(planned, Audit(actor, planned, AuditEventType.PeriodChanged, summary: "Selections updated"));
            return ApplicationResult<KpiPeriod>.Success(planned);
        }
        catch (KpiDomainException ex) { return ApplicationResult<KpiPeriod>.Failure("LIFECYCLE_CONFLICT", ex.Message, 409); }
    }

    public ApplicationResult<KpiPeriod> Submit(ActorContext actor, Guid periodId, ConcurrencyToken? token = null)
    {
        var period = FindOwned(actor, periodId, KpiCapability.PlanPeriod, "Only the Planner can submit this Period.");
        if (!period.IsSuccess) return period;
        var planned = period.Value!;
        if (!Matches(planned, token)) return ApplicationResult<KpiPeriod>.Failure("CONCURRENCY_CONFLICT", "The Period changed; reload before submitting.", 409);
        if (store.Periods.Any(other => other.Id != planned.Id && other.OrganizationId == planned.OrganizationId && other.Cadence == planned.Cadence && (other.Status is KpiPeriodStatus.InReview or KpiPeriodStatus.Scheduled or KpiPeriodStatus.Active or KpiPeriodStatus.Closed) && Overlaps(planned.StartsAt, planned.EndsAt, other.StartsAt, other.EndsAt)))
            return ApplicationResult<KpiPeriod>.Failure("PERIOD_ELIGIBILITY_CONFLICT", "Same-cadence Periods cannot overlap.", 409);
        try { planned.Submit(); Save(planned, Audit(actor, planned, AuditEventType.Submitted)); return period; }
        catch (KpiDomainException ex) { return ApplicationResult<KpiPeriod>.Failure("LIFECYCLE_CONFLICT", ex.Message, 409); }
    }

    public ApplicationResult<KpiPeriod> Approve(ActorContext actor, Guid periodId)
    {
        var period = FindOwned(actor, periodId, KpiCapability.ApprovePeriod, "Actor cannot approve a Period.");
        if (!period.IsSuccess) return period;
        try { period.Value!.Approve(actor.ActorId); Save(period.Value, Audit(actor, period.Value, AuditEventType.Approved)); return period; }
        catch (KpiDomainException ex) { return ApplicationResult<KpiPeriod>.Failure("LIFECYCLE_CONFLICT", ex.Message, 409); }
    }

    public IReadOnlyList<KpiPeriod> List(Guid? organizationId = null)
    {
        if (organizationId is not null && _persistence is not null)
        {
            var loaded = _persistence.LoadPeriods(organizationId.Value);
            if (loaded.Count > 0) store.ReplacePeriods(loaded);
        }
        return organizationId is null ? store.Periods : store.Periods.Where(x => x.OrganizationId == organizationId.Value).ToArray();
    }

    public ApplicationResult<KpiPeriod> Reject(ActorContext actor, Guid periodId, string comment)
    {
        var period = FindOwned(actor, periodId, KpiCapability.ApprovePeriod, "Actor cannot reject a Period.");
        if (!period.IsSuccess) return period;
        try { period.Value!.Reject(comment); Save(period.Value, Audit(actor, period.Value, AuditEventType.Rejected, comment)); return period; }
        catch (KpiDomainException ex) { return ApplicationResult<KpiPeriod>.Failure("LIFECYCLE_CONFLICT", ex.Message, 409); }
    }

    public ApplicationResult<KpiPeriod> ReturnToDraft(ActorContext actor, Guid periodId)
    {
        var period = FindOwned(actor, periodId, KpiCapability.PlanPeriod, "Actor cannot edit a Period.");
        if (!period.IsSuccess) return period;
        try { period.Value!.ReturnToDraft(actor.ActorId); Save(period.Value, Audit(actor, period.Value, AuditEventType.PeriodChanged, summary: "Rejected to Draft")); return period; }
        catch (KpiDomainException ex) { return ApplicationResult<KpiPeriod>.Failure("LIFECYCLE_CONFLICT", ex.Message, 409); }
    }

    public ApplicationResult<KpiPeriodAmendment> ProposeAmendment(ActorContext actor, Guid periodId, IReadOnlyDictionary<Guid, Guid> selections, string reason, DateTimeOffset? startsAt = null, DateTimeOffset? endsAt = null, ConcurrencyToken? token = null)
    {
        var period = FindOwned(actor, periodId, KpiCapability.PlanPeriod, "Only the Planner can amend a Period.");
        if (!period.IsSuccess) return ApplicationResult<KpiPeriodAmendment>.Failure(period.Error!.Code, period.Error.Message, period.Error.Status);
        var planned = period.Value!;
        if (!Matches(planned, token)) return ApplicationResult<KpiPeriodAmendment>.Failure("CONCURRENCY_CONFLICT", "The Period changed; reload before proposing an amendment.", 409);
        var proposedStarts = startsAt ?? planned.StartsAt;
        var proposedEnds = endsAt ?? planned.EndsAt;
        foreach (var selection in selections)
        {
            var definition = store.Find(selection.Key);
            var version = definition?.Versions.FirstOrDefault(x => x.Id == selection.Value);
            if (definition is null || definition.OrganizationId != actor.OrganizationId || version is null || !IsEligible(planned, version, proposedStarts, proposedEnds))
                return ApplicationResult<KpiPeriodAmendment>.Failure("PERIOD_ELIGIBILITY_CONFLICT", "Amendment contains an ineligible Version.", 409);
        }
        try
        {
            var amendment = planned.ProposeAmendment(actor.ActorId, selections, proposedStarts, proposedEnds, reason);
            Save(planned, Audit(actor, planned, AuditEventType.PeriodChanged, reason, "Amendment proposed"));
            return ApplicationResult<KpiPeriodAmendment>.Success(amendment);
        }
        catch (KpiDomainException ex) { return ApplicationResult<KpiPeriodAmendment>.Failure("LIFECYCLE_CONFLICT", ex.Message, 409); }
    }

    public ApplicationResult<KpiPeriod> ReviewAmendment(ActorContext actor, Guid periodId, Guid amendmentId, bool approve, string comment)
    {
        var period = FindOwned(actor, periodId, KpiCapability.ApprovePeriod, "Actor cannot review a Period Amendment.");
        if (!period.IsSuccess) return period;
        try { period.Value!.ReviewAmendment(actor.ActorId, amendmentId, approve, comment); Save(period.Value, Audit(actor, period.Value, approve ? AuditEventType.Approved : AuditEventType.Rejected, comment, "Amendment reviewed")); return period; }
        catch (KpiDomainException ex) { return ApplicationResult<KpiPeriod>.Failure("LIFECYCLE_CONFLICT", ex.Message, 409); }
    }

    /// <summary>Legacy command retained for callers; it now creates an InReview amendment and never self-approves it.</summary>
    public ApplicationResult<KpiPeriod> Amend(ActorContext actor, Guid periodId, Guid approverId, IReadOnlyDictionary<Guid, Guid> selections, string reason)
    {
        var proposed = ProposeAmendment(actor, periodId, selections, reason);
        return proposed.IsSuccess ? ApplicationResult<KpiPeriod>.Success(store.FindPeriod(periodId)!) : ApplicationResult<KpiPeriod>.Failure(proposed.Error!.Code, proposed.Error.Message, proposed.Error.Status);
    }

    public ApplicationResult<KpiPeriod> Activate(ActorContext actor, Guid periodId)
    {
        var period = FindOwned(actor, periodId, KpiCapability.None, "Actor cannot activate this Period.");
        if (!period.IsSuccess) return period;
        try { period.Value!.Activate(clock.UtcNow); Save(period.Value, Audit(actor, period.Value, AuditEventType.PeriodChanged, summary: "Activated")); return period; }
        catch (KpiDomainException ex) { return ApplicationResult<KpiPeriod>.Failure("LIFECYCLE_CONFLICT", ex.Message, 409); }
    }

    public ApplicationResult<KpiPeriod> Close(ActorContext actor, Guid periodId)
    {
        var period = FindOwned(actor, periodId, KpiCapability.None, "Actor cannot close this Period.");
        if (!period.IsSuccess) return period;
        try { period.Value!.Close(clock.UtcNow); Save(period.Value, Audit(actor, period.Value, AuditEventType.PeriodChanged, summary: "Closed")); return period; }
        catch (KpiDomainException ex) { return ApplicationResult<KpiPeriod>.Failure("LIFECYCLE_CONFLICT", ex.Message, 409); }
    }

    private ApplicationResult<KpiPeriod> FindOwned(ActorContext actor, Guid periodId, KpiCapability capability, string message)
    {
        var period = store.FindPeriod(periodId);
        if (period is null) return ApplicationResult<KpiPeriod>.Failure("RESOURCE_NOT_FOUND", "Period was not found.", 404);
        if (period.OrganizationId != actor.OrganizationId) return ApplicationResult<KpiPeriod>.Failure("ORGANIZATION_SCOPE_CONFLICT", "Period belongs to another company.", 403);
        if (capability != KpiCapability.None && !actor.Can(capability)) return ApplicationResult<KpiPeriod>.Failure("AUTHORIZATION_DENIED", message, 403);
        if (capability == KpiCapability.PlanPeriod && period.PlannerId != actor.ActorId) return ApplicationResult<KpiPeriod>.Failure("AUTHORIZATION_DENIED", message, 403);
        return ApplicationResult<KpiPeriod>.Success(period);
    }

    private static bool IsEligible(KpiPeriod period, KpiVersion version, DateTimeOffset? starts = null, DateTimeOffset? ends = null) => version.Status == KpiVersionStatus.Published && version.Cadence == period.Cadence && version.EffectiveFrom is not null && version.EffectiveFrom <= (starts ?? period.StartsAt) && (version.EffectiveTo is null || version.EffectiveTo >= (ends ?? period.EndsAt));
    private static bool Matches(KpiPeriod period, ConcurrencyToken? token) => token is null || string.Equals(period.Revision.ToString(System.Globalization.CultureInfo.InvariantCulture), token.Value.Value, StringComparison.Ordinal);
    private AuditRecord Audit(ActorContext actor, KpiPeriod period, AuditEventType type, string? reason = null, string? summary = null) => AuditRecord.Create(actor.OrganizationId, actor.ActorId, "KPI_PERIOD", period.Id, type, clock.UtcNow, actor.CorrelationId, reason: reason, summary: summary);
    private void Save(KpiPeriod period, AuditRecord audit)
    {
        if (_persistence is null) { store.AddAudit(audit); return; }
        _persistence.ExecuteInTransaction(() => { _persistence.SavePeriod(period); _persistence.SaveAudit(audit); });
        store.AddAudit(audit);
    }
    private static bool Overlaps(DateTimeOffset start, DateTimeOffset end, DateTimeOffset otherStart, DateTimeOffset otherEnd) => start < otherEnd && otherStart < end;
}
