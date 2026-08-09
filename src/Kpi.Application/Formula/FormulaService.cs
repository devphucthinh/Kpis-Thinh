using Kpi.Domain.Formula;

namespace Kpi.Application.Formula;

/// <summary>Application seam for validation and transient Test Run.</summary>
public sealed class FormulaService
{
    public FormulaCompilation Validate(string source, IReadOnlyList<FormulaVariableDefinition> variables, FormulaResultType resultType) => FormulaEngine.Compile(source, variables, resultType);
    public EvaluationOutcome TestRun(FormulaDocument formula, IReadOnlyList<FormulaVariableDefinition> variables, IReadOnlyDictionary<string, FormulaValue> inputs) => FormulaEngine.Evaluate(formula, variables, inputs);
}
