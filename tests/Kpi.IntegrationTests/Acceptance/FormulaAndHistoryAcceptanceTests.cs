using Kpi.Domain.Formula;
using Xunit;

namespace Kpi.IntegrationTests.Acceptance;

public sealed class FormulaAndHistoryAcceptanceTests
{
    [Fact]
    public void Formula_is_repeatable_and_test_run_is_transient_at_domain_boundary()
    {
        var variables = new[] { FormulaVariableDefinition.Create("value", "Value", FormulaValueType.Decimal) };
        var formula = FormulaEngine.Compile("ROUND(value / 3, 2)", variables, FormulaResultType.Decimal).Formula!;
        for (var i = 0; i < 100; i++)
        {
            var outcome = FormulaEngine.Evaluate(formula, variables, new Dictionary<string, FormulaValue> { ["value"] = FormulaValue.Decimal(10m) });
            Assert.Equal(3.33m, Assert.IsType<DecimalFormulaValue>(Assert.IsType<EvaluationSuccess>(outcome).Value).Value);
        }
    }
}
