using Kpi.Domain.Formula;

namespace Kpi.Application.Formula;

/// <summary>Source-only validation command that generates the server AST.</summary>
public sealed class ValidateFormulaCommand(FormulaService formulaService)
{
    public FormulaCompilation Execute(string source, IReadOnlyList<FormulaVariableDefinition> variables, FormulaResultType resultType) => formulaService.Validate(source, variables, resultType);
}
