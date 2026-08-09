using Microsoft.EntityFrameworkCore;

namespace Kpi.Infrastructure.Postgres.Persistence;

/// <summary>Explicit transaction boundary for governed PostgreSQL commands.</summary>
public sealed class PostgresUnitOfWork(KpiDbContext context)
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => context.SaveChangesAsync(cancellationToken);
}
