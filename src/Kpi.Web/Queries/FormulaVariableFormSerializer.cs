using System.Text.Json;
using System.Text.Json.Serialization;
using Kpi.Domain.Formula;
using Kpi.Web.ViewModels;

namespace Kpi.Web.Queries;

public static class FormulaVariableFormSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static IReadOnlyList<FormulaVariableInputVm> Deserialize(string? json, string? legacyText = null)
    {
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var rows = JsonSerializer.Deserialize<List<FormulaVariableInputVm>>(json, JsonOptions);
                if (rows is { Count: > 0 }) return rows.OrderBy(x => x.DisplayOrder).ToArray();
            }
            catch (JsonException)
            {
                // The controller reports malformed JSON as a validation error.
            }
        }

        return (legacyText ?? string.Empty)
            .Split(['\r', '\n', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select((code, index) => new FormulaVariableInputVm(code, code, FormulaValueType.Decimal, true, null, null, index))
            .ToArray();
    }

    public static string Serialize(IEnumerable<FormulaVariableInputVm> rows) => JsonSerializer.Serialize(rows.OrderBy(x => x.DisplayOrder), JsonOptions);
}
