namespace Kpi.Infrastructure.Postgres.Persistence;

/// <summary>Evaluation persistence seam for immutable attempt storage.</summary>
public sealed class PostgresEvaluationStore(KpiDbContext context)
{
    public KpiDbContext Context { get; } = context;
}
