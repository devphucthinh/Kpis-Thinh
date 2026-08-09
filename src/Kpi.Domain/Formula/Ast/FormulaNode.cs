namespace Kpi.Domain.Formula.Ast;

/// <summary>Typed, closed AST node base.</summary>
public abstract record FormulaNode(FormulaResultType ResultType, SourceSpan Span);

public sealed record DecimalLiteralNode(decimal Value, SourceSpan Span) : FormulaNode(FormulaResultType.Decimal, Span);
public sealed record BooleanLiteralNode(bool Value, SourceSpan Span) : FormulaNode(FormulaResultType.Boolean, Span);
public sealed record VariableNode(string Code, FormulaResultType ResultType, SourceSpan Span) : FormulaNode(ResultType, Span);
public sealed record UnaryNode(UnaryOperator Operator, FormulaNode Operand, FormulaResultType ResultType, SourceSpan Span) : FormulaNode(ResultType, Span);
public sealed record BinaryNode(BinaryOperator Operator, FormulaNode Left, FormulaNode Right, FormulaResultType ResultType, SourceSpan Span) : FormulaNode(ResultType, Span);
public sealed record PercentNode(FormulaNode Operand, SourceSpan Span) : FormulaNode(FormulaResultType.Decimal, Span);
public sealed record CallNode(string Name, IReadOnlyList<FormulaNode> Arguments, FormulaResultType ResultType, SourceSpan Span) : FormulaNode(ResultType, Span);

public enum UnaryOperator { Negate, Not }
public enum BinaryOperator { Add, Subtract, Multiply, Divide, Mod, Equal, NotEqual, Less, LessOrEqual, Greater, GreaterOrEqual, And, Or }
