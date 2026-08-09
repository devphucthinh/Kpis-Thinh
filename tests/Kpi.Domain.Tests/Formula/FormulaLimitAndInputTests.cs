using Kpi.Domain.Formula;
using Xunit;

namespace Kpi.Domain.Tests.Formula;

public sealed class FormulaLimitAndInputTests
{
    [Fact]
    public void Missing_required_input_is_a_failure()
    {
        var variables = new[] { FormulaVariableDefinition.Create("value", "Value", FormulaValueType.Decimal) };
        var formula = FormulaEngine.Compile("value", variables, FormulaResultType.Decimal).Formula!;
        var result = FormulaEngine.Evaluate(formula, variables, new Dictionary<string, FormulaValue>());
        Assert.Equal("FORMULA_INPUT_MISSING", Assert.IsType<EvaluationFailure>(result).Code);
    }
}
