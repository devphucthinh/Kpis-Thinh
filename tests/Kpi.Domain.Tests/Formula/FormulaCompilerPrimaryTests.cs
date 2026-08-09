using Kpi.Domain.Formula;
using Xunit;

namespace Kpi.Domain.Tests.Formula;

public sealed class FormulaCompilerPrimaryTests
{
    [Fact]
    public void Unknown_variable_returns_stable_diagnostic_with_span()
    {
        var result = FormulaEngine.Compile("unknown + 1", [], FormulaResultType.Decimal);
        Assert.Contains(result.Diagnostics, x => x.Code == "FORMULA_VARIABLE_UNKNOWN");
    }
}
