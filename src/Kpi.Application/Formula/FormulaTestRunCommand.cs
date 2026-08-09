using Kpi.Domain.Formula;

namespace Kpi.Application.Formula;

/// <summary>Transient test-run operation; it has no Evaluation or Audit store dependency.</summary>
public sealed class FormulaTestRunCommand(FormulaService formulaService)
{
    public EvaluationOutcome Execute(FormulaDocument formula, IReadOnlyList<FormulaVariableDefinition> variables, IReadOnlyDictionary<string, FormulaValue> inputs) => formulaService.TestRun(formula, variables, inputs);
}
