using Kpi.Infrastructure.Postgres.Migrations;
using Kpi.Infrastructure.Postgres.Configuration;
using Kpi.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Kpi.IntegrationTests.Migrations;

[Collection("PostgreSQL migration contract")]
public sealed class KpiMigrationRunnerTests
{
    private readonly MigrationDatabaseFixture fixture;

    public KpiMigrationRunnerTests(MigrationDatabaseFixture fixture) => this.fixture = fixture;

    [Fact(DisplayName = "FR-036 empty database applies ordered migrations with checksums")]
    public async Task Empty_test_database_applies_all_scripts_in_manifest_order_with_checksums()
    {
        fixture.RequireEnabled();
        await fixture.ResetAsync();
        await fixture.CreateRunner().ApplyAsync(fixture.Options, TestContext.Current.CancellationToken);

        await using var connection = fixture.CreateConnection();
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand("SELECT id, checksum FROM kpi_schema_migrations ORDER BY id;", connection);
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        var rows = new List<(string Id, string Checksum)>();
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
            rows.Add((reader.GetString(0), reader.GetString(1)));

        Assert.Equal(KpiMigrationManifest.Scripts.Count, rows.Count);
        Assert.Equal(KpiMigrationManifest.Scripts.Select(x => x.Id), rows.Select(x => x.Id));
        Assert.All(rows, row => Assert.False(string.IsNullOrWhiteSpace(row.Checksum)));
    }

    [Fact(DisplayName = "FR-036 reapplying the manifest is idempotent")]
    public async Task Reapplying_the_same_manifest_is_a_no_op()
    {
        fixture.RequireEnabled();
        await fixture.ResetAsync();
        var runner = fixture.CreateRunner();
        await runner.ApplyAsync(fixture.Options, TestContext.Current.CancellationToken);
        await runner.ApplyAsync(fixture.Options, TestContext.Current.CancellationToken);

        await using var connection = fixture.CreateConnection();
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var count = new NpgsqlCommand("SELECT count(*) FROM kpi_schema_migrations;", connection);
        Assert.Equal(KpiMigrationManifest.Scripts.Count, (long)(await count.ExecuteScalarAsync(TestContext.Current.CancellationToken) ?? -1L));
    }

    [Fact(DisplayName = "FR-036 changed migration checksums are rejected")]
    public async Task A_changed_applied_script_is_rejected_with_a_stable_error()
    {
        fixture.RequireEnabled();
        await fixture.ResetAsync();
        await fixture.CreateRunner().ApplyAsync(fixture.Options, TestContext.Current.CancellationToken);
        await using (var connection = fixture.CreateConnection())
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var tamper = new NpgsqlCommand("UPDATE kpi_schema_migrations SET checksum = 'tampered' WHERE id = @id;", connection);
            tamper.Parameters.AddWithValue("id", KpiMigrationManifest.Scripts[0].Id);
            await tamper.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.CreateRunner().ApplyAsync(fixture.Options, TestContext.Current.CancellationToken));
        Assert.Equal("MIGRATION_CHECKSUM_MISMATCH", error.Message);
    }

    [Fact(DisplayName = "FR-036 failed migration runs roll back ledger changes")]
    public async Task A_failed_migration_run_rolls_back_ledger_changes()
    {
        fixture.RequireEnabled();
        await fixture.ResetAsync();
        await fixture.CreateRunner().ApplyAsync(fixture.Options, TestContext.Current.CancellationToken);
        await using (var connection = fixture.CreateConnection())
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var removeLast = new NpgsqlCommand("DELETE FROM kpi_schema_migrations WHERE id = @id;", connection);
            removeLast.Parameters.AddWithValue("id", KpiMigrationManifest.Scripts[^1].Id);
            await removeLast.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
            await using var tamper = new NpgsqlCommand("UPDATE kpi_schema_migrations SET checksum = 'tampered' WHERE id = @id;", connection);
            tamper.Parameters.AddWithValue("id", KpiMigrationManifest.Scripts[0].Id);
            await tamper.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.CreateRunner().ApplyAsync(fixture.Options, TestContext.Current.CancellationToken));

        await using var verify = fixture.CreateConnection();
        await verify.OpenAsync(TestContext.Current.CancellationToken);
        await using var count = new NpgsqlCommand("SELECT count(*) FROM kpi_schema_migrations WHERE id = @id;", verify);
        count.Parameters.AddWithValue("id", KpiMigrationManifest.Scripts[^1].Id);
        Assert.Equal(0L, (long)(await count.ExecuteScalarAsync(TestContext.Current.CancellationToken) ?? -1L));
    }

    [Fact(DisplayName = "FR-001 migration targets outside the local test allow-list are rejected")]
    public async Task A_database_outside_the_local_test_allow_list_is_rejected_before_open()
    {
        fixture.RequireEnabled();
        await fixture.ResetAsync();
        var connection = new NpgsqlConnectionStringBuilder(fixture.ConnectionString) { Database = "postgres" }.ConnectionString;
        var options = new PostgresOptions
        {
            ConnectionString = connection,
            DatabaseName = fixture.Options.DatabaseName,
            TestDatabaseName = fixture.Options.TestDatabaseName,
            RuntimeRole = fixture.Options.RuntimeRole,
            MigrationRole = fixture.Options.MigrationRole
        };
        var dbOptions = new DbContextOptionsBuilder<KpiDbContext>()
            .UseNpgsql(connection)
            .Options;
        await using var context = new KpiDbContext(dbOptions);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => new KpiMigrationRunner(context).ApplyAsync(options, TestContext.Current.CancellationToken));
        Assert.Equal("MIGRATION_TARGET_NOT_ALLOWED", error.Message);
    }
}
