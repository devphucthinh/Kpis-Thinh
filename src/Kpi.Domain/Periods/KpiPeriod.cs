using Kpi.Domain.Kpis;

namespace Kpi.Domain.Periods;

/// <summary>Cadenced plan that freezes exact KPI Version selections at approval.</summary>
public sealed class KpiPeriod
{
    private KpiPeriod(Guid id, Guid organizationId, string code, string name, string description, KpiCadence cadence, DateTimeOffset starts, DateTimeOffset ends, Guid plannerId)
    { Id = id; OrganizationId = organizationId; Code = code; Name = name; Description = description; Cadence = cadence; StartsAt = starts; EndsAt = ends; PlannerId = plannerId; }
    public Guid Id { get; }
    public Guid OrganizationId { get; }
    public string Code { get; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public KpiCadence Cadence { get; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public Guid PlannerId { get; }
    public Guid? ApproverId { get; private set; }
    public KpiPeriodStatus Status { get; private set; } = KpiPeriodStatus.Draft;
    public string? RejectionComment { get; private set; }
    public long Revision { get; private set; }
    public Dictionary<Guid, Guid> SelectedVersions { get; } = [];
    public int LatestEffectiveRevision { get; private set; }
    public List<KpiPeriodEffectiveRevision> EffectiveRevisions { get; } = [];
    public List<KpiPeriodAmendment> Amendments { get; } = [];
    public List<KpiPeriodActivation> Activations { get; } = [];

    public static KpiPeriod Create(Guid organizationId, string code, DateTimeOffset starts, DateTimeOffset ends, Guid plannerId) =>
        Create(organizationId, code, code, code, KpiCadence.Monthly, starts, ends, plannerId);

    public static KpiPeriod Create(Guid organizationId, string code, string name, string description, KpiCadence cadence, DateTimeOffset starts, DateTimeOffset ends, Guid plannerId)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Period code, name and description are required.");
        if (ends <= starts) throw new KpiDomainException("Period end must be after start.");
        return new(Guid.NewGuid(), organizationId, code.Trim(), name.Trim(), description.Trim(), cadence, starts, ends, plannerId);
    }

    public static KpiPeriod Rehydrate(Guid id, Guid organizationId, string code, string name, string description, KpiCadence cadence, DateTimeOffset starts, DateTimeOffset ends, Guid plannerId, Guid? approverId, KpiPeriodStatus status, string? rejectionComment, long revision, int latestEffectiveRevision, IReadOnlyDictionary<Guid, Guid> selections, IEnumerable<KpiPeriodEffectiveRevision> effectiveRevisions, IEnumerable<KpiPeriodAmendment> amendments, IEnumerable<KpiPeriodActivation> activations)
    {
        var period = new KpiPeriod(id, organizationId, code.Trim(), name.Trim(), description.Trim(), cadence, starts, ends, plannerId)
        {
            ApproverId = approverId, Status = status, RejectionComment = rejectionComment, Revision = revision, LatestEffectiveRevision = latestEffectiveRevision
        };
        foreach (var selection in selections) period.SelectedVersions[selection.Key] = selection.Value;
        period.EffectiveRevisions.AddRange(effectiveRevisions);
        period.Amendments.AddRange(amendments);
        period.Activations.AddRange(activations);
        return period;
    }

    public void Select(Guid definitionId, Guid versionId)
    {
        if (Status != KpiPeriodStatus.Draft) throw new KpiDomainException("Only Draft Periods can change selections.");
        SelectedVersions[definitionId] = versionId;
        Revision++;
    }

    public void Submit()
    { if (Status != KpiPeriodStatus.Draft || SelectedVersions.Count == 0) throw new KpiDomainException("Period requires selections and Draft status."); Status = KpiPeriodStatus.InReview; Revision++; }

    public void Approve(Guid approverId)
    {
        if (Status != KpiPeriodStatus.InReview || approverId == PlannerId) throw new KpiDomainException("Period approval requires a distinct approver.");
        ApproverId = approverId; Status = KpiPeriodStatus.Scheduled; Revision++;
        if (EffectiveRevisions.Count == 0) EffectiveRevisions.Add(new(0, SelectedVersions.ToDictionary(x => x.Key, x => x.Value), "Original approved plan", StartsAt, EndsAt));
    }

    public void Reject(string comment)
    { if (Status != KpiPeriodStatus.InReview || string.IsNullOrWhiteSpace(comment)) throw new KpiDomainException("Period rejection requires a comment."); RejectionComment = comment.Trim(); Status = KpiPeriodStatus.Rejected; Revision++; }

    public void ReturnToDraft(Guid plannerId)
    { if (Status != KpiPeriodStatus.Rejected || plannerId != PlannerId) throw new KpiDomainException("Only the Planner can reopen a rejected Period."); Status = KpiPeriodStatus.Draft; Revision++; }

    public KpiPeriodAmendment ProposeAmendment(Guid proposerId, IReadOnlyDictionary<Guid, Guid> selections, DateTimeOffset startsAt, DateTimeOffset endsAt, string reason)
    {
        if (Status != KpiPeriodStatus.Scheduled || proposerId != PlannerId || string.IsNullOrWhiteSpace(reason) || endsAt <= startsAt) throw new KpiDomainException("Only a Planner can propose a valid Amendment for a Scheduled Period.");
        var baseRevision = LatestEffectiveRevision;
        var amendment = new KpiPeriodAmendment(Guid.NewGuid(), Id, Amendments.Count + 1, baseRevision, startsAt, endsAt, selections.ToDictionary(x => x.Key, x => x.Value), reason.Trim(), proposerId, DateTimeOffset.UtcNow);
        Amendments.Add(amendment);
        return amendment;
    }

    public void ReviewAmendment(Guid approverId, Guid amendmentId, bool approve, string comment)
    {
        var amendment = Amendments.FirstOrDefault(x => x.Id == amendmentId) ?? throw new KpiDomainException("Amendment was not found.");
        if (amendment.Status != KpiPeriodAmendmentStatus.InReview || approverId == PlannerId || string.IsNullOrWhiteSpace(comment)) throw new KpiDomainException("Amendment review requires a distinct approver and comment.");
        amendment.Decide(approverId, approve, comment.Trim(), DateTimeOffset.UtcNow);
        if (approve)
        {
            if (amendment.BaseRevisionNumber != LatestEffectiveRevision) throw new KpiDomainException("Amendment base revision is stale.");
            LatestEffectiveRevision = amendment.RevisionNumber;
            EffectiveRevisions.Add(new(amendment.RevisionNumber, amendment.ProposedSelections, amendment.Reason, amendment.ProposedStartsAt, amendment.ProposedEndsAt));
        }
        Revision++;
    }

    /// <summary>Compatibility helper; Application commands use ProposeAmendment + ReviewAmendment.</summary>
    public void Amend(Guid proposerId, Guid approverId, IReadOnlyDictionary<Guid, Guid> selections, string reason)
    {
        var amendment = ProposeAmendment(proposerId, selections, StartsAt, EndsAt, reason);
        ReviewAmendment(approverId, amendment.Id, true, reason);
    }

    public IReadOnlyList<KpiPeriodActivation> Activate(DateTimeOffset now)
    {
        if (Status != KpiPeriodStatus.Scheduled || now < StartsAt) throw new KpiDomainException("Period is not due for activation.");
        var revision = EffectiveRevisions.LastOrDefault(x => x.Number == LatestEffectiveRevision) ?? new(0, SelectedVersions.ToDictionary(x => x.Key, x => x.Value), "Original approved plan", StartsAt, EndsAt);
        StartsAt = revision.StartsAt;
        EndsAt = revision.EndsAt;
        Activations.Clear();
        foreach (var selection in revision.Selections) Activations.Add(new(Guid.NewGuid(), Id, selection.Key, selection.Value, revision.Number, now));
        Status = KpiPeriodStatus.Active;
        Revision++;
        return Activations.ToArray();
    }

    public void Close(DateTimeOffset now)
    {
        if (Status != KpiPeriodStatus.Active || now < EndsAt) throw new KpiDomainException("Period is not due for closure.");
        foreach (var activation in Activations) activation.Close(now);
        Status = KpiPeriodStatus.Closed;
        Revision++;
    }

    public void Cancel()
    { if (Status is KpiPeriodStatus.Active or KpiPeriodStatus.Closed or KpiPeriodStatus.Cancelled) throw new KpiDomainException("Period cannot be cancelled in its current state."); Status = KpiPeriodStatus.Cancelled; Revision++; }
}

public enum KpiPeriodStatus { Draft, InReview, Rejected, Scheduled, Active, Closed, Cancelled }
public sealed record KpiPeriodEffectiveRevision(int Number, IReadOnlyDictionary<Guid, Guid> Selections, string Reason, DateTimeOffset StartsAt, DateTimeOffset EndsAt)
{
    public KpiPeriodEffectiveRevision(int number, IReadOnlyDictionary<Guid, Guid> selections, string reason) : this(number, selections, reason, DateTimeOffset.MinValue, DateTimeOffset.MaxValue) { }
}

public enum KpiPeriodAmendmentStatus { InReview, Approved, Rejected }
public sealed class KpiPeriodAmendment(Guid id, Guid periodId, int revisionNumber, int baseRevisionNumber, DateTimeOffset proposedStartsAt, DateTimeOffset proposedEndsAt, IReadOnlyDictionary<Guid, Guid> proposedSelections, string reason, Guid proposedBy, DateTimeOffset proposedAt)
{
    public Guid Id { get; } = id;
    public Guid PeriodId { get; } = periodId;
    public int RevisionNumber { get; } = revisionNumber;
    public int BaseRevisionNumber { get; } = baseRevisionNumber;
    public DateTimeOffset ProposedStartsAt { get; } = proposedStartsAt;
    public DateTimeOffset ProposedEndsAt { get; } = proposedEndsAt;
    public IReadOnlyDictionary<Guid, Guid> ProposedSelections { get; } = proposedSelections;
    public string Reason { get; } = reason;
    public Guid ProposedBy { get; } = proposedBy;
    public DateTimeOffset ProposedAt { get; } = proposedAt;
    public KpiPeriodAmendmentStatus Status { get; private set; } = KpiPeriodAmendmentStatus.InReview;
    public Guid? ReviewedBy { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public string? ReviewComment { get; private set; }
    public void Decide(Guid reviewer, bool approved, string comment, DateTimeOffset at) { Status = approved ? KpiPeriodAmendmentStatus.Approved : KpiPeriodAmendmentStatus.Rejected; ReviewedBy = reviewer; ReviewedAt = at; ReviewComment = comment; }
    public static KpiPeriodAmendment Rehydrate(Guid id, Guid periodId, int revisionNumber, int baseRevisionNumber, DateTimeOffset proposedStartsAt, DateTimeOffset proposedEndsAt, IReadOnlyDictionary<Guid, Guid> proposedSelections, string reason, Guid proposedBy, DateTimeOffset proposedAt, KpiPeriodAmendmentStatus status, Guid? reviewedBy, DateTimeOffset? reviewedAt, string? reviewComment)
    {
        var amendment = new KpiPeriodAmendment(id, periodId, revisionNumber, baseRevisionNumber, proposedStartsAt, proposedEndsAt, proposedSelections, reason, proposedBy, proposedAt);
        if (status != KpiPeriodAmendmentStatus.InReview && reviewedBy is not null && reviewedAt is not null)
            amendment.Decide(reviewedBy.Value, status == KpiPeriodAmendmentStatus.Approved, reviewComment ?? string.Empty, reviewedAt.Value);
        return amendment;
    }
}
