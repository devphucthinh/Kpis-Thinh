using Kpi.Domain.Kpis;
using Kpi.Domain.Periods;

namespace Kpi.Web.ViewModels;

public sealed class CreatePeriodModel
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public KpiCadence Cadence { get; set; } = KpiCadence.Monthly;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
}

public sealed record KpiPeriodListItemVm(
    Guid Id,
    string Code,
    string Name,
    KpiCadence Cadence,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    KpiPeriodStatus Status,
    int SelectionCount,
    int ActivationCount,
    bool CanOpen);

public sealed record KpiPeriodVersionOptionVm(
    Guid VersionId,
    int VersionNumber,
    string VersionName,
    KpiVersionStatus Status,
    KpiCadence Cadence,
    DateTimeOffset? EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    bool IsEligible,
    string? IneligibilityReason);

public sealed record KpiPeriodSelectionVm(
    Guid DefinitionId,
    string Code,
    string Name,
    Guid? SelectedVersionId,
    IReadOnlyList<KpiPeriodVersionOptionVm> Versions);

public sealed record KpiPeriodActivationVm(
    Guid Id,
    Guid DefinitionId,
    Guid VersionId,
    int EffectiveRevisionNumber,
    DateTimeOffset ActivatedAt,
    DateTimeOffset? ClosedAt);

public sealed record KpiPeriodDetailsVm(
    Guid Id,
    string Code,
    string Name,
    string Description,
    KpiCadence Cadence,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    Guid PlannerId,
    Guid? ApproverId,
    KpiPeriodStatus Status,
    string? RejectionComment,
    long Revision,
    string ConcurrencyToken,
    IReadOnlyList<KpiPeriodSelectionVm> Selections,
    IReadOnlyList<KpiPeriodActivationVm> Activations,
    IReadOnlyList<KpiPeriodAmendment> Amendments,
    bool CanEdit,
    bool CanSubmit,
    bool CanApprove,
    bool CanAmend,
    bool CanActivate,
    bool CanClose,
    string? Notice);
