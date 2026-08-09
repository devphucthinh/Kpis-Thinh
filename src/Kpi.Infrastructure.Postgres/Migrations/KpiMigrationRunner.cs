using System.Data.Common;
using Kpi.Infrastructure.Postgres.Configuration;
using Kpi.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kpi.Infrastructure.Postgres.Migrations;

/// <summary>Applies reviewed, additive migrations only to declared local/test databases.</summary>
public sealed class KpiMigrationRunner(KpiDbContext context)
{
    public async Task ApplyAsync(PostgresOptions options, CancellationToken cancellationToken = default)
    {
        var database = context.Database.GetDbConnection().Database;
        if (!string.Equals(database, options.DatabaseName, StringComparison.Ordinal) && !string.Equals(database, options.TestDatabaseName, StringComparison.Ordinal))
            throw new InvalidOperationException("Configured database is outside the declared KPI local/test targets.");

        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await ExecuteAsync(connection, transaction,
                "CREATE TABLE IF NOT EXISTS kpi_schema_migrations (id text PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now());",
                cancellationToken);

            var applied = new HashSet<string>(StringComparer.Ordinal);
            await using (var read = connection.CreateCommand())
            {
                read.Transaction = transaction;
                read.CommandText = "SELECT id FROM kpi_schema_migrations ORDER BY id;";
                await using var reader = await read.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken)) applied.Add(reader.GetString(0));
            }

            foreach (var migration in KpiMigrationManifest.Scripts)
            {
                if (applied.Contains(migration.Id)) continue;
                await ExecuteAsync(connection, transaction, migration.Sql, cancellationToken);
                await ExecuteAsync(connection, transaction,
                    "INSERT INTO kpi_schema_migrations (id) VALUES (@id);",
                    cancellationToken,
                    ("id", migration.Id));
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    private static async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, string sql, CancellationToken cancellationToken, params (string Name, object Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name.StartsWith('@') ? name : $"@{name}";
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
