namespace Kpi.Domain.Formula;

/// <summary>Closed set of values accepted by the KPI formula language.</summary>
public abstract record FormulaValue
{
    public static DecimalFormulaValue Decimal(decimal value) => new(value);
    public static BooleanFormulaValue Boolean(bool value) => new(value);
    public static NullFormulaValue Null { get; } = new();
}

/// <summary>Decimal KPI value using System.Decimal, never binary floating point.</summary>
public sealed record DecimalFormulaValue(decimal Value) : FormulaValue;

/// <summary>Boolean KPI value.</summary>
public sealed record BooleanFormulaValue(bool Value) : FormulaValue;

/// <summary>Explicit null value used only for non-computable outcomes.</summary>
public sealed record NullFormulaValue : FormulaValue;
