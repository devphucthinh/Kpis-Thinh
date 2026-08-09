using Kpi.Domain.Auditing;
using Kpi.Domain.Periods;

namespace Kpi.Web.ViewModels;

public sealed record OverviewMetricVm(string Label, int Value, string Detail, string Tone);

public sealed record OverviewActionVm(string Label, string Description, string Href, string Tone);

public sealed record OverviewEvaluationVm(Guid DefinitionId, string DefinitionCode, DateTimeOffset EvaluatedAt, string OutcomeKind, string OutcomeText);

public sealed record OverviewPageVm(
    IReadOnlyList<OverviewMetricVm> Metrics,
    IReadOnlyList<OverviewActionVm> Actions,
    IReadOnlyList<OverviewEvaluationVm> RecentEvaluations,
    IReadOnlyList<KpiPeriodListItemVm> UpcomingPeriods);

public sealed record AuditItemVm(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid ActorId,
    string EntityType,
    Guid EntityId,
    AuditEventType EventType,
    string? Reason,
    string? Summary);

public sealed record AuditPageVm(
    IReadOnlyList<AuditItemVm> Items,
    string? EntityType,
    Guid? EntityId,
    Guid? ActorId,
    AuditEventType? EventType,
    DateTimeOffset? From,
    DateTimeOffset? To);
