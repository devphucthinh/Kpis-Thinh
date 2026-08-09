using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Formula;
using Kpi.Domain.Formula.Serialization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Kpi.Web.Api.V1;

[ApiController]
[Route("api/v1/formulas")]
public sealed class FormulaController(KpiOperations operations, ICurrentActor actor) : ControllerBase
{
    [HttpPost("validate")]
    public IActionResult Validate([FromBody] FormulaRequest request)
    {
        var variables = request.Variables.Select((v, i) => FormulaVariableDefinition.Create(v.Code, v.DisplayName ?? v.Code, ParseType(v.Type), v.Required, ParseValue(v.DefaultValue, v.Type), i, v.Description)).ToArray();
        var result = operations.Validate(actor.Current, request.Source, variables, ParseResultType(request.DeclaredResultType));
        var compilation = result.Value!;
        return Ok(new { valid = compilation.IsSuccess, diagnostics = compilation.Diagnostics, formula = compilation.Formula is null ? null : new { source = compilation.Formula.Source, ast = JsonDocument.Parse(FormulaDocumentSerializer.Serialize(compilation.Formula)).RootElement.GetProperty("ast"), formulaLanguageVersion = compilation.Formula.LanguageVersion, astSchemaVersion = compilation.Formula.AstSchemaVersion } });
    }

    [HttpPost("test-run")]
    public IActionResult TestRun([FromBody] FormulaTestRequest request)
    {
        var variables = request.Variables.Select((v, i) => FormulaVariableDefinition.Create(v.Code, v.DisplayName ?? v.Code, ParseType(v.Type), v.Required, ParseValue(v.DefaultValue, v.Type), i, v.Description)).ToArray();
        var compiled = FormulaEngine.Compile(request.Source, variables, ParseResultType(request.DeclaredResultType));
        if (!compiled.IsSuccess) return UnprocessableEntity(new { code = "FORMULA_INVALID", diagnostics = compiled.Diagnostics });
        var inputs = request.Inputs.ToDictionary(x => x.Key, x => ParseFormulaValue(x.Value));
        var outcome = FormulaEngine.Evaluate(compiled.Formula!, variables, inputs);
        return Ok(new { persisted = false, outcome });
    }

    private static FormulaValueType ParseType(string? type) => string.Equals(type, "Boolean", StringComparison.OrdinalIgnoreCase) ? FormulaValueType.Boolean : FormulaValueType.Decimal;
    private static FormulaResultType ParseResultType(string? type) => string.Equals(type, "Boolean", StringComparison.OrdinalIgnoreCase) ? FormulaResultType.Boolean : FormulaResultType.Decimal;
    private static FormulaValue? ParseValue(JsonElement? value, string? type) => value is null ? null : ParseFormulaValue(value.Value);
    private static FormulaValue ParseFormulaValue(JsonElement value) => value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False ? FormulaValue.Boolean(value.GetBoolean()) : FormulaValue.Decimal(value.ValueKind == JsonValueKind.String ? decimal.Parse(value.GetString()!, System.Globalization.CultureInfo.InvariantCulture) : value.GetDecimal());
}

public sealed record FormulaRequest(string Source, IReadOnlyList<FormulaVariableRequest> Variables, string DeclaredResultType);
public sealed record FormulaTestRequest(string Source, IReadOnlyList<FormulaVariableRequest> Variables, string DeclaredResultType, IReadOnlyDictionary<string, JsonElement> Inputs);
public sealed record FormulaVariableRequest(string Code, string? DisplayName, string Type, bool Required, JsonElement? DefaultValue, string? Description);
