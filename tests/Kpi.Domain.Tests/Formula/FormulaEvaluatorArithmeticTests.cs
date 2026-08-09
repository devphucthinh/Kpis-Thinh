using Kpi.Domain.Formula;
using Xunit;

namespace Kpi.Domain.Tests.Formula;

public sealed class FormulaEvaluatorArithmeticTests
{
    [Fact]
    public void Round_uses_midpoint_away_from_zero_and_mod_is_remainder()
    {
        var round = FormulaEngine.Compile("ROUND(2.345, 2)", [], FormulaResultType.Decimal).Formula!;
        var mod = FormulaEngine.Compile("MOD(5, 2)", [], FormulaResultType.Decimal).Formula!;
        Assert.Equal(2.35m, Assert.IsType<DecimalFormulaValue>(Assert.IsType<EvaluationSuccess>(FormulaEngine.Evaluate(round, [], new Dictionary<string, FormulaValue>())).Value).Value);
        Assert.Equal(1m, Assert.IsType<DecimalFormulaValue>(Assert.IsType<EvaluationSuccess>(FormulaEngine.Evaluate(mod, [], new Dictionary<string, FormulaValue>())).Value).Value);
    }
}
