using Kpi.Domain.Formula;
using Xunit;

namespace Kpi.Domain.Tests.Formula;

public sealed class FormulaEvaluatorLogicTests
{
    [Fact]
    public void And_or_if_short_circuit_division_by_zero()
    {
        foreach (var source in new[] { "FALSE AND (1 / 0 > 2)", "TRUE OR (1 / 0 > 2)", "IF(FALSE, 1 / 0, 10)" })
        {
            var type = source.StartsWith("IF", StringComparison.Ordinal) ? FormulaResultType.Decimal : FormulaResultType.Boolean;
            var compiled = FormulaEngine.Compile(source, [], type);
            Assert.True(compiled.IsSuccess, string.Join(";", compiled.Diagnostics));
            Assert.IsType<EvaluationSuccess>(FormulaEngine.Evaluate(compiled.Formula!, [], new Dictionary<string, FormulaValue>()));
        }
    }
}
