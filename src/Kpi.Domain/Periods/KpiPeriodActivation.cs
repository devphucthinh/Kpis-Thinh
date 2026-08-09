namespace Kpi.Domain.Periods;

/// <summary>Resolved activation records the exact effective revision used by a live Period.</summary>
public sealed record KpiPeriodActivation(Guid PeriodId, Guid DefinitionId, Guid VersionId, int EffectiveRevisionNumber, DateTimeOffset ActivatedAt, DateTimeOffset? ClosedAt = null);
