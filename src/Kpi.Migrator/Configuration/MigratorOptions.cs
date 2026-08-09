using Kpi.Infrastructure.Postgres.Configuration;
using Microsoft.Extensions.Configuration;

namespace Kpi.Migrator.Configuration;

/// <summary>Non-secret migrator settings with a separately supplied migration connection.</summary>
public sealed record MigratorOptions(
    string MigrationConnectionString,
    string DatabaseName,
    string TestDatabaseName,
    string RuntimeRole,
    string MigrationRole)
{
    public static MigratorOptions FromConfiguration(IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("KpiMigration");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("MIGRATION_CONFIGURATION_MISSING");

        var section = configuration.GetSection("Kpi");
        return new MigratorOptions(
            connection,
            section["DatabaseName"] ?? "kpi_lab",
            section["TestDatabaseName"] ?? "kpi_lab_test",
            section["RuntimeRole"] ?? "kpi_runtime",
            section["MigrationRole"] ?? "kpi_migrator");
    }

    public PostgresOptions ToPostgresOptions() => new()
    {
        ConnectionString = MigrationConnectionString,
        DatabaseName = DatabaseName,
        TestDatabaseName = TestDatabaseName,
        RuntimeRole = RuntimeRole,
        MigrationRole = MigrationRole
    };
}
