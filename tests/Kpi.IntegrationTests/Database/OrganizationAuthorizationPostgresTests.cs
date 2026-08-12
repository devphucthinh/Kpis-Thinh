using Kpi.Infrastructure.Postgres.Migrations;
using Kpi.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Kpi.IntegrationTests.Migrations;

[Collection("PostgreSQL migration contract")]
public sealed class OrganizationAuthorizationPostgresTests(MigrationDatabaseFixture fixture)
{
    [Fact]
    public async Task Cross_organization_assignment_is_rejected_by_composite_foreign_keys()
    {
        await PrepareAsync();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();
        var unitA = Guid.NewGuid();
        var positionA = Guid.NewGuid();
        var employeeB = Guid.NewGuid();

        await using var connection = fixture.CreateConnection();
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await ExecuteAsync(connection, "INSERT INTO organizations (id, code, name) VALUES (@id, @code, 'A'), (@id2, @code2, 'B');",
            ("id", organizationA), ("code", $"ORG-{organizationA:N}"), ("id2", organizationB), ("code2", $"ORG-{organizationB:N}"));
        await ExecuteAsync(connection, "INSERT INTO organization_units (id, organization_id, code, name) VALUES (@id, @org, 'UNIT-A', 'A');", ("id", unitA), ("org", organizationA));
        await ExecuteAsync(connection, "INSERT INTO organization_positions (id, organization_id, code, name, organization_unit_id) VALUES (@id, @org, 'POS-A', 'A', @unit);", ("id", positionA), ("org", organizationA), ("unit", unitA));
        await ExecuteAsync(connection, "INSERT INTO organization_employees (id, organization_id, employee_number, display_name, employment_from) VALUES (@id, @org, 'EMP-B', 'B', now());", ("id", employeeB), ("org", organizationB));

        var error = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection,
            "INSERT INTO organization_position_assignments (id, organization_id, employee_id, position_id, effective_from) VALUES (@id, @org, @employee, @position, now());",
            ("id", Guid.NewGuid()), ("org", organizationA), ("employee", employeeB), ("position", positionA)));
        Assert.Equal("23503", error.SqlState);
    }

    [Fact]
    public async Task Approved_baseline_and_applicability_segment_are_append_only()
    {
        await PrepareAsync();
        var organization = Guid.NewGuid();
        var baseline = Guid.NewGuid();
        var segment = Guid.NewGuid();
        await using var connection = fixture.CreateConnection();
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await ExecuteAsync(connection, "INSERT INTO organizations (id, code, name) VALUES (@id, @code, 'A');", ("id", organization), ("code", $"ORG-{organization:N}"));
        await ExecuteAsync(connection, "INSERT INTO organization_baselines (id, organization_id, structure_revision, effective_from, status, content_hash) VALUES (@id, @org, 1, now(), 'Approved', 'hash');", ("id", baseline), ("org", organization));
        await ExecuteAsync(connection, "INSERT INTO organization_baseline_applicability_segments (id, organization_id, baseline_id, effective_from) VALUES (@id, @org, @baseline, now());", ("id", segment), ("org", organization), ("baseline", baseline));

        var updateError = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, "UPDATE organization_baselines SET status = 'Retired' WHERE id = @id;", ("id", baseline)));
        Assert.Contains("append-only", updateError.MessageText, StringComparison.OrdinalIgnoreCase);
        var deleteError = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, "DELETE FROM organization_baseline_applicability_segments WHERE id = @id;", ("id", segment)));
        Assert.Contains("append-only", deleteError.MessageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Organization_xmin_is_used_as_an_optimistic_concurrency_token()
    {
        await PrepareAsync();
        var organization = Guid.NewGuid();
        await using (var connection = fixture.CreateConnection())
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await ExecuteAsync(connection, "INSERT INTO organizations (id, code, name) VALUES (@id, @code, 'A');", ("id", organization), ("code", $"ORG-{organization:N}"));
        }

        var options = new DbContextOptionsBuilder<KpiDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        await using var first = new KpiDbContext(options);
        await using var second = new KpiDbContext(options);
        var firstRow = await first.Organizations.SingleAsync(row => row.Id == organization, TestContext.Current.CancellationToken);
        var secondRow = await second.Organizations.SingleAsync(row => row.Id == organization, TestContext.Current.CancellationToken);
        firstRow.Name = "first";
        await first.SaveChangesAsync(TestContext.Current.CancellationToken);
        secondRow.Name = "second";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync(TestContext.Current.CancellationToken));
    }

    private async Task PrepareAsync()
    {
        fixture.RequireEnabled();
        await fixture.ResetAsync();
        await fixture.CreateRunner().ApplyAsync(fixture.Options, TestContext.Current.CancellationToken);
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
