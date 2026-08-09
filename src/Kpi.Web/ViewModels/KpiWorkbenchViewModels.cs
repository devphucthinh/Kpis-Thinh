using Kpi.Domain.Formula;
using Kpi.Domain.Kpis;

namespace Kpi.Web.ViewModels;

public sealed record FormulaVariableInputVm(
    string Code,
    string DisplayName,
    FormulaValueType Type,
    bool Required,
    string? DefaultValue,
    string? Description,
    int DisplayOrder);

public sealed record KpiVersionListItemVm(
    Guid Id,
    int VersionNumber,
    string Name,
    KpiVersionStatus Status,
    DateTimeOffset? EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    bool IsCurrent,
    bool CanEdit,
    bool CanSubmit,
    bool CanReview,
    bool CanPublish);

public sealed record KpiIndexItemVm(
    Guid Id,
    string Code,
    string Name,
    string Description,
    int? CurrentVersionNumber,
    string Status,
    Guid OwnerId,
    bool Archived);

public sealed record KpiIndexPageVm(
    IReadOnlyList<KpiIndexItemVm> Items,
    string? Query,
    KpiVersionStatus? Status);

public sealed record KpiWorkbenchVm(
    Guid DefinitionId,
    string Code,
    string Name,
    string Description,
    Guid OwnerId,
    bool Archived,
    IReadOnlyList<KpiVersionListItemVm> Versions,
    KpiVersionEditorVm? Draft,
    string? Notice,
    IReadOnlyList<string> Diagnostics);

public sealed record KpiVersionEditorVm(
    Guid VersionId,
    int VersionNumber,
    string Name,
    string Description,
    string Source,
    IReadOnlyList<FormulaVariableInputVm> Variables,
    string AstJson,
    string? ChangeSummary,
    KpiVersionStatus Status,
    long Revision,
    string ConcurrencyToken,
    IReadOnlyList<string> Diagnostics,
    bool CanSave,
    bool CanSubmit,
    bool CanReview,
    bool CanPublish,
    bool CanClone,
    bool CanArchive,
    bool CanRestore);
