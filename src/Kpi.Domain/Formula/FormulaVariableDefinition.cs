using System.Text.RegularExpressions;

namespace Kpi.Domain.Formula;

/// <summary>An ordered, typed input declared by a KPI Version.</summary>
public sealed record FormulaVariableDefinition
{
    private static readonly Regex CodePattern = new("^[a-z][a-z0-9_]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private FormulaVariableDefinition(string code, string displayName, FormulaValueType type, bool required, FormulaValue? defaultValue, int displayOrder, string description)
    {
        Code = code;
        DisplayName = displayName;
        Type = type;
        Required = required;
        DefaultValue = defaultValue;
        DisplayOrder = displayOrder;
        Description = description;
    }

    public string Code { get; }
    public string DisplayName { get; }
    public FormulaValueType Type { get; }
    public bool Required { get; }
    public FormulaValue? DefaultValue { get; }
    public int DisplayOrder { get; }
    public string Description { get; }

    public static FormulaVariableDefinition Create(string code, string displayName, FormulaValueType type, bool required = true, FormulaValue? defaultValue = null, int displayOrder = 0, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code) || !CodePattern.IsMatch(code))
            throw new ArgumentException("Variable code must use lower snake case.", nameof(code));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Variable display name is required.", nameof(displayName));
        if (displayOrder < 0)
            throw new ArgumentOutOfRangeException(nameof(displayOrder));
        if (defaultValue is not null && !Matches(type, defaultValue))
            throw new ArgumentException("Variable default has the wrong type.", nameof(defaultValue));
        if (required && defaultValue is null)
            return new(code, displayName.Trim(), type, true, null, displayOrder, description?.Trim() ?? string.Empty);
        return new(code, displayName.Trim(), type, required, defaultValue, displayOrder, description?.Trim() ?? string.Empty);
    }

    internal static bool Matches(FormulaValueType type, FormulaValue value) =>
        (type == FormulaValueType.Decimal && value is DecimalFormulaValue) ||
        (type == FormulaValueType.Boolean && value is BooleanFormulaValue);
}
