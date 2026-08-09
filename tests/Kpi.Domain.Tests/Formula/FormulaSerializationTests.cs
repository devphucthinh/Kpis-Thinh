using Kpi.Domain.Formula;
using Kpi.Domain.Formula.Serialization;
using Xunit;

namespace Kpi.Domain.Tests.Formula;

public sealed class FormulaSerializationTests
{
    [Fact]
    public void Formula_document_round_trips_source_ast_versions_and_decimal_text()
    {
        var variables = new[] { FormulaVariableDefinition.Create("amount", "Amount", FormulaValueType.Decimal) };
        var compiled = FormulaEngine.Compile(" amount / 3 ", variables, FormulaResultType.Decimal);
        var formula = compiled.Formula!;
        var json = FormulaDocumentSerializer.Serialize(formula);
        var reloaded = FormulaDocumentSerializer.Deserialize(json);

        Assert.Equal(" amount / 3 ", reloaded.Source);
        Assert.Equal(formula.Ast, reloaded.Ast);
        Assert.Contains("\"ast\"", json, StringComparison.Ordinal);
        Assert.Equal(1, reloaded.AstSchemaVersion);
    }
}
