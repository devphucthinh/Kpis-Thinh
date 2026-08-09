using Kpi.Domain.Formula;
using Xunit;

namespace Kpi.Domain.Tests.Formula;

public sealed class FormulaVariableDefinitionTests
{
    [Fact]
    public void Variable_requires_lower_snake_case_and_compatible_default()
    {
        Assert.Throws<ArgumentException>(() => FormulaVariableDefinition.Create("Revenue", "Revenue", FormulaValueType.Decimal));
        Assert.Throws<ArgumentException>(() => FormulaVariableDefinition.Create("active", "Active", FormulaValueType.Boolean, defaultValue: FormulaValue.Decimal(1)));
        Assert.Equal(FormulaValueType.Decimal, FormulaVariableDefinition.Create("revenue", "Revenue", FormulaValueType.Decimal).Type);
    }
}
