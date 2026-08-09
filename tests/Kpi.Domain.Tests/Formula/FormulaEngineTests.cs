using Kpi.Domain.Formula;
using Kpi.Domain.Formula.Ast;
using Xunit;

namespace Kpi.Domain.Tests.Formula;

public sealed class FormulaEngineTests
{
    private static readonly IReadOnlyList<FormulaVariableDefinition> Variables =
    [
        FormulaVariableDefinition.Create("revenue", "Revenue", FormulaValueType.Decimal, true, null, 0),
        FormulaVariableDefinition.Create("target", "Target", FormulaValueType.Decimal, true, null, 1),
        FormulaVariableDefinition.Create("active", "Active", FormulaValueType.Boolean, true, null, 2)
    ];

    [Fact]
    public void Compile_preserves_source_and_precedence()
    {
        var result = FormulaEngine.Compile(" revenue + target * 2 ", Variables, FormulaResultType.Decimal);

        Assert.True(result.IsSuccess);
        Assert.Equal(" revenue + target * 2 ", result.Formula!.Source);
        var add = Assert.IsType<BinaryNode>(result.Formula.Ast);
        Assert.Equal(BinaryOperator.Add, add.Operator);
        Assert.Equal(BinaryOperator.Multiply, Assert.IsType<BinaryNode>(add.Right).Operator);
    }

    [Fact]
    public void Evaluate_supports_decimal_logic_and_percentage()
    {
        var compiled = FormulaEngine.Compile("IF(revenue > target AND active, ROUND(revenue / target * 100, 2), 0)", Variables, FormulaResultType.Decimal);
        Assert.True(compiled.IsSuccess, string.Join(Environment.NewLine, compiled.Diagnostics));

        var outcome = FormulaEngine.Evaluate(compiled.Formula!, Variables, new Dictionary<string, FormulaValue>
        {
            ["revenue"] = FormulaValue.Decimal(130m),
            ["target"] = FormulaValue.Decimal(100m),
            ["active"] = FormulaValue.Boolean(true)
        });

        var success = Assert.IsType<EvaluationSuccess>(outcome);
        Assert.Equal(130m, Assert.IsType<DecimalFormulaValue>(success.Value).Value);
    }

    [Fact]
    public void Evaluate_short_circuits_if_branch_and_and_or()
    {
        var ifFormula = FormulaEngine.Compile("IF(FALSE, 1 / 0, 10)", [], FormulaResultType.Decimal);
        var ifOutcome = FormulaEngine.Evaluate(ifFormula.Formula!, [], new Dictionary<string, FormulaValue>());
        Assert.Equal(10m, Assert.IsType<DecimalFormulaValue>(Assert.IsType<EvaluationSuccess>(ifOutcome).Value).Value);

        var andFormula = FormulaEngine.Compile("FALSE AND (1 / 0 > 2)", [], FormulaResultType.Boolean);
        Assert.True(andFormula.IsSuccess, string.Join(Environment.NewLine, andFormula.Diagnostics));
        var andOutcome = FormulaEngine.Evaluate(andFormula.Formula!, [], new Dictionary<string, FormulaValue>());
        Assert.False(Assert.IsType<BooleanFormulaValue>(Assert.IsType<EvaluationSuccess>(andOutcome).Value).Value);
    }

    [Fact]
    public void Evaluate_reports_division_by_zero()
    {
        var compiled = FormulaEngine.Compile("10 / 0", [], FormulaResultType.Decimal);
        var outcome = FormulaEngine.Evaluate(compiled.Formula!, [], new Dictionary<string, FormulaValue>());
        var failure = Assert.IsType<EvaluationFailure>(outcome);
        Assert.Equal("FORMULA_DIVIDE_BY_ZERO", failure.Code);
    }
}
