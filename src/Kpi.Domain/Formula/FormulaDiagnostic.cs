namespace Kpi.Domain.Formula;

/// <summary>Stable, user-facing formula validation diagnostic.</summary>
public sealed record FormulaDiagnostic(string Code, string Message, SourceSpan Span)
{
    public override string ToString() => $"{Code}: {Message} ({Span.Start},{Span.Length})";
}
