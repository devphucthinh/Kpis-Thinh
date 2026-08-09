using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Formula;
using Kpi.Domain.Formula.Serialization;
using Kpi.Domain.Evaluations;
using Kpi.Domain.Kpis;
using Kpi.Domain.Periods;
using Kpi.Web.ViewModels;

namespace Kpi.Web.Queries;

public sealed class KpiWebReadModelService(KpiOperations kpis, PeriodOperations periods, EvaluationOperations evaluations)
{
    public KpiIndexPageVm GetKpiIndex(ActorContext actor, string? query = null, KpiVersionStatus? status = null)
    {
        var normalizedQuery = string.IsNullOrWhiteSpace(query) ? null : query.Trim();
        var items = kpis.List(actor.OrganizationId)
            .Where(definition => normalizedQuery is null || definition.Code.Value.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase) || definition.Name.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            .Select(definition => ToIndexItem(definition))
            .Where(item => status is null || string.Equals(item.Status, status.Value.ToString(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new(items, normalizedQuery, status);
    }

    public KpiWorkbenchVm? GetWorkbench(ActorContext actor, Guid definitionId, string? notice = null)
    {
        var definition = kpis.List(actor.OrganizationId).FirstOrDefault(x => x.Id == definitionId);
        if (definition is null) return null;

        var currentVersion = definition.Versions
            .Where(x => x.Status == KpiVersionStatus.Published && x.EffectiveTo is null)
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.VersionNumber)
            .FirstOrDefault();

        var versions = definition.Versions
            .OrderByDescending(x => x.VersionNumber)
            .Select(version => ToVersionItem(actor, definition, version, currentVersion?.Id == version.Id))
            .ToArray();
        var editorVersion = definition.Versions
            .Where(x => x.Status != KpiVersionStatus.Retired)
            .OrderByDescending(x => x.VersionNumber)
            .Select(version => ToEditor(actor, definition, version))
            .FirstOrDefault();

        return new(definition.Id, definition.Code.Value, definition.Name, definition.Description, definition.OwnerId, definition.Archived, versions, editorVersion, notice, []);
    }

    public KpiVersionEditorVm? GetVersionEditor(ActorContext actor, Guid definitionId, Guid versionId)
    {
        var definition = kpis.List(actor.OrganizationId).FirstOrDefault(x => x.Id == definitionId);
        var version = definition?.Versions.FirstOrDefault(x => x.Id == versionId);
        return definition is null || version is null ? null : ToEditor(actor, definition, version);
    }

    public IReadOnlyList<KpiPeriodListItemVm> GetPeriodIndex(ActorContext actor) => periods.List(actor.OrganizationId)
        .OrderByDescending(period => period.StartsAt)
        .Select(period => new KpiPeriodListItemVm(period.Id, period.Code, period.Name, period.Cadence, period.StartsAt, period.EndsAt, period.Status, period.SelectedVersions.Count, period.Activations.Count, true))
        .ToArray();

    public KpiPeriodDetailsVm? GetPeriodDetails(ActorContext actor, Guid periodId, string? notice = null)
    {
        var period = periods.List(actor.OrganizationId).FirstOrDefault(x => x.Id == periodId);
        if (period is null) return null;
        var definitions = kpis.List(actor.OrganizationId).OrderBy(x => x.Code.Value, StringComparer.OrdinalIgnoreCase).ToArray();
        var selections = definitions.Select(definition => new KpiPeriodSelectionVm(
            definition.Id,
            definition.Code.Value,
            definition.Name,
            period.SelectedVersions.TryGetValue(definition.Id, out var selected) ? selected : null,
            definition.Versions
                .OrderByDescending(version => version.VersionNumber)
                .Select(version => ToVersionOption(period, version))
                .ToArray())).ToArray();
        var planner = period.PlannerId == actor.ActorId;
        var canApprove = period.Status == KpiPeriodStatus.InReview && actor.Can(KpiCapability.ApprovePeriod) && period.PlannerId != actor.ActorId;
        return new(
            period.Id,
            period.Code,
            period.Name,
            period.Description,
            period.Cadence,
            period.StartsAt,
            period.EndsAt,
            period.PlannerId,
            period.ApproverId,
            period.Status,
            period.RejectionComment,
            period.Revision,
            period.Revision.ToString(System.Globalization.CultureInfo.InvariantCulture),
            selections,
            period.Activations.Select(activation => new KpiPeriodActivationVm(activation.Id, activation.DefinitionId, activation.VersionId, activation.EffectiveRevisionNumber, activation.ActivatedAt, activation.ClosedAt)).ToArray(),
            period.Amendments,
            planner && actor.Can(KpiCapability.PlanPeriod) && period.Status == KpiPeriodStatus.Draft,
            planner && actor.Can(KpiCapability.PlanPeriod) && period.Status == KpiPeriodStatus.Draft && period.SelectedVersions.Count > 0,
            canApprove,
            planner && actor.Can(KpiCapability.PlanPeriod) && period.Status == KpiPeriodStatus.Scheduled,
            period.Status == KpiPeriodStatus.Scheduled,
            period.Status == KpiPeriodStatus.Active,
            notice);
    }

    public KpiEvaluationPageVm? GetEvaluationPage(ActorContext actor, Guid definitionId, Guid activationId, string? notice = null)
    {
        var definition = kpis.List(actor.OrganizationId).FirstOrDefault(x => x.Id == definitionId);
        if (definition is null) return null;
        var activation = periods.List(actor.OrganizationId)
            .SelectMany(period => period.Activations.Select(item => (Period: period, Activation: item)))
            .FirstOrDefault(item => item.Activation.Id == activationId);
        var version = activation.Activation is not null
            ? definition.Versions.FirstOrDefault(x => x.Id == activation.Activation.VersionId)
            : definition.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
        if (version is null) return null;
        var current = evaluations.Current(definitionId, actor.OrganizationId);
        var attempts = evaluations.History(definitionId, actor.OrganizationId)
            .OrderByDescending(x => x.EvaluatedAt)
            .Take(25)
            .Select(item => ToEvaluationAttempt(item, current?.Id == item.Id))
            .ToArray();
        var activationMatches = activation.Activation is not null && activation.Period.Status == KpiPeriodStatus.Active && activation.Activation.DefinitionId == definitionId && activation.Activation.VersionId == version.Id;
        var variables = version.Variables.OrderBy(x => x.DisplayOrder).Select(variable => new FormulaVariableInputVm(variable.Code, variable.DisplayName, variable.Type, variable.Required, Format(variable.DefaultValue), variable.Description, variable.DisplayOrder)).ToArray();
        return new(
            definition.Id,
            definition.Code.Value,
            definition.Name,
            version.Id,
            version.VersionNumber,
            activationId,
            version.Formula.Source,
            FormulaDocumentSerializer.Serialize(version.Formula),
            variables,
            current is null ? null : ToEvaluationAttempt(current, true),
            attempts,
            actor.Can(KpiCapability.Evaluate) && activationMatches,
            notice);
    }

    public KpiEvaluationPageVm? GetEvaluationHistory(ActorContext actor, Guid definitionId, string? notice = null)
    {
        var definition = kpis.List(actor.OrganizationId).FirstOrDefault(x => x.Id == definitionId);
        if (definition is null) return null;
        var activationId = evaluations.History(definitionId, actor.OrganizationId).OrderByDescending(x => x.EvaluatedAt).Select(x => x.ActivationId).FirstOrDefault() ?? Guid.Empty;
        return GetEvaluationPage(actor, definitionId, activationId, notice);
    }

    public KpiCorrectionPageVm? GetCorrectionPage(ActorContext actor, Guid definitionId, Guid predecessorId, string? notice = null)
    {
        var history = GetEvaluationHistory(actor, definitionId, notice);
        var predecessor = history?.Attempts.FirstOrDefault(x => x.Id == predecessorId);
        return history is null || predecessor is null ? null : new(history, predecessor, actor.Can(KpiCapability.Evaluate), notice);
    }

    private static EvaluationAttemptVm ToEvaluationAttempt(KpiEvaluation evaluation, bool current) => new(
        evaluation.Id,
        evaluation.EvaluatedAt,
        evaluation.Outcome switch { EvaluationSuccess => "Success", EvaluationFailure => "Failure", _ => "Unknown" },
        FormatOutcome(evaluation.Outcome),
        current,
        evaluation.VersionId,
        evaluation.ActivationId,
        evaluation.SupersedesId,
        evaluation.CorrectionReason,
        evaluation.Inputs.ToDictionary(item => item.Key, item => Format(item.Value) ?? "null"),
        evaluation.FormulaSnapshot?.Source ?? "(formula snapshot unavailable)",
        evaluation.CorrectionDiff);

    private static string FormatOutcome(EvaluationOutcome outcome) => outcome switch
    {
        EvaluationSuccess success => Format(success.Value) ?? "null",
        EvaluationFailure failure => $"{failure.Code}: {failure.Message}",
        _ => "Unknown"
    };

    private static KpiPeriodVersionOptionVm ToVersionOption(KpiPeriod period, KpiVersion version)
    {
        string? reason = null;
        if (version.Status != KpiVersionStatus.Published) reason = "Version phải Published.";
        else if (version.Cadence != period.Cadence) reason = "Cadence của Version không khớp với kỳ.";
        else if (version.EffectiveFrom is null || version.EffectiveFrom > period.StartsAt) reason = "Version chưa có hiệu lực tại thời điểm bắt đầu kỳ.";
        else if (version.EffectiveTo is not null && version.EffectiveTo < period.EndsAt) reason = "Version hết hiệu lực trước khi kỳ kết thúc.";
        return new(version.Id, version.VersionNumber, version.Name, version.Status, version.Cadence, version.EffectiveFrom, version.EffectiveTo, reason is null, reason);
    }

    private static KpiIndexItemVm ToIndexItem(KpiDefinition definition)
    {
        var current = definition.Versions
            .Where(x => x.Status == KpiVersionStatus.Published && x.EffectiveTo is null)
            .OrderByDescending(x => x.EffectiveFrom)
            .ThenByDescending(x => x.VersionNumber)
            .FirstOrDefault();
        var latest = definition.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
        return new(definition.Id, definition.Code.Value, definition.Name, definition.Description, current?.VersionNumber, (current ?? latest)?.Status.ToString() ?? "NoVersion", definition.OwnerId, definition.Archived);
    }

    private static KpiVersionListItemVm ToVersionItem(ActorContext actor, KpiDefinition definition, KpiVersion version, bool isCurrent)
    {
        var isOwner = definition.OwnerId == actor.ActorId;
        return new(
            version.Id,
            version.VersionNumber,
            version.Name,
            version.Status,
            version.EffectiveFrom,
            version.EffectiveTo,
            isCurrent,
            version.Status == KpiVersionStatus.Draft && isOwner && actor.Can(KpiCapability.EditDraft),
            version.Status == KpiVersionStatus.Draft && isOwner && actor.Can(KpiCapability.EditDraft),
            version.Status == KpiVersionStatus.InReview && !isOwner && actor.Can(KpiCapability.ReviewKpi),
            version.Status == KpiVersionStatus.Approved && actor.Can(KpiCapability.ReviewKpi));
    }

    private static KpiVersionEditorVm ToEditor(ActorContext actor, KpiDefinition definition, KpiVersion version)
    {
        var isOwner = definition.OwnerId == actor.ActorId;
        var variables = version.Variables
            .OrderBy(x => x.DisplayOrder)
            .Select(variable => new FormulaVariableInputVm(variable.Code, variable.DisplayName, variable.Type, variable.Required, Format(variable.DefaultValue), variable.Description, variable.DisplayOrder))
            .ToArray();
        return new(
            version.Id,
            version.VersionNumber,
            version.Name,
            version.Description,
            version.Formula.Source,
            variables,
            FormulaDocumentSerializer.Serialize(version.Formula),
            version.ChangeSummary,
            version.Status,
            version.Revision,
            version.Revision.ToString(System.Globalization.CultureInfo.InvariantCulture),
            [],
            version.Status == KpiVersionStatus.Draft && isOwner && actor.Can(KpiCapability.EditDraft),
            version.Status == KpiVersionStatus.Draft && isOwner && actor.Can(KpiCapability.EditDraft),
            version.Status == KpiVersionStatus.InReview && !isOwner && actor.Can(KpiCapability.ReviewKpi),
            version.Status == KpiVersionStatus.Approved && actor.Can(KpiCapability.ReviewKpi),
            isOwner && actor.Can(KpiCapability.EditDraft),
            !definition.Archived && (isOwner || actor.Can(KpiCapability.Administrator)),
            definition.Archived && actor.Can(KpiCapability.Administrator));
    }

    private static string? Format(FormulaValue? value) => value switch
    {
        DecimalFormulaValue decimalValue => decimalValue.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
        BooleanFormulaValue booleanValue => booleanValue.Value.ToString().ToLowerInvariant(),
        _ => null
    };
}
