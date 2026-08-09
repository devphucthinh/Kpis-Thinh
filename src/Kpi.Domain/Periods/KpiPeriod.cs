using Kpi.Domain.Kpis;

namespace Kpi.Domain.Periods;

/// <summary>Cadenced plan that freezes exact KPI Version selections at approval.</summary>
public sealed class KpiPeriod
{
    private KpiPeriod(Guid id, Guid organizationId, string code, DateTimeOffset starts, DateTimeOffset ends, Guid plannerId)
    { Id = id; OrganizationId = organizationId; Code = code; StartsAt = starts; EndsAt = ends; PlannerId = plannerId; }
    public Guid Id { get; }
    public Guid OrganizationId { get; }
    public string Code { get; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public Guid PlannerId { get; }
    public Guid? ApproverId { get; private set; }
    public KpiPeriodStatus Status { get; private set; } = KpiPeriodStatus.Draft;
    public Dictionary<Guid, Guid> SelectedVersions { get; } = [];
    public int LatestEffectiveRevision { get; private set; }
    public List<KpiPeriodEffectiveRevision> EffectiveRevisions { get; } = [];

    public static KpiPeriod Create(Guid organizationId, string code, DateTimeOffset starts, DateTimeOffset ends, Guid plannerId)
    { if (ends <= starts) throw new KpiDomainException("Period end must be after start."); return new(Guid.NewGuid(), organizationId, code.Trim(), starts, ends, plannerId); }
    public void Select(Guid definitionId, Guid versionId) { if (Status != KpiPeriodStatus.Draft) throw new KpiDomainException("Only Draft Periods can change selections."); SelectedVersions[definitionId] = versionId; }
    public void Submit() { if (Status != KpiPeriodStatus.Draft || SelectedVersions.Count == 0) throw new KpiDomainException("Period requires selections and Draft status."); Status = KpiPeriodStatus.InReview; }
    public void Approve(Guid approverId) { if (Status != KpiPeriodStatus.InReview || approverId == PlannerId) throw new KpiDomainException("Period approval requires a distinct approver."); ApproverId = approverId; Status = KpiPeriodStatus.Scheduled; EffectiveRevisions.Add(new(0, SelectedVersions.ToDictionary(x => x.Key, x => x.Value), "Original approved plan")); }
    public void Reject(string comment) { if (Status != KpiPeriodStatus.InReview || string.IsNullOrWhiteSpace(comment)) throw new KpiDomainException("Period rejection requires a comment."); Status = KpiPeriodStatus.Rejected; }
    public void ReturnToDraft(Guid plannerId) { if (Status != KpiPeriodStatus.Rejected || plannerId != PlannerId) throw new KpiDomainException("Only the Planner can reopen a rejected Period."); Status = KpiPeriodStatus.Draft; }
    public void Amend(Guid proposerId, Guid approverId, IReadOnlyDictionary<Guid, Guid> selections, string reason) { if (Status != KpiPeriodStatus.Scheduled || proposerId != PlannerId || approverId == proposerId || string.IsNullOrWhiteSpace(reason)) throw new KpiDomainException("Only a separately reviewed Amendment can change a Scheduled Period."); var revision = ++LatestEffectiveRevision; EffectiveRevisions.Add(new(revision, selections.ToDictionary(x => x.Key, x => x.Value), reason.Trim())); }
    public void Activate(DateTimeOffset now) { if (Status != KpiPeriodStatus.Scheduled || now < StartsAt) throw new KpiDomainException("Period is not due for activation."); Status = KpiPeriodStatus.Active; }
    public void Close(DateTimeOffset now) { if (Status != KpiPeriodStatus.Active || now < EndsAt) throw new KpiDomainException("Period is not due for closure."); Status = KpiPeriodStatus.Closed; }
    public void Cancel() { if (Status is KpiPeriodStatus.Active or KpiPeriodStatus.Closed or KpiPeriodStatus.Cancelled) throw new KpiDomainException("Period cannot be cancelled in its current state."); Status = KpiPeriodStatus.Cancelled; }
}

public enum KpiPeriodStatus { Draft, InReview, Rejected, Scheduled, Active, Closed, Cancelled }
public sealed record KpiPeriodEffectiveRevision(int Number, IReadOnlyDictionary<Guid, Guid> Selections, string Reason);
