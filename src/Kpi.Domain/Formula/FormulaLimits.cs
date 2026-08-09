namespace Kpi.Domain.Formula;

/// <summary>Safety bounds for compilation and evaluation.</summary>
public static class FormulaLimits
{
    public const int MaxVariables = 100;
    public const int MaxSourceCharacters = 10_000;
    public const int MaxAstDepth = 32;
    public const int MaxEvaluatedNodes = 10_000;
    public const int MaxMilliseconds = 500;
    public const int MaxScale = 10;
}
