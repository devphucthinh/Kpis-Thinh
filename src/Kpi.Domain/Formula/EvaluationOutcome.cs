namespace Kpi.Domain.Formula;

/// <summary>Outcome of a safe formula evaluation.</summary>
public abstract record EvaluationOutcome
{
    public bool IsSuccess => this is EvaluationSuccess;
}

public sealed record EvaluationSuccess(FormulaValue Value) : EvaluationOutcome;
public sealed record EvaluationFailure(string Code, string Message, SourceSpan? Span = null) : EvaluationOutcome;
