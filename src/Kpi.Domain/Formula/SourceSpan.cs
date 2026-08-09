namespace Kpi.Domain.Formula;

/// <summary>Zero-based source location for diagnostics and AST nodes.</summary>
public readonly record struct SourceSpan(int Start, int Length)
{
    public int End => Start + Length;
}
