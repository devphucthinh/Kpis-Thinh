using Kpi.Domain.Formula;
using Kpi.Domain.Formula.Ast;
using Xunit;

namespace Kpi.Domain.Tests.Formula;

public sealed class FormulaLimitAndInputTests
{
    [Fact]
    public void Missing_required_input_is_a_failure()
    {
        var variables = new[] { FormulaVariableDefinition.Create("value", "Value", FormulaValueType.Decimal) };
        var formula = FormulaEngine.Compile("value", variables, FormulaResultType.Decimal).Formula!;
        var result = FormulaEngine.Evaluate(formula, variables, new Dictionary<string, FormulaValue>());
        Assert.Equal("FORMULA_INPUT_MISSING", Assert.IsType<EvaluationFailure>(result).Code);
    }

    [Fact]
    public void Missing_optional_input_is_allowed_when_the_formula_does_not_reference_it()
    {
        var optional = FormulaVariableDefinition.Create("optional_value", "Optional", FormulaValueType.Decimal, required: false);
        var formula = FormulaEngine.Compile("1", [optional], FormulaResultType.Decimal).Formula!;
        var result = FormulaEngine.Evaluate(formula, [optional], new Dictionary<string, FormulaValue>());
        Assert.Equal(1m, Assert.IsType<DecimalFormulaValue>(Assert.IsType<EvaluationSuccess>(result).Value).Value);
    }

    [Fact]
    public void Evaluation_budget_limit_is_a_typed_failure_with_a_source_span()
    {
        FormulaNode node = BuildTree(14);
        var result = FormulaEngine.Evaluate(new FormulaDocument("1", node), [], new Dictionary<string, FormulaValue>());
        var failure = Assert.IsType<EvaluationFailure>(result);
        Assert.Equal("FORMULA_NODE_LIMIT", failure.Code);
        Assert.NotNull(failure.Span);

        static FormulaNode BuildTree(int depth)
        {
            if (depth == 0) return new DecimalLiteralNode(1m, new SourceSpan(0, 1));
            var left = BuildTree(depth - 1); var right = BuildTree(depth - 1);
            return new BinaryNode(BinaryOperator.Add, left, right, FormulaResultType.Decimal, new SourceSpan(0, 1));
        }
    }

    [Fact]
    public void Overflow_is_a_typed_failure_instead_of_an_exception()
    {
        var formula = FormulaEngine.Compile("79228162514264337593543950335 * 2", [], FormulaResultType.Decimal).Formula!;
        var result = FormulaEngine.Evaluate(formula, [], new Dictionary<string, FormulaValue>());
        Assert.Equal("FORMULA_OVERFLOW", Assert.IsType<EvaluationFailure>(result).Code);
    }
}
