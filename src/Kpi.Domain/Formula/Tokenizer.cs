using System.Globalization;

namespace Kpi.Domain.Formula;

/// <summary>Safe tokenizer for the closed formula language.</summary>
public static class Tokenizer
{
    public static IReadOnlyList<Token> Tokenize(string source, IList<FormulaDiagnostic>? diagnostics = null)
    {
        var tokens = new List<Token>();
        diagnostics ??= new List<FormulaDiagnostic>();
        var i = 0;
        while (i < source.Length)
        {
            var c = source[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }
            var start = i;
            if (char.IsLetter(c) || c == '_')
            {
                i++;
                while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] == '_')) i++;
                var text = source[start..i];
                var upper = text.ToUpperInvariant();
                var lexKind = upper switch
                {
                    "TRUE" => TokenKind.True,
                    "FALSE" => TokenKind.False,
                    "AND" => TokenKind.And,
                    "OR" => TokenKind.Or,
                    "NOT" => TokenKind.Not,
                    "MOD" => TokenKind.Mod,
                    _ => TokenKind.Identifier
                };
                tokens.Add(new(lexKind, text, new SourceSpan(start, i - start)));
                continue;
            }
            if (char.IsDigit(c) || c == '.')
            {
                i++;
                while (i < source.Length && (char.IsDigit(source[i]) || source[i] == '.')) i++;
                var text = source[start..i];
                if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                    diagnostics.Add(new("FORMULA_LITERAL_INVALID", "Decimal literal is invalid.", new SourceSpan(start, i - start)));
                else
                    tokens.Add(new(TokenKind.Number, text, new SourceSpan(start, i - start), number));
                continue;
            }
            TokenKind? kind = c switch
            {
                '+' => TokenKind.Plus,
                '-' => TokenKind.Minus,
                '*' => TokenKind.Star,
                '/' => TokenKind.Slash,
                '%' => TokenKind.Percent,
                '(' => TokenKind.LeftParen,
                ')' => TokenKind.RightParen,
                ',' => TokenKind.Comma,
                '=' => TokenKind.Equal,
                '!' when i + 1 < source.Length && source[i + 1] == '=' => TokenKind.NotEqual,
                '<' when i + 1 < source.Length && source[i + 1] == '=' => TokenKind.LessOrEqual,
                '>' when i + 1 < source.Length && source[i + 1] == '=' => TokenKind.GreaterOrEqual,
                '<' => TokenKind.Less,
                '>' => TokenKind.Greater,
                _ => null
            };
            if (kind is null)
            {
                diagnostics.Add(new("FORMULA_TOKEN_INVALID", $"Unsupported character '{c}'.", new SourceSpan(start, 1)));
                i++;
                continue;
            }
            var length = kind is TokenKind.NotEqual or TokenKind.LessOrEqual or TokenKind.GreaterOrEqual ? 2 : 1;
            tokens.Add(new(kind.Value, source[start..(start + length)], new SourceSpan(start, length)));
            i += length;
        }
        tokens.Add(new(TokenKind.End, string.Empty, new SourceSpan(source.Length, 0)));
        return tokens;
    }
}
