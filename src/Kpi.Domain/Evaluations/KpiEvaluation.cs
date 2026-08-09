using Kpi.Domain.Formula;
using Kpi.Domain.Kpis;

namespace Kpi.Domain.Evaluations;

/// <summary>Immutable official KPI evaluation attempt.</summary>
public sealed record KpiEvaluation(Guid Id, Guid DefinitionId, Guid VersionId, DateTimeOffset EvaluatedAt, IReadOnlyDictionary<string, FormulaValue> Inputs, EvaluationOutcome Outcome, Guid? SupersedesId = null, string? CorrectionReason = null)
{
    public bool IsSuccessful => Outcome is EvaluationSuccess;
}

/// <summary>Evaluation stream that retains every attempt and one successful Current result.</summary>
public sealed class EvaluationStream
{
    private readonly List<KpiEvaluation> _attempts = [];
    public IReadOnlyList<KpiEvaluation> Attempts => _attempts;
    public KpiEvaluation? Current => _attempts.LastOrDefault(x => x.IsSuccessful && _attempts.All(y => !y.IsSuccessful || y.SupersedesId != x.Id));

    public KpiEvaluation Evaluate(Guid definitionId, Guid versionId, FormulaDocument formula, IReadOnlyList<FormulaVariableDefinition> variables, IReadOnlyDictionary<string, FormulaValue> inputs, DateTimeOffset at)
    {
        var outcome = FormulaEvaluator.Evaluate(formula, variables, inputs);
        var evaluation = new KpiEvaluation(Guid.NewGuid(), definitionId, versionId, at, new Dictionary<string, FormulaValue>(inputs), outcome);
        _attempts.Add(evaluation);
        return evaluation;
    }

    public KpiEvaluation Correct(KpiEvaluation predecessor, FormulaDocument formula, IReadOnlyList<FormulaVariableDefinition> variables, IReadOnlyDictionary<string, FormulaValue> inputs, string reason, DateTimeOffset at)
    {
        if (!predecessor.IsSuccessful) throw new KpiDomainException("Only a successful evaluation can be corrected.");
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Correction reason is required.", nameof(reason));
        var outcome = FormulaEvaluator.Evaluate(formula, variables, inputs);
        var evaluation = new KpiEvaluation(Guid.NewGuid(), predecessor.DefinitionId, predecessor.VersionId, at, new Dictionary<string, FormulaValue>(inputs), outcome, predecessor.Id, reason.Trim());
        _attempts.Add(evaluation);
        return evaluation;
    }
}
