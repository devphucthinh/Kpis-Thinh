namespace Kpi.Infrastructure.Postgres.Persistence;

/// <summary>Period persistence seam reserved for the additive Period migration.</summary>
public sealed class PostgresPeriodStore(KpiDbContext context)
{
    public KpiDbContext Context { get; } = context;
}
