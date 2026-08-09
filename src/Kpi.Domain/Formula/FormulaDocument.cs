using Kpi.Domain.Formula.Ast;

namespace Kpi.Domain.Formula;

/// <summary>Versioned formula snapshot returned by the compiler and persisted with history.</summary>
public sealed record FormulaDocument(string Source, FormulaNode Ast, int LanguageVersion = 1, int AstSchemaVersion = 1)
{
    public string Checksum => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Source))).ToLowerInvariant();
}

/// <summary>Compilation result with stable diagnostics.</summary>
public sealed record FormulaCompilation(FormulaDocument? Formula, IReadOnlyList<FormulaDiagnostic> Diagnostics)
{
    public bool IsSuccess => Formula is not null && Diagnostics.Count == 0;
}
