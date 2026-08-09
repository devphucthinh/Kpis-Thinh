namespace Kpi.Domain.Formula;

/// <summary>Closed lexical token set for the formula language.</summary>
public enum TokenKind
{
    End,
    Identifier,
    Number,
    True,
    False,
    Plus,
    Minus,
    Star,
    Slash,
    Percent,
    LeftParen,
    RightParen,
    Comma,
    Equal,
    NotEqual,
    Less,
    LessOrEqual,
    Greater,
    GreaterOrEqual,
    And,
    Or,
    Not,
    Mod
}

/// <summary>A token and exact source location.</summary>
public sealed record Token(TokenKind Kind, string Text, SourceSpan Span, decimal? Number = null);
