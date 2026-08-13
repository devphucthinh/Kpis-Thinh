namespace Kpi.Domain.Organizations;

/// <summary>Half-open UTC interval used by all effective-dated organization facts.</summary>
public sealed record EffectiveInterval
{
    public EffectiveInterval(DateTimeOffset from, DateTimeOffset? to = null)
    {
        From = from.ToUniversalTime();
        To = to?.ToUniversalTime();

        if (To is not null && To <= From)
            throw new ArgumentException("Effective interval end must be after its start.", nameof(to));
    }

    public DateTimeOffset From { get; }
    public DateTimeOffset? To { get; }

    public bool Contains(DateTimeOffset instant)
    {
        var utc = instant.ToUniversalTime();
        return utc >= From && (To is null || utc < To.Value);
    }

    public bool Overlaps(EffectiveInterval other) =>
        other is not null && From < (other.To ?? DateTimeOffset.MaxValue) &&
        other.From < (To ?? DateTimeOffset.MaxValue);
}
