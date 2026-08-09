namespace Kpi.Domain.Formula;

/// <summary>Small public formula facade used by Application and delivery.</summary>
public static class FormulaEngine
{
    public static FormulaCompilation Compile(string source, IReadOnlyList<FormulaVariableDefinition> variables, FormulaResultType expectedResultType) => FormulaCompiler.Compile(source, variables, expectedResultType);
    public static EvaluationOutcome Evaluate(FormulaDocument formula, IReadOnlyList<FormulaVariableDefinition> variables, IReadOnlyDictionary<string, FormulaValue> inputs) => FormulaEvaluator.Evaluate(formula, variables, inputs);
}
