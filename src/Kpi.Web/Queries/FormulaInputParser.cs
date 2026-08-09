using System.Globalization;
using Kpi.Application.Common;
using Kpi.Domain.Formula;
using Kpi.Web.ViewModels;

namespace Kpi.Web.Queries;

public static class FormulaInputParser
{
    public static ApplicationResult<IReadOnlyDictionary<string, FormulaValue>> Parse(
        IReadOnlyList<FormulaVariableInputVm> variables,
        IReadOnlyDictionary<string, string> inputs)
    {
        var values = new Dictionary<string, FormulaValue>(StringComparer.Ordinal);
        foreach (var variable in variables.OrderBy(x => x.DisplayOrder))
        {
            inputs.TryGetValue(variable.Code, out var raw);
            raw = string.IsNullOrWhiteSpace(raw) ? variable.DefaultValue : raw;
            if (string.IsNullOrWhiteSpace(raw))
            {
                if (variable.Required)
                    return ApplicationResult<IReadOnlyDictionary<string, FormulaValue>>.Failure("FORMULA_INPUT_INVALID", $"Input '{variable.Code}' is required.");
                values[variable.Code] = FormulaValue.Null;
                continue;
            }

            if (variable.Type == FormulaValueType.Decimal && decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue))
            {
                values[variable.Code] = FormulaValue.Decimal(decimalValue);
                continue;
            }

            if (variable.Type == FormulaValueType.Boolean && bool.TryParse(raw, out var booleanValue))
            {
                values[variable.Code] = FormulaValue.Boolean(booleanValue);
                continue;
            }

            return ApplicationResult<IReadOnlyDictionary<string, FormulaValue>>.Failure(
                "FORMULA_INPUT_INVALID",
                $"Input '{variable.Code}' is not a valid {variable.Type} value.");
        }

        return ApplicationResult<IReadOnlyDictionary<string, FormulaValue>>.Success(values);
    }
}
