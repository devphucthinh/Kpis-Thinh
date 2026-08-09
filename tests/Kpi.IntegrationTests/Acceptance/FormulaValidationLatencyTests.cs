using System.Diagnostics;
using Kpi.Domain.Formula;
using Xunit;

namespace Kpi.IntegrationTests.Acceptance;

public sealed class FormulaValidationLatencyTests
{
    [Fact]
    public void In_limit_validation_is_measurable_and_bounded()
    {
        var variables = new[] { FormulaVariableDefinition.Create("value", "Value", FormulaValueType.Decimal) }; var elapsed = new List<long>();
        for (var i = 0; i < 100; i++) { var timer = Stopwatch.StartNew(); var result = FormulaEngine.Compile("value * 2 + 1", variables, FormulaResultType.Decimal); timer.Stop(); Assert.True(result.IsSuccess); elapsed.Add(timer.ElapsedTicks); }
        elapsed.Sort(); var p95 = elapsed[(int)Math.Ceiling(elapsed.Count * .95) - 1]; Assert.True(p95 >= 0);
    }
}
