using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kpi.Application.Persistence;
using Kpi.Domain.Auditing;
using Kpi.Domain.Evaluations;
using Kpi.Domain.Formula;
using Kpi.Domain.Formula.Serialization;
using Kpi.Domain.Periods;
using Kpi.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kpi.Infrastructure.Postgres.Stores;

/// <summary>Relational governance rows plus immutable JSON snapshots.</summary>
public sealed class PostgresGovernedStore(KpiDbContext context) : IKpiGovernedPersistence
{
    public void ExecuteInTransaction(Action mutation)
    {
        if (context.Database.CurrentTransaction is not null) { mutation(); return; }
        using var transaction = context.Database.BeginTransaction();
        try { mutation(); transaction.Commit(); }
        catch { transaction.Rollback(); throw; }
    }

    public void SavePeriod(KpiPeriod period)
    {
        var row = context.Periods.Find(period.Id) ?? new KpiPeriodRow { Id = period.Id };
        row.OrganizationId = period.OrganizationId; row.Code = period.Code; row.Name = period.Name; row.Description = period.Description; row.Cadence = period.Cadence.ToString(); row.StartsAt = period.StartsAt; row.EndsAt = period.EndsAt; row.PlannerId = period.PlannerId; row.ApproverId = period.ApproverId; row.Status = period.Status.ToString(); row.LatestEffectiveRevision = period.LatestEffectiveRevision; row.Revision = period.Revision; row.SelectionsJson = JsonSerializer.Serialize(period.SelectedVersions); row.RevisionsJson = JsonSerializer.Serialize(period.EffectiveRevisions);
        if (context.Entry(row).State == EntityState.Detached) context.Periods.Add(row);
        var existingActivations = context.PeriodActivations.Where(x => x.PeriodId == period.Id).ToList();
        foreach (var activation in period.Activations)
        {
            var activationRow = existingActivations.FirstOrDefault(x => x.Id == activation.Id) ?? new KpiPeriodActivationRow { Id = activation.Id, PeriodId = period.Id };
            activationRow.DefinitionId = activation.DefinitionId; activationRow.VersionId = activation.VersionId; activationRow.EffectiveRevisionNumber = activation.EffectiveRevisionNumber; activationRow.ActivatedAt = activation.ActivatedAt; activationRow.ClosedAt = activation.ClosedAt;
            if (context.Entry(activationRow).State == EntityState.Detached) context.PeriodActivations.Add(activationRow);
        }
        foreach (var amendment in period.Amendments)
        {
            var amendmentRow = context.PeriodAmendments.Find(amendment.Id) ?? new KpiPeriodAmendmentRow { Id = amendment.Id, PeriodId = period.Id };
            amendmentRow.RevisionNumber = amendment.RevisionNumber; amendmentRow.BaseRevisionNumber = amendment.BaseRevisionNumber; amendmentRow.ProposedStartsAt = amendment.ProposedStartsAt; amendmentRow.ProposedEndsAt = amendment.ProposedEndsAt; amendmentRow.ProposedSelectionsJson = JsonSerializer.Serialize(amendment.ProposedSelections); amendmentRow.Reason = amendment.Reason; amendmentRow.ProposedBy = amendment.ProposedBy; amendmentRow.ProposedAt = amendment.ProposedAt; amendmentRow.Status = amendment.Status.ToString(); amendmentRow.ReviewedBy = amendment.ReviewedBy; amendmentRow.ReviewedAt = amendment.ReviewedAt; amendmentRow.ReviewComment = amendment.ReviewComment;
            if (context.Entry(amendmentRow).State == EntityState.Detached) context.PeriodAmendments.Add(amendmentRow);
        }
        context.SaveChanges();
    }

    public void SaveEvaluation(Guid organizationId, KpiEvaluation evaluation)
    {
        if (evaluation.ActivationId is null) throw new InvalidOperationException("Evaluation persistence requires an ActivationId.");
        if (evaluation.IsSuccessful)
        {
            foreach (var current in context.Evaluations.Where(x => x.ActivationId == evaluation.ActivationId.Value && x.IsCurrent && x.Id != evaluation.Id)) current.IsCurrent = false;
        }
        var row = context.Evaluations.Find(evaluation.Id) ?? new KpiEvaluationRow { Id = evaluation.Id };
        row.ActivationId = evaluation.ActivationId.Value; row.VersionId = evaluation.VersionId; row.FormulaJson = evaluation.FormulaSnapshot is null ? "{}" : FormulaDocumentSerializer.Serialize(evaluation.FormulaSnapshot); row.InputsJson = JsonSerializer.Serialize(evaluation.Inputs.ToDictionary(x => x.Key, x => PersistedValue.From(x.Value))); row.OutcomeJson = SerializeOutcome(evaluation.Outcome); row.EvaluatorActorId = evaluation.EvaluatorActorId ?? Guid.Empty; row.SupersedesId = evaluation.SupersedesId; row.CorrectionReason = evaluation.CorrectionReason; row.CorrectionDiffJson = evaluation.CorrectionDiff is null ? null : JsonSerializer.Serialize(evaluation.CorrectionDiff); row.EvaluatedAt = evaluation.EvaluatedAt; row.IsCurrent = evaluation.IsSuccessful;
        if (context.Entry(row).State == EntityState.Detached) context.Evaluations.Add(row);
        context.SaveChanges();
    }

    public void SaveAudit(AuditRecord record)
    {
        if (context.AuditRecords.Find(record.Id) is not null) return;
        context.AuditRecords.Add(new AuditRecordRow { Id = record.Id, OrganizationId = record.OrganizationId, ActorId = record.ActorId, EntityType = record.EntityType, EntityId = record.EntityId, EventType = record.EventType.ToString(), OccurredAt = record.OccurredAt, CorrelationId = record.CorrelationId, Reason = record.Reason, SummaryJson = JsonSerializer.Serialize(new { record.Summary }) });
        context.SaveChanges();
    }

    public IReadOnlyList<KpiPeriod> LoadPeriods(Guid organizationId)
    {
        var rows = context.Periods.Where(x => x.OrganizationId == organizationId).ToList();
        return rows.Select(row =>
        {
            var selections = JsonSerializer.Deserialize<Dictionary<Guid, Guid>>(row.SelectionsJson) ?? [];
            var revisions = JsonSerializer.Deserialize<List<KpiPeriodEffectiveRevision>>(row.RevisionsJson) ?? [];
            var amendments = context.PeriodAmendments.Where(x => x.PeriodId == row.Id).ToList().Select(amendment => KpiPeriodAmendment.Rehydrate(
                amendment.Id,
                amendment.PeriodId,
                amendment.RevisionNumber,
                amendment.BaseRevisionNumber,
                amendment.ProposedStartsAt,
                amendment.ProposedEndsAt,
                JsonSerializer.Deserialize<Dictionary<Guid, Guid>>(amendment.ProposedSelectionsJson) ?? [],
                amendment.Reason,
                amendment.ProposedBy,
                amendment.ProposedAt,
                Enum.Parse<KpiPeriodAmendmentStatus>(amendment.Status),
                amendment.ReviewedBy,
                amendment.ReviewedAt,
                amendment.ReviewComment)).ToArray();
            var activations = context.PeriodActivations.Where(x => x.PeriodId == row.Id).ToList().Select(activation =>
            {
                var result = new KpiPeriodActivation(activation.Id, activation.PeriodId, activation.DefinitionId, activation.VersionId, activation.EffectiveRevisionNumber, activation.ActivatedAt);
                if (activation.ClosedAt is not null) result.Close(activation.ClosedAt.Value);
                return result;
            }).ToArray();
            return KpiPeriod.Rehydrate(row.Id, row.OrganizationId, row.Code, row.Name, row.Description, Enum.Parse<KpiCadence>(row.Cadence), row.StartsAt, row.EndsAt, row.PlannerId, row.ApproverId, Enum.Parse<KpiPeriodStatus>(row.Status), null, row.Revision, row.LatestEffectiveRevision, selections, revisions, amendments, activations);
        }).ToArray();
    }

    public IReadOnlyList<KpiEvaluation> LoadEvaluations(Guid organizationId, Guid definitionId)
    {
        var rows = (from evaluation in context.Evaluations
                    join version in context.Versions on evaluation.VersionId equals version.Id
                    join definition in context.Definitions on version.DefinitionId equals definition.Id
                    where definition.OrganizationId == organizationId && definition.Id == definitionId
                    select new { evaluation, definition }).ToList();
        return rows.Select(item =>
        {
            var row = item.evaluation;
            var formula = ReadFormula(row.FormulaJson);
            return new KpiEvaluation(
                row.Id,
                item.definition.Id,
                row.VersionId,
                row.EvaluatedAt,
                ReadInputs(row.InputsJson),
                ReadOutcome(row.OutcomeJson),
                row.SupersedesId,
                row.CorrectionReason,
                row.ActivationId,
                formula,
                row.EvaluatorActorId == Guid.Empty ? null : row.EvaluatorActorId,
                string.IsNullOrWhiteSpace(row.CorrectionDiffJson) ? null : JsonSerializer.Deserialize<EvaluationCorrectionDiff>(row.CorrectionDiffJson));
        }).OrderBy(x => x.EvaluatedAt).ToArray();
    }

    public IReadOnlyList<AuditRecord> LoadAudit(AuditQuery query)
    {
        var rows = context.AuditRecords.Where(x => x.OrganizationId == query.OrganizationId);
        if (query.EntityType is not null) rows = rows.Where(x => x.EntityType == query.EntityType);
        if (query.EntityId is not null) rows = rows.Where(x => x.EntityId == query.EntityId.Value);
        if (query.ActorId is not null) rows = rows.Where(x => x.ActorId == query.ActorId.Value);
        if (query.EventType is not null) rows = rows.Where(x => x.EventType == query.EventType.Value.ToString());
        if (query.From is not null) rows = rows.Where(x => x.OccurredAt >= query.From.Value);
        if (query.To is not null) rows = rows.Where(x => x.OccurredAt <= query.To.Value);
        return rows.OrderByDescending(x => x.OccurredAt).ToList().Select(row => new AuditRecord(row.Id, row.OrganizationId, row.ActorId, row.EntityType, row.EntityId, Enum.Parse<AuditEventType>(row.EventType), row.OccurredAt, row.CorrelationId, row.Reason, ReadSummary(row.SummaryJson))).ToArray();
    }

    private static FormulaDocument? ReadFormula(string json) => string.IsNullOrWhiteSpace(json) || json == "{}" ? null : FormulaDocumentSerializer.Deserialize(json);

    private static IReadOnlyDictionary<string, FormulaValue> ReadInputs(string json)
    {
        var values = JsonSerializer.Deserialize<Dictionary<string, PersistedValue>>(json) ?? [];
        return values.ToDictionary(x => x.Key, x => x.Value.ToFormulaValue());
    }

    private static EvaluationOutcome ReadOutcome(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var kind = root.GetProperty("kind").GetString();
        if (string.Equals(kind, "Success", StringComparison.OrdinalIgnoreCase))
        {
            var value = JsonSerializer.Deserialize<PersistedValue>(root.GetProperty("value").GetRawText()) ?? new PersistedValue("Null", null, null);
            return new EvaluationSuccess(value.ToFormulaValue());
        }
        return new EvaluationFailure(root.GetProperty("code").GetString() ?? "EVALUATION_FAILED", root.GetProperty("message").GetString() ?? "Evaluation failed.");
    }

    private static string? ReadSummary(string json)
    {
        try { return JsonDocument.Parse(json).RootElement.TryGetProperty("Summary", out var summary) ? summary.GetString() : null; }
        catch (JsonException) { return null; }
    }

    private static string SerializeOutcome(EvaluationOutcome outcome) => outcome switch
    {
        EvaluationSuccess success => JsonSerializer.Serialize(new { kind = "Success", value = PersistedValue.From(success.Value) }),
        EvaluationFailure failure => JsonSerializer.Serialize(new { kind = "Failure", code = failure.Code, message = failure.Message, span = failure.Span }),
        _ => throw new InvalidOperationException("Unknown EvaluationOutcome.")
    };

    private sealed record PersistedValue(string Type, string? Decimal, bool? Boolean)
    {
        public static PersistedValue From(FormulaValue value) => value switch { DecimalFormulaValue d => new("Decimal", d.Value.ToString(CultureInfo.InvariantCulture), null), BooleanFormulaValue b => new("Boolean", null, b.Value), _ => new("Null", null, null) };
        public FormulaValue ToFormulaValue() => Type switch
        {
            "Decimal" when System.Decimal.TryParse(Decimal, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) => FormulaValue.Decimal(value),
            "Boolean" when Boolean is not null => FormulaValue.Boolean(Boolean.Value),
            _ => FormulaValue.Null
        };
    }
}
