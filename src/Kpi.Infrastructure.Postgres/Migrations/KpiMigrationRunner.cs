using Kpi.Infrastructure.Postgres.Configuration;
using Kpi.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kpi.Infrastructure.Postgres.Migrations;

/// <summary>Applies only safe local/test schema setup; production deployment uses reviewed scripts.</summary>
public sealed class KpiMigrationRunner(KpiDbContext context)
{
    public async Task ApplyAsync(PostgresOptions options, CancellationToken cancellationToken = default)
    {
        var database = context.Database.GetDbConnection().Database;
        if (!string.Equals(database, options.DatabaseName, StringComparison.Ordinal) && !string.Equals(database, options.TestDatabaseName, StringComparison.Ordinal))
            throw new InvalidOperationException("Configured database is outside the declared KPI local/test targets.");
        await context.Database.EnsureCreatedAsync(cancellationToken);
        await context.Database.ExecuteSqlRawAsync("CREATE OR REPLACE FUNCTION reject_audit_mutation() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'audit_records are append-only'; END; $$;", cancellationToken);
        await context.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS audit_records_append_only ON audit_records; CREATE TRIGGER audit_records_append_only BEFORE UPDATE OR DELETE ON audit_records FOR EACH ROW EXECUTE FUNCTION reject_audit_mutation();", cancellationToken);
    }
}
