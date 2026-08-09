using Kpi.Domain.Formula;
using Kpi.Domain.Kpis;

namespace Kpi.Domain.Evaluations;

/// <summary>Immutable official KPI evaluation attempt.</summary>
public sealed record KpiEvaluation(Guid Id, Guid DefinitionId, Guid VersionId, DateTimeOffset EvaluatedAt, IReadOnlyDictionary<string, FormulaValue> Inputs, EvaluationOutcome Outcome, Guid? SupersedesId = null, string? CorrectionReason = null, Guid? ActivationId = null, FormulaDocument? FormulaSnapshot = null, Guid? EvaluatorActorId = null, EvaluationCorrectionDiff? CorrectionDiff = null)
{
    public bool IsSuccessful => Outcome is EvaluationSuccess;
}

public sealed record EvaluationCorrectionDiff(IReadOnlyDictionary<string, string> ChangedInputs, string OldResult, string NewResult);

/// <summary>Evaluation stream that retains every attempt and one successful Current result.</summary>
public sealed class EvaluationStream
{
    private readonly List<KpiEvaluation> _attempts = [];
    public IReadOnlyList<KpiEvaluation> Attempts => _attempts;
    public KpiEvaluation? Current => _attempts.LastOrDefault(x => x.IsSuccessful && _attempts.All(y => !y.IsSuccessful || y.SupersedesId != x.Id));
    public void Replace(IEnumerable<KpiEvaluation> evaluations)
    {
        _attempts.Clear();
        _attempts.AddRange(evaluations.OrderBy(x => x.EvaluatedAt));
    }

    public KpiEvaluation Evaluate(Guid definitionId, Guid versionId, FormulaDocument formula, IReadOnlyList<FormulaVariableDefinition> variables, IReadOnlyDictionary<string, FormulaValue> inputs, DateTimeOffset at, Guid? activationId = null, Guid? evaluatorActorId = null)
    {
        var outcome = FormulaEvaluator.Evaluate(formula, variables, inputs);
        var resolvedInputs = ResolveInputs(variables, inputs);
        var evaluation = new KpiEvaluation(Guid.NewGuid(), definitionId, versionId, at, resolvedInputs, outcome, ActivationId: activationId, FormulaSnapshot: formula, EvaluatorActorId: evaluatorActorId);
        _attempts.Add(evaluation);
        return evaluation;
    }

    public KpiEvaluation Correct(KpiEvaluation predecessor, FormulaDocument formula, IReadOnlyList<FormulaVariableDefinition> variables, IReadOnlyDictionary<string, FormulaValue> inputs, string reason, DateTimeOffset at, Guid? evaluatorActorId = null)
    {
        if (!predecessor.IsSuccessful) throw new KpiDomainException("Only a successful evaluation can be corrected.");
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Correction reason is required.", nameof(reason));
        var outcome = FormulaEvaluator.Evaluate(formula, variables, inputs);
        var resolvedInputs = ResolveInputs(variables, inputs);
        var changes = resolvedInputs.Where(x => !predecessor.Inputs.TryGetValue(x.Key, out var old) || !Equals(old, x.Value)).ToDictionary(x => x.Key, x => $"{Format(predecessor.Inputs.TryGetValue(x.Key, out var old) ? old : FormulaValue.Null)} -> {Format(x.Value)}");
        var diff = new EvaluationCorrectionDiff(changes, FormatOutcome(predecessor.Outcome), FormatOutcome(outcome));
        var evaluation = new KpiEvaluation(Guid.NewGuid(), predecessor.DefinitionId, predecessor.VersionId, at, resolvedInputs, outcome, predecessor.Id, reason.Trim(), predecessor.ActivationId, formula, evaluatorActorId, diff);
        _attempts.Add(evaluation);
        return evaluation;
    }

    private static IReadOnlyDictionary<string, FormulaValue> ResolveInputs(IReadOnlyList<FormulaVariableDefinition> variables, IReadOnlyDictionary<string, FormulaValue> inputs)
    {
        return FormulaInputResolver.TryResolve(variables, inputs, out var resolved, out _) ? resolved : new Dictionary<string, FormulaValue>(inputs);
    }

    private static string Format(FormulaValue value) => value switch { DecimalFormulaValue d => d.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), BooleanFormulaValue b => b.Value ? "true" : "false", _ => "null" };
    private static string FormatOutcome(EvaluationOutcome outcome) => outcome switch { EvaluationSuccess success => Format(success.Value), EvaluationFailure failure => $"Failure:{failure.Code}", _ => "unknown" };
}
