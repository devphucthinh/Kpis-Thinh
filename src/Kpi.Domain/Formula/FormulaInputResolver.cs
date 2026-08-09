namespace Kpi.Domain.Formula;

/// <summary>Resolves explicit inputs and compatible variable defaults.</summary>
public static class FormulaInputResolver
{
    public static bool TryResolve(IReadOnlyList<FormulaVariableDefinition> variables, IReadOnlyDictionary<string, FormulaValue> inputs, out Dictionary<string, FormulaValue> resolved, out EvaluationFailure? failure)
    {
        resolved = new(StringComparer.OrdinalIgnoreCase);
        foreach (var variable in variables)
        {
            if (inputs.TryGetValue(variable.Code, out var value))
            {
                if (!FormulaVariableDefinition.Matches(variable.Type, value))
                { failure = new("FORMULA_INPUT_TYPE", $"Input '{variable.Code}' has the wrong type."); return false; }
                resolved[variable.Code] = value;
                continue;
            }
            if (variable.DefaultValue is not null) { resolved[variable.Code] = variable.DefaultValue; continue; }
            if (variable.Required)
            {
                failure = new("FORMULA_INPUT_MISSING", $"Required input '{variable.Code}' is missing.");
                return false;
            }
            // Optional variables are absent from the resolved snapshot. If the AST
            // references one, the evaluator returns a stable missing-input failure.
        }
        failure = null;
        return true;
    }
}
