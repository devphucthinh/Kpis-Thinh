using System.Data.Common;
using Kpi.Infrastructure.Postgres.Configuration;
using Kpi.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kpi.Infrastructure.Postgres.Migrations;

/// <summary>Applies reviewed, additive migrations only to declared local/test databases.</summary>
public sealed class KpiMigrationRunner(KpiDbContext context)
{
    public async Task<MigrationApplyResult> ApplyAsync(PostgresOptions options, CancellationToken cancellationToken = default)
    {
        var connection = context.Database.GetDbConnection();
        var database = connection.Database;
        if (!string.Equals(database, options.DatabaseName, StringComparison.Ordinal) &&
            !string.Equals(database, options.TestDatabaseName, StringComparison.Ordinal))
            throw new InvalidOperationException("MIGRATION_TARGET_NOT_ALLOWED");

        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var appliedIds = new List<string>();
        var skippedIds = new List<string>();
        try
        {
            await ExecuteAsync(connection, transaction,
                "CREATE TABLE IF NOT EXISTS kpi_schema_migrations (id text PRIMARY KEY, checksum text NOT NULL, applied_at timestamptz NOT NULL DEFAULT now());",
                cancellationToken);
            await ExecuteAsync(connection, transaction,
                "ALTER TABLE kpi_schema_migrations ADD COLUMN IF NOT EXISTS checksum text;",
                cancellationToken);

            var applied = new Dictionary<string, string?>(StringComparer.Ordinal);
            await using (var read = connection.CreateCommand())
            {
                read.Transaction = transaction;
                read.CommandText = "SELECT id, checksum FROM kpi_schema_migrations ORDER BY id;";
                await using var reader = await read.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                    applied[reader.GetString(0)] = reader.IsDBNull(1) ? null : reader.GetString(1);
            }

            foreach (var migration in KpiMigrationManifest.Scripts)
            {
                if (applied.TryGetValue(migration.Id, out var checksum))
                {
                    if (!string.Equals(checksum, migration.Checksum, StringComparison.Ordinal))
                        throw new InvalidOperationException("MIGRATION_CHECKSUM_MISMATCH");
                    skippedIds.Add(migration.Id);
                    continue;
                }

                await ExecuteAsync(connection, transaction, migration.Sql, cancellationToken);
                await ExecuteAsync(connection, transaction,
                    "INSERT INTO kpi_schema_migrations (id, checksum) VALUES (@id, @checksum);",
                    cancellationToken,
                    ("id", migration.Id),
                    ("checksum", migration.Checksum));
                appliedIds.Add(migration.Id);
            }

            await transaction.CommitAsync(cancellationToken);
            return new MigrationApplyResult(database, appliedIds, skippedIds);
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

public sealed record MigrationApplyResult(
    string TargetDatabase,
    IReadOnlyList<string> AppliedIds,
    IReadOnlyList<string> SkippedIds);
