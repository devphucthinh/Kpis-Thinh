using Kpi.Domain.Formula;
using Xunit;

namespace Kpi.Domain.Tests.Formula;

public sealed class FormulaCompilerGrammarTests
{
    [Theory]
    [InlineData("1 + 2 * 3")]
    [InlineData("ROUND(ABS(-2), 0)")]
    [InlineData("MOD(5, 2)")]
    [InlineData("NOT FALSE")]
    public void Approved_constructs_compile(string source)
    {
        var resultType = source.Contains("NOT", StringComparison.OrdinalIgnoreCase) ? FormulaResultType.Boolean : FormulaResultType.Decimal;
        var result = FormulaEngine.Compile(source, [], resultType);
        Assert.True(result.IsSuccess, string.Join(";", result.Diagnostics));
    }
}
