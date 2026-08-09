using System.Globalization;

namespace Kpi.Domain.Formula;

/// <summary>Deterministic Decimal normalization for business calculations.</summary>
public static class DecimalPolicy
{
    public static decimal Normalize(decimal value) => decimal.Round(value, FormulaLimits.MaxScale, MidpointRounding.AwayFromZero);
    public static bool TryParseInvariant(string text, out decimal value) => decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    public static bool IsValidScale(decimal value) => decimal.Round(value, FormulaLimits.MaxScale, MidpointRounding.AwayFromZero) == value;
}
