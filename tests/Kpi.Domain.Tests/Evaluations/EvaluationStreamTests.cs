using Kpi.Domain.Evaluations;
using Kpi.Domain.Formula;
using Xunit;

namespace Kpi.Domain.Tests.Evaluations;

public sealed class EvaluationStreamTests
{
    [Fact]
    public void Failed_later_evaluation_does_not_replace_current_success()
    {
        var variables = new[] { FormulaVariableDefinition.Create("value", "Value", FormulaValueType.Decimal) };
        var formula = FormulaEngine.Compile("IF(value = 20, 2, value / 0)", variables, FormulaResultType.Decimal).Formula!;
        var stream = new EvaluationStream(); var now = DateTimeOffset.UtcNow;
        var success = stream.Evaluate(Guid.NewGuid(), Guid.NewGuid(), formula, variables, new Dictionary<string, FormulaValue> { ["value"] = FormulaValue.Decimal(20) }, now);
        var failure = stream.Correct(success, formula, variables, new Dictionary<string, FormulaValue> { ["value"] = FormulaValue.Decimal(30) }, "bad correction", now.AddMinutes(1));
        Assert.False(failure.IsSuccessful);
        Assert.NotNull(stream.Current);
        Assert.Equal(success.Id, stream.Current!.Id);
    }
}
