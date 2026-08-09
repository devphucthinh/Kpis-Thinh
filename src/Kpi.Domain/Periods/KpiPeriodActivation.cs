namespace Kpi.Domain.Periods;

/// <summary>Resolved activation records the exact effective revision used by a live Period.</summary>
public sealed class KpiPeriodActivation
{
    public KpiPeriodActivation(Guid id, Guid periodId, Guid definitionId, Guid versionId, int effectiveRevisionNumber, DateTimeOffset activatedAt)
    { Id = id; PeriodId = periodId; DefinitionId = definitionId; VersionId = versionId; EffectiveRevisionNumber = effectiveRevisionNumber; ActivatedAt = activatedAt; }
    public KpiPeriodActivation(Guid periodId, Guid definitionId, Guid versionId, int effectiveRevisionNumber, DateTimeOffset activatedAt, DateTimeOffset? closedAt = null) : this(Guid.NewGuid(), periodId, definitionId, versionId, effectiveRevisionNumber, activatedAt) { ClosedAt = closedAt; }
    public Guid Id { get; }
    public Guid PeriodId { get; }
    public Guid DefinitionId { get; }
    public Guid VersionId { get; }
    public int EffectiveRevisionNumber { get; }
    public DateTimeOffset ActivatedAt { get; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public void Close(DateTimeOffset at) => ClosedAt ??= at;
}
