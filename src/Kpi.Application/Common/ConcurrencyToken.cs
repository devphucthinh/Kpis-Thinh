namespace Kpi.Application.Common;

/// <summary>Opaque optimistic-concurrency value accepted by mutable commands.</summary>
public readonly record struct ConcurrencyToken(string Value)
{
    public static ConcurrencyToken Empty => new("0");
}
