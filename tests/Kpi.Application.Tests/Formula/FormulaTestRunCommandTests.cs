using Kpi.Application.Formula;
using Kpi.Domain.Formula;
using Xunit;

namespace Kpi.Application.Tests.Formula;

public sealed class FormulaTestRunCommandTests
{
    [Fact]
    public void Test_run_returns_outcome_without_persistence_dependency()
    {
        var variables = new[] { FormulaVariableDefinition.Create("value", "Value", FormulaValueType.Decimal) };
        var formula = FormulaEngine.Compile("value * 2", variables, FormulaResultType.Decimal).Formula!;
        var command = new FormulaTestRunCommand(new FormulaService());
        var outcome = command.Execute(formula, variables, new Dictionary<string, FormulaValue> { ["value"] = FormulaValue.Decimal(5) });
        Assert.Equal(10m, Assert.IsType<DecimalFormulaValue>(Assert.IsType<EvaluationSuccess>(outcome).Value).Value);
    }
}
