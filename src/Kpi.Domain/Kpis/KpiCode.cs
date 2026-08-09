using System.Text.RegularExpressions;

namespace Kpi.Domain.Kpis;

/// <summary>Immutable organization-scoped KPI identifier.</summary>
public readonly record struct KpiCode
{
    private static readonly Regex Pattern = new("^[A-Z][A-Z0-9_]*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private KpiCode(string value) => Value = value;
    public string Value { get; }
    public static KpiCode Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !Pattern.IsMatch(value.Trim())) throw new ArgumentException("KPI code must be uppercase snake case.", nameof(value));
        return new(value.Trim());
    }
    public override string ToString() => Value;
}
