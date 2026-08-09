using Kpi.Domain.Formula;
using Kpi.Domain.Formula.Serialization;
using Xunit;

namespace Kpi.Domain.Tests.Formula;

public sealed class FormulaDocumentSerializationTests
{
    [Fact]
    public void Decimal_literals_are_serialized_as_invariant_strings()
    {
        var formula = FormulaEngine.Compile("1.25", [], FormulaResultType.Decimal).Formula!;
        var json = FormulaDocumentSerializer.Serialize(formula);
        Assert.Contains("1.25", json, StringComparison.Ordinal);
    }
}
