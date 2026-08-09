namespace Kpi.Infrastructure.Postgres.Configuration;

/// <summary>Non-secret PostgreSQL settings supplied by environment or user secrets.</summary>
public sealed class PostgresOptions
{
    public string ConnectionString { get; init; } = string.Empty;
    public string DatabaseName { get; init; } = "kpi_lab";
    public string TestDatabaseName { get; init; } = "kpi_lab_test";
    public string RuntimeRole { get; init; } = "kpi_runtime";
    public string MigrationRole { get; init; } = "kpi_migrator";
}
