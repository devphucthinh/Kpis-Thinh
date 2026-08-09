using Kpi.Infrastructure.Postgres.Configuration;
using Kpi.Infrastructure.Postgres.Migrations;
using Kpi.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Kpi.IntegrationTests.Migrations;

/// <summary>
/// Opt-in PostgreSQL fixture for the migration contract. The default harness never
/// opens a database connection; enabling the profile requires the declared local
/// test database and an explicit migration connection.
/// </summary>
public sealed class MigrationDatabaseFixture
{
    public bool Enabled { get; }
    public string? ConnectionString { get; }
    public PostgresOptions Options { get; }

    public MigrationDatabaseFixture()
    {
        Enabled = string.Equals(Environment.GetEnvironmentVariable("KPI_POSTGRES_TESTS"), "1", StringComparison.Ordinal);
        if (!Enabled)
        {
            Options = new PostgresOptions();
            return;
        }

        ConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__KpiMigration")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings:KpiMigration");
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new InvalidOperationException("POSTGRES_TEST_CONFIGURATION_MISSING");

        var configuration = new PostgresOptions
        {
            ConnectionString = ConnectionString,
            DatabaseName = Environment.GetEnvironmentVariable("Kpi__DatabaseName") ?? "kpi_lab",
            TestDatabaseName = Environment.GetEnvironmentVariable("Kpi__TestDatabaseName") ?? "kpi_lab_test",
            RuntimeRole = Environment.GetEnvironmentVariable("Kpi__RuntimeRole") ?? "kpi_runtime",
            MigrationRole = Environment.GetEnvironmentVariable("Kpi__MigrationRole") ?? "kpi_migrator"
        };
        var builder = new NpgsqlConnectionStringBuilder(ConnectionString);
        if (!string.Equals(builder.Database, configuration.TestDatabaseName, StringComparison.Ordinal))
            throw new InvalidOperationException("POSTGRES_TEST_DATABASE_NOT_ALLOWED");

        Options = configuration;
    }

    public void RequireEnabled()
    {
        if (!Enabled)
            Assert.Skip("PostgreSQL migration tests skipped; set KPI_POSTGRES_TESTS=1 with ConnectionStrings__KpiMigration targeting kpi_lab_test.");
    }

    public KpiMigrationRunner CreateRunner()
    {
        RequireEnabled();
        var dbOptions = new DbContextOptionsBuilder<KpiDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new KpiMigrationRunner(new KpiDbContext(dbOptions));
    }

    public NpgsqlConnection CreateConnection()
    {
        RequireEnabled();
        return new NpgsqlConnection(ConnectionString);
    }

    public async Task ResetAsync()
    {
        RequireEnabled();
        await using var connection = CreateConnection();
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand("""
            DROP TABLE IF EXISTS kpi_evaluations, kpi_period_amendments,
                kpi_period_activations, kpi_periods, audit_records, kpi_versions,
                kpi_definitions, actors, organizations, kpi_schema_migrations CASCADE;
            """, connection);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
