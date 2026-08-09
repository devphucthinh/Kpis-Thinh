namespace Kpi.Domain.Periods;

/// <summary>Half-open period interval in the configured Gregorian business timezone.</summary>
public readonly record struct PeriodInterval(DateTimeOffset StartsAt, DateTimeOffset EndsAt)
{
    public static PeriodInterval Create(DateTimeOffset startsAt, DateTimeOffset endsAt) => endsAt > startsAt ? new(startsAt, endsAt) : throw new ArgumentException("Period end must be after start.");
    public bool Contains(DateTimeOffset instant) => instant >= StartsAt && instant < EndsAt;
}
