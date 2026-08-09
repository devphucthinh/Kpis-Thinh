using Kpi.Domain.Formula.Ast;

namespace Kpi.Domain.Formula;

/// <summary>Compiles approved KPI source into a typed, closed AST.</summary>
public static class FormulaCompiler
{
    public static FormulaCompilation Compile(string source, IReadOnlyList<FormulaVariableDefinition> variables, FormulaResultType expectedResultType)
    {
        var diagnostics = new List<FormulaDiagnostic>();
        if (source is null) throw new ArgumentNullException(nameof(source));
        if (source.Length > FormulaLimits.MaxSourceCharacters)
            diagnostics.Add(new("FORMULA_SOURCE_TOO_LONG", $"Formula exceeds {FormulaLimits.MaxSourceCharacters} characters.", new SourceSpan(FormulaLimits.MaxSourceCharacters, source.Length - FormulaLimits.MaxSourceCharacters)));
        if (variables.Count > FormulaLimits.MaxVariables)
            diagnostics.Add(new("FORMULA_VARIABLE_LIMIT", $"At most {FormulaLimits.MaxVariables} variables are allowed.", new SourceSpan(0, source.Length)));
        var byCode = new Dictionary<string, FormulaVariableDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var variable in variables.OrderBy(x => x.DisplayOrder))
        {
            if (!byCode.TryAdd(variable.Code, variable))
                diagnostics.Add(new("FORMULA_VARIABLE_DUPLICATE", $"Variable '{variable.Code}' is declared more than once.", new SourceSpan(0, 0)));
        }
        var tokens = Tokenizer.Tokenize(source, diagnostics);
        if (diagnostics.Count > 0 && tokens.Count == 1) return new(null, diagnostics);
        var parser = new PrattFormulaParser(source, tokens, byCode, diagnostics);
        var root = parser.ParseExpression();
        if (!parser.AtEnd)
            parser.AddDiagnostic("FORMULA_TRAILING_TOKEN", "Unexpected trailing token.", parser.Current.Span);
        if (root is null) return new(null, diagnostics);
        if (root.ResultType != expectedResultType)
            diagnostics.Add(new("FORMULA_RESULT_TYPE_MISMATCH", $"Formula returns {root.ResultType}, expected {expectedResultType}.", root.Span));
        if (diagnostics.Count > 0) return new(null, diagnostics);
        return new(new FormulaDocument(source, root), diagnostics);
    }
}

internal sealed class PrattFormulaParser
{
    private readonly string _source;
    private readonly IReadOnlyList<Token> _tokens;
    private readonly IReadOnlyDictionary<string, FormulaVariableDefinition> _variables;
    private readonly IList<FormulaDiagnostic> _diagnostics;
    private int _index;
    private int _depth;

    public PrattFormulaParser(string source, IReadOnlyList<Token> tokens, IReadOnlyDictionary<string, FormulaVariableDefinition> variables, IList<FormulaDiagnostic> diagnostics)
    { _source = source; _tokens = tokens; _variables = variables; _diagnostics = diagnostics; }
    public Token Current => _tokens[Math.Min(_index, _tokens.Count - 1)];
    public bool AtEnd => Current.Kind == TokenKind.End;
    public void AddDiagnostic(string code, string message, SourceSpan span) => _diagnostics.Add(new(code, message, span));

    public FormulaNode? ParseExpression(int minimumPrecedence = 0)
    {
        if (++_depth > FormulaLimits.MaxAstDepth)
        {
            AddDiagnostic("FORMULA_DEPTH_LIMIT", $"Formula nesting exceeds {FormulaLimits.MaxAstDepth}.", Current.Span);
            _depth--;
            return null;
        }
        var left = ParsePrefix();
        while (left is not null && TryGetBinary(Current.Kind, out var op, out var precedence) && precedence >= minimumPrecedence)
        {
            var token = Consume();
            var right = ParseExpression(precedence + 1);
            if (right is null) break;
            var resultType = ResolveBinaryType(op, left, right, token.Span);
            left = resultType is null ? null : new BinaryNode(op, left, right, resultType.Value, new SourceSpan(left.Span.Start, right.Span.End - left.Span.Start));
        }
        _depth--;
        return left;
    }

    private FormulaNode? ParsePrefix()
    {
        var token = Current;
        switch (token.Kind)
        {
            case TokenKind.Number:
                Consume();
                return ParsePostfix(new DecimalLiteralNode(token.Number ?? 0m, token.Span));
            case TokenKind.True:
                Consume(); return ParsePostfix(new BooleanLiteralNode(true, token.Span));
            case TokenKind.False:
                Consume(); return ParsePostfix(new BooleanLiteralNode(false, token.Span));
            case TokenKind.Identifier:
                Consume();
                if (Current.Kind == TokenKind.LeftParen) return ParseCall(token);
                if (!_variables.TryGetValue(token.Text, out var variable))
                {
                    AddDiagnostic("FORMULA_VARIABLE_UNKNOWN", $"Unknown variable '{token.Text}'.", token.Span);
                    return ParsePostfix(new VariableNode(token.Text, FormulaResultType.Decimal, token.Span));
                }
                return ParsePostfix(new VariableNode(variable.Code, ToResultType(variable.Type), token.Span));
            case TokenKind.Mod when PeekKind() == TokenKind.LeftParen:
                Consume();
                return ParseCall(token with { Kind = TokenKind.Identifier });
            case TokenKind.Minus:
                Consume();
                var negative = ParseExpression(70);
                if (negative is null) return null;
                if (negative.ResultType != FormulaResultType.Decimal) AddDiagnostic("FORMULA_UNARY_TYPE", "Unary minus requires a Decimal operand.", negative.Span);
                return new UnaryNode(UnaryOperator.Negate, negative, FormulaResultType.Decimal, new SourceSpan(token.Span.Start, negative.Span.End - token.Span.Start));
            case TokenKind.Not:
                Consume();
                var not = ParseExpression(70);
                if (not is null) return null;
                if (not.ResultType != FormulaResultType.Boolean) AddDiagnostic("FORMULA_UNARY_TYPE", "NOT requires a Boolean operand.", not.Span);
                return new UnaryNode(UnaryOperator.Not, not, FormulaResultType.Boolean, new SourceSpan(token.Span.Start, not.Span.End - token.Span.Start));
            case TokenKind.LeftParen:
                Consume();
                var inner = ParseExpression();
                if (Current.Kind != TokenKind.RightParen) AddDiagnostic("FORMULA_PAREN_MISSING", "Missing closing parenthesis.", Current.Span);
                else Consume();
                return inner is null ? null : ParsePostfix(inner);
            default:
                AddDiagnostic("FORMULA_EXPRESSION_EXPECTED", "An expression is required.", token.Span);
                if (!AtEnd) Consume();
                return null;
        }
    }

    private FormulaNode ParsePostfix(FormulaNode node)
    {
        while (Current.Kind == TokenKind.Percent)
        {
            var percent = Consume();
            if (node.ResultType != FormulaResultType.Decimal) AddDiagnostic("FORMULA_PERCENT_TYPE", "Percentage requires a Decimal operand.", node.Span);
            node = new PercentNode(node, new SourceSpan(node.Span.Start, percent.Span.End - node.Span.Start));
        }
        return node;
    }

    private CallNode? ParseCall(Token nameToken)
    {
        Consume();
        var args = new List<FormulaNode>();
        if (Current.Kind != TokenKind.RightParen)
        {
            while (true)
            {
                var argument = ParseExpression();
                if (argument is not null) args.Add(argument);
                if (Current.Kind != TokenKind.Comma) break;
                Consume();
            }
        }
        var end = Current.Kind == TokenKind.RightParen ? Consume().Span.End : nameToken.Span.End;
        var name = nameToken.Text.ToUpperInvariant();
        FormulaResultType? result = name switch
        {
            "IF" when args.Count == 3 && args[0].ResultType == FormulaResultType.Boolean && args[1].ResultType == args[2].ResultType => args[1].ResultType,
            "ROUND" when args.Count == 2 && args[0].ResultType == FormulaResultType.Decimal && args[1].ResultType == FormulaResultType.Decimal => FormulaResultType.Decimal,
            "ABS" when args.Count == 1 && args[0].ResultType == FormulaResultType.Decimal => FormulaResultType.Decimal,
            "MOD" when args.Count == 2 && args[0].ResultType == FormulaResultType.Decimal && args[1].ResultType == FormulaResultType.Decimal => FormulaResultType.Decimal,
            _ => null
        };
        if (result is null) AddDiagnostic("FORMULA_CALL_INVALID", $"Function {name} has invalid name, arity, or argument types.", nameToken.Span);
        return result is null ? null : new CallNode(name, args, result.Value, new SourceSpan(nameToken.Span.Start, end - nameToken.Span.Start));
    }

    private FormulaResultType? ResolveBinaryType(BinaryOperator op, FormulaNode left, FormulaNode right, SourceSpan span)
    {
        var bothDecimal = left.ResultType == FormulaResultType.Decimal && right.ResultType == FormulaResultType.Decimal;
        var bothBoolean = left.ResultType == FormulaResultType.Boolean && right.ResultType == FormulaResultType.Boolean;
        if (op is BinaryOperator.Add or BinaryOperator.Subtract or BinaryOperator.Multiply or BinaryOperator.Divide or BinaryOperator.Mod)
        { if (!bothDecimal) AddDiagnostic("FORMULA_BINARY_TYPE", "Arithmetic operators require Decimal operands.", span); return bothDecimal ? FormulaResultType.Decimal : null; }
        if (op is BinaryOperator.And or BinaryOperator.Or)
        { if (!bothBoolean) AddDiagnostic("FORMULA_BINARY_TYPE", "AND/OR require Boolean operands.", span); return bothBoolean ? FormulaResultType.Boolean : null; }
        if (op is BinaryOperator.Equal or BinaryOperator.NotEqual)
        { if (left.ResultType != right.ResultType) AddDiagnostic("FORMULA_COMPARISON_TYPE", "Compared values must have the same type.", span); return left.ResultType == right.ResultType ? FormulaResultType.Boolean : null; }
        if (!bothDecimal) AddDiagnostic("FORMULA_COMPARISON_TYPE", "Ordered comparisons require Decimal operands.", span);
        return bothDecimal ? FormulaResultType.Boolean : null;
    }

    private static bool TryGetBinary(TokenKind kind, out BinaryOperator op, out int precedence)
    {
        (op, precedence) = kind switch
        {
            TokenKind.Or => (BinaryOperator.Or, 10),
            TokenKind.And => (BinaryOperator.And, 20),
            TokenKind.Equal => (BinaryOperator.Equal, 30),
            TokenKind.NotEqual => (BinaryOperator.NotEqual, 30),
            TokenKind.Less => (BinaryOperator.Less, 30),
            TokenKind.LessOrEqual => (BinaryOperator.LessOrEqual, 30),
            TokenKind.Greater => (BinaryOperator.Greater, 30),
            TokenKind.GreaterOrEqual => (BinaryOperator.GreaterOrEqual, 30),
            TokenKind.Plus => (BinaryOperator.Add, 40),
            TokenKind.Minus => (BinaryOperator.Subtract, 40),
            TokenKind.Star => (BinaryOperator.Multiply, 50),
            TokenKind.Slash => (BinaryOperator.Divide, 50),
            TokenKind.Mod => (BinaryOperator.Mod, 50),
            _ => (default, -1)
        };
        return precedence >= 0;
    }

    private Token Consume() => _tokens[_index++];
    private TokenKind PeekKind() => _index + 1 < _tokens.Count ? _tokens[_index + 1].Kind : TokenKind.End;
    private static FormulaResultType ToResultType(FormulaValueType type) => type == FormulaValueType.Decimal ? FormulaResultType.Decimal : FormulaResultType.Boolean;
}
