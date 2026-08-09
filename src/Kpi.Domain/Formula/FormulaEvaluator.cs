using System.Diagnostics;
using Kpi.Domain.Formula.Ast;

namespace Kpi.Domain.Formula;

/// <summary>Evaluates only the closed typed AST with Decimal/Boolean semantics.</summary>
public static class FormulaEvaluator
{
    public static EvaluationOutcome Evaluate(FormulaDocument formula, IReadOnlyList<FormulaVariableDefinition> variables, IReadOnlyDictionary<string, FormulaValue> inputs)
    {
        if (!FormulaInputResolver.TryResolve(variables, inputs, out var resolved, out var missing)) return missing!;
        var budget = new EvaluationBudget();
        try { return EvaluateNode(formula.Ast, resolved, budget); }
        catch (OverflowException) { return new EvaluationFailure("FORMULA_OVERFLOW", "The calculation exceeded Decimal limits.", new SourceSpan(0, formula.Source.Length)); }
        catch (InvalidOperationException ex) when (ex.Message is "FORMULA_NODE_LIMIT" or "FORMULA_TIME_LIMIT")
        { return new EvaluationFailure(ex.Message, ex.Message == "FORMULA_NODE_LIMIT" ? "The formula evaluated too many AST nodes." : "The formula exceeded the evaluation time limit.", new SourceSpan(0, formula.Source.Length)); }
    }

    private static EvaluationOutcome EvaluateNode(FormulaNode node, IReadOnlyDictionary<string, FormulaValue> inputs, EvaluationBudget budget)
    {
        budget.Visit(node.Span);
        switch (node)
        {
            case DecimalLiteralNode decimalLiteral: return new EvaluationSuccess(FormulaValue.Decimal(decimalLiteral.Value));
            case BooleanLiteralNode booleanLiteral: return new EvaluationSuccess(FormulaValue.Boolean(booleanLiteral.Value));
            case VariableNode variable when inputs.TryGetValue(variable.Code, out var value): return new EvaluationSuccess(value);
            case VariableNode variable: return new EvaluationFailure("FORMULA_INPUT_MISSING", $"Input '{variable.Code}' is missing.", variable.Span);
            case PercentNode percent:
                return MapDecimal(EvaluateNode(percent.Operand, inputs, budget), value => DecimalPolicy.Normalize(value / 100m));
            case UnaryNode unary:
                return EvaluateUnary(unary, inputs, budget);
            case BinaryNode binary:
                return EvaluateBinary(binary, inputs, budget);
            case CallNode call:
                return EvaluateCall(call, inputs, budget);
            default:
                return new EvaluationFailure("FORMULA_NODE_UNKNOWN", "Formula AST node is not supported.", node.Span);
        }
    }

    private static EvaluationOutcome EvaluateUnary(UnaryNode node, IReadOnlyDictionary<string, FormulaValue> inputs, EvaluationBudget budget)
    {
        var operand = EvaluateNode(node.Operand, inputs, budget);
        if (operand is EvaluationFailure failure) return failure;
        if (node.Operator == UnaryOperator.Negate && operand is EvaluationSuccess { Value: DecimalFormulaValue d }) return new EvaluationSuccess(FormulaValue.Decimal(DecimalPolicy.Normalize(-d.Value)));
        if (node.Operator == UnaryOperator.Not && operand is EvaluationSuccess { Value: BooleanFormulaValue b }) return new EvaluationSuccess(FormulaValue.Boolean(!b.Value));
        return new EvaluationFailure("FORMULA_RUNTIME_TYPE", "Unary operand has the wrong type.", node.Span);
    }

    private static EvaluationOutcome EvaluateBinary(BinaryNode node, IReadOnlyDictionary<string, FormulaValue> inputs, EvaluationBudget budget)
    {
        var left = EvaluateNode(node.Left, inputs, budget);
        if (left is EvaluationFailure leftFailure) return leftFailure;
        if (node.Operator == BinaryOperator.And && left is EvaluationSuccess { Value: BooleanFormulaValue { Value: false } }) return new EvaluationSuccess(FormulaValue.Boolean(false));
        if (node.Operator == BinaryOperator.Or && left is EvaluationSuccess { Value: BooleanFormulaValue { Value: true } }) return new EvaluationSuccess(FormulaValue.Boolean(true));
        var right = EvaluateNode(node.Right, inputs, budget);
        if (right is EvaluationFailure rightFailure) return rightFailure;
        if (left is not EvaluationSuccess leftSuccess || right is not EvaluationSuccess rightSuccess) return new EvaluationFailure("FORMULA_RUNTIME_TYPE", "Binary operand is invalid.", node.Span);
        if (node.Operator is BinaryOperator.And or BinaryOperator.Or && leftSuccess.Value is BooleanFormulaValue lb && rightSuccess.Value is BooleanFormulaValue rb)
            return new EvaluationSuccess(FormulaValue.Boolean(node.Operator == BinaryOperator.And ? lb.Value && rb.Value : lb.Value || rb.Value));
        if (leftSuccess.Value is DecimalFormulaValue ld && rightSuccess.Value is DecimalFormulaValue rd)
        {
            try
            {
                var result = node.Operator switch
                {
                    BinaryOperator.Add => DecimalPolicy.Normalize(ld.Value + rd.Value),
                    BinaryOperator.Subtract => DecimalPolicy.Normalize(ld.Value - rd.Value),
                    BinaryOperator.Multiply => DecimalPolicy.Normalize(ld.Value * rd.Value),
                    BinaryOperator.Divide when rd.Value != 0m => DecimalPolicy.Normalize(ld.Value / rd.Value),
                    BinaryOperator.Mod when rd.Value != 0m => DecimalPolicy.Normalize(ld.Value % rd.Value),
                    BinaryOperator.Divide or BinaryOperator.Mod => throw new DivideByZeroException(),
                    _ => 0m
                };
                if (node.Operator is BinaryOperator.Add or BinaryOperator.Subtract or BinaryOperator.Multiply or BinaryOperator.Divide or BinaryOperator.Mod)
                    return new EvaluationSuccess(FormulaValue.Decimal(result));
                return new EvaluationSuccess(FormulaValue.Boolean(Compare(node.Operator, ld.Value, rd.Value)));
            }
            catch (DivideByZeroException) { return new EvaluationFailure("FORMULA_DIVIDE_BY_ZERO", "Division or MOD by zero is not defined.", node.Span); }
        }
        if (node.Operator is BinaryOperator.Equal or BinaryOperator.NotEqual && leftSuccess.Value is BooleanFormulaValue lbool && rightSuccess.Value is BooleanFormulaValue rbool)
            return new EvaluationSuccess(FormulaValue.Boolean(node.Operator == BinaryOperator.Equal ? lbool.Value == rbool.Value : lbool.Value != rbool.Value));
        return new EvaluationFailure("FORMULA_RUNTIME_TYPE", "Binary values have the wrong type.", node.Span);
    }

    private static EvaluationOutcome EvaluateCall(CallNode node, IReadOnlyDictionary<string, FormulaValue> inputs, EvaluationBudget budget)
    {
        if (node.Name == "IF")
        {
            var condition = EvaluateNode(node.Arguments[0], inputs, budget);
            if (condition is EvaluationFailure failure) return failure;
            var chooseTrue = condition is EvaluationSuccess { Value: BooleanFormulaValue { Value: true } };
            return EvaluateNode(node.Arguments[chooseTrue ? 1 : 2], inputs, budget);
        }
        var args = new List<FormulaValue>();
        foreach (var arg in node.Arguments)
        {
            var outcome = EvaluateNode(arg, inputs, budget);
            if (outcome is EvaluationFailure failure) return failure;
            args.Add(((EvaluationSuccess)outcome).Value);
        }
        if (node.Name == "ABS" && args[0] is DecimalFormulaValue abs) return new EvaluationSuccess(FormulaValue.Decimal(DecimalPolicy.Normalize(Math.Abs(abs.Value))));
        if (node.Name == "MOD" && args[0] is DecimalFormulaValue modA && args[1] is DecimalFormulaValue modB)
            return modB.Value == 0m ? new EvaluationFailure("FORMULA_DIVIDE_BY_ZERO", "MOD by zero is not defined.", node.Span) : new EvaluationSuccess(FormulaValue.Decimal(DecimalPolicy.Normalize(modA.Value % modB.Value)));
        if (node.Name == "ROUND" && args[0] is DecimalFormulaValue roundValue && args[1] is DecimalFormulaValue scale)
        {
            if (scale.Value != decimal.Truncate(scale.Value) || scale.Value < 0 || scale.Value > FormulaLimits.MaxScale)
                return new EvaluationFailure("FORMULA_ROUND_SCALE", "ROUND scale must be an integer between 0 and 10.", node.Span);
            return new EvaluationSuccess(FormulaValue.Decimal(decimal.Round(roundValue.Value, (int)scale.Value, MidpointRounding.AwayFromZero)));
        }
        return new EvaluationFailure("FORMULA_RUNTIME_TYPE", "Function arguments have the wrong type.", node.Span);
    }

    private static EvaluationOutcome MapDecimal(EvaluationOutcome outcome, Func<decimal, decimal> map) =>
        outcome is EvaluationSuccess { Value: DecimalFormulaValue d } ? new EvaluationSuccess(FormulaValue.Decimal(map(d.Value))) : outcome;
    private static bool Compare(BinaryOperator op, decimal left, decimal right) => op switch { BinaryOperator.Equal => left == right, BinaryOperator.NotEqual => left != right, BinaryOperator.Less => left < right, BinaryOperator.LessOrEqual => left <= right, BinaryOperator.Greater => left > right, BinaryOperator.GreaterOrEqual => left >= right, _ => false };
}

/// <summary>Evaluation budget preventing denial-of-service formulas.</summary>
public sealed class EvaluationBudget
{
    private readonly Stopwatch _watch = Stopwatch.StartNew();
    private int _nodes;
    public void Visit(SourceSpan span)
    {
        if (++_nodes > FormulaLimits.MaxEvaluatedNodes) throw new InvalidOperationException("FORMULA_NODE_LIMIT");
        if (_watch.ElapsedMilliseconds > FormulaLimits.MaxMilliseconds) throw new InvalidOperationException("FORMULA_TIME_LIMIT");
    }
}
