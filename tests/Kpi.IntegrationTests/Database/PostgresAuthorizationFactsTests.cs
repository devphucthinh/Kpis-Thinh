using Kpi.Application.Authorization;
using Kpi.Infrastructure.Postgres.Authorization;
using Kpi.Infrastructure.Postgres.Migrations;
using Kpi.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;
using Kpi.IntegrationTests.Migrations;

namespace Kpi.IntegrationTests.Database;

[Collection("PostgreSQL migration contract")]
public sealed class PostgresAuthorizationFactsTests(MigrationDatabaseFixture fixture)
{
    [Fact(DisplayName = "FR-023 FR-024 FR-031 FR-049 current PostgreSQL role scope baseline and delegation facts are reloaded")]
    public async Task Reader_loads_current_role_capability_scope_baseline_and_delegation_facts()
    {
        fixture.RequireEnabled();
        await fixture.ResetAsync();
        await fixture.CreateRunner().ApplyAsync(fixture.Options, TestContext.Current.CancellationToken);
        var organizationId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        var delegateId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var baselineId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var roleVersionId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var delegationId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var capability = new KpiCapabilityId("organization.structure.view");

        await using var connection = fixture.CreateConnection();
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await ExecuteAsync(connection, "INSERT INTO organizations (id, code, name) VALUES (@id, @code, 'Auth Org');", ("id", organizationId), ("code", $"AUTH-{organizationId:N}"));
        await ExecuteAsync(connection, "INSERT INTO organization_units (id, organization_id, code, name) VALUES (@id, @org, 'UNIT-A', 'Unit A');", ("id", unitId), ("org", organizationId));
        await ExecuteAsync(connection, "INSERT INTO organization_employees (id, organization_id, employee_number, display_name, employment_from) VALUES (@id, @org, 'EMP-A', 'Employee A', @from), (@delegate, @org, 'EMP-D', 'Delegate', @from);", ("id", employeeId), ("delegate", delegateId), ("org", organizationId), ("from", now.AddDays(-1)));
        await ExecuteAsync(connection, "INSERT INTO organization_baselines (id, organization_id, structure_revision, effective_from, status, content_hash) VALUES (@id, @org, 1, @from, 'Approved', 'auth-hash');", ("id", baselineId), ("org", organizationId), ("from", now.AddDays(-1)));
        await ExecuteAsync(connection, "INSERT INTO organization_baseline_applicability_segments (id, organization_id, baseline_id, effective_from) VALUES (@id, @org, @baseline, @from);", ("id", Guid.NewGuid()), ("org", organizationId), ("baseline", baselineId), ("from", now.AddDays(-1)));
        await ExecuteAsync(connection, "INSERT INTO custom_kpi_roles (id, organization_id, name, status) VALUES (@id, @org, 'Structure Viewer', 'Active');", ("id", roleId), ("org", organizationId));
        await ExecuteAsync(connection, "INSERT INTO custom_kpi_role_versions (id, organization_id, role_id, version_number, status, created_by) VALUES (@id, @org, @role, 1, 'Active', @created);", ("id", roleVersionId), ("org", organizationId), ("role", roleId), ("created", employeeId));
        await ExecuteAsync(connection, "INSERT INTO custom_kpi_role_capabilities (organization_id, role_version_id, capability_id) VALUES (@org, @version, @capability);", ("org", organizationId), ("version", roleVersionId), ("capability", capability.Value));
        await ExecuteAsync(connection, "INSERT INTO role_assignments (id, organization_id, employee_id, role_version_id, scope_kind, scope_target_id, baseline_id, effective_from, status) VALUES (@id, @org, @employee, @version, 'UnitSubtree', @unit, @baseline, @from, 'Effective');", ("id", assignmentId), ("org", organizationId), ("employee", employeeId), ("version", roleVersionId), ("unit", unitId), ("baseline", baselineId), ("from", now.AddDays(-1)));
        await ExecuteAsync(connection, "INSERT INTO approval_delegations (id, organization_id, original_actor_id, delegate_actor_id, capability_id, scope_kind, scope_target_id, baseline_id, effective_from, status) VALUES (@id, @org, @original, @delegate, @capability, 'UnitSubtree', @unit, @baseline, @from, 'Active');", ("id", delegationId), ("org", organizationId), ("original", employeeId), ("delegate", delegateId), ("capability", capability.Value), ("unit", unitId), ("baseline", baselineId), ("from", now.AddDays(-1)));

        var options = new DbContextOptionsBuilder<KpiDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        await using var context = new KpiDbContext(options);
        var reader = new PostgresAuthorizationFactsReader(context);
        var actor = new ActorIdentity("delegate-reference", delegateId, organizationId);
        var resource = new AuthorizationResource(organizationId, "OrganizationUnit", unitId, 1, baselineId, [unitId]);
        var represented = new RepresentedAuthority(employeeId, delegationId);

        var facts = await reader.LoadAsync(actor, resource, now, represented, capability, TestContext.Current.CancellationToken);

        Assert.Contains(facts.Capabilities, item => item.Value == capability.Value);
        Assert.Contains(assignmentId, facts.AssignmentIds);
        Assert.True(facts.ScopeMatches);
        Assert.True(facts.BaselineApplicable);
        Assert.True(facts.AuthorityEffective);
        Assert.True(facts.DelegationValid);
        Assert.True(facts.DelegationScopeMatches);
        Assert.Equal(employeeId, facts.RepresentedAuthorityActorId);
        Assert.Equal(delegationId, facts.DelegationId);

        var service = new AuthorizationDecisionService(reader);
        var action = new AuthorizationActionContext(service);
        var first = await action.DecideAsync(actor, capability, resource, now, represented, TestContext.Current.CancellationToken);
        await ExecuteAsync(connection, "UPDATE custom_kpi_role_versions SET status = 'Retired' WHERE id = @id;", ("id", roleVersionId));
        var roleRetired = new AuthorizationActionContext(service);
        var retiredDecision = await roleRetired.DecideAsync(actor, capability, resource, now, represented, TestContext.Current.CancellationToken);
        await ExecuteAsync(connection, "UPDATE custom_kpi_role_versions SET status = 'Active' WHERE id = @id;", ("id", roleVersionId));
        await ExecuteAsync(connection, "UPDATE role_assignments SET status = 'Revoked' WHERE id = @id;", ("id", assignmentId));
        var cached = await action.DecideAsync(actor, capability, resource, now, represented, TestContext.Current.CancellationToken);
        var nextAction = new AuthorizationActionContext(service);
        var fresh = await nextAction.DecideAsync(actor, capability, resource, now, represented, TestContext.Current.CancellationToken);

        Assert.Equal(AuthorizationOutcome.Allow, first.Outcome);
        Assert.Equal(AuthorizationDecisionReason.AuthorityNotEffective, retiredDecision.ReasonCode);
        Assert.Equal(AuthorizationOutcome.Allow, cached.Outcome);
        Assert.Equal(AuthorizationDecisionReason.AuthorityNotEffective, fresh.ReasonCode);
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
