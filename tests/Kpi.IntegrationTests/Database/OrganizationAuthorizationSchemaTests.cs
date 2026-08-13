using Kpi.Infrastructure.Postgres.Migrations;
using Kpi.Infrastructure.Postgres;
using Kpi.Infrastructure.Postgres.Persistence;
using Kpi.Web.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kpi.IntegrationTests.Database;

public sealed class OrganizationAuthorizationSchemaTests
{
    [Fact(DisplayName = "FR-001 FR-002 FR-013 FR-036 organization authorization model preserves scoped facts")]
    public void Organization_authorization_model_has_organization_scoped_heads_and_xmin_tokens()
    {
        var options = new DbContextOptionsBuilder<KpiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new KpiDbContext(options);

        Assert.NotNull(context.Model.FindEntityType("Kpi.Infrastructure.Postgres.Persistence.OrganizationRow"));
        Assert.NotNull(context.Model.FindEntityType("Kpi.Infrastructure.Postgres.Persistence.OrganizationUnitRow"));
        Assert.NotNull(context.Model.FindEntityType("Kpi.Infrastructure.Postgres.Persistence.OrganizationBaselineRow"));
        Assert.NotNull(context.Model.FindEntityType("Kpi.Infrastructure.Postgres.Persistence.BaselineApplicabilitySegmentRow"));
        Assert.Contains(context.Model.GetEntityTypes(), entity => entity.GetProperties().Any(property => property.Name == "RowVersion" && property.GetColumnName() == "xmin"));
        var organizationEntity = context.Model.FindEntityType(typeof(OrganizationRow));
        Assert.NotNull(organizationEntity);
        Assert.DoesNotContain(organizationEntity!.GetProperties(), property => property.Name == nameof(OrganizationScopedHeadRow.OrganizationId));
        var unitEntity = context.Model.FindEntityType("Kpi.Infrastructure.Postgres.Persistence.OrganizationUnitRow");
        Assert.NotNull(unitEntity);
        Assert.Contains(unitEntity!.GetForeignKeys(), foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(OrganizationRow));
        AssertColumn<OrganizationUnitRow>(context, "organization_units", nameof(OrganizationUnitRow.ParentUnitId), "parent_unit_id");
        AssertColumn<OrganizationPositionRow>(context, "organization_positions", nameof(OrganizationPositionRow.OrganizationUnitId), "organization_unit_id");
        AssertColumn<OrganizationPositionAssignmentRow>(context, "organization_position_assignments", nameof(OrganizationPositionAssignmentRow.EmployeeId), "employee_id");
        AssertColumn<OrganizationPositionAssignmentRow>(context, "organization_position_assignments", nameof(OrganizationPositionAssignmentRow.EffectiveFrom), "effective_from");
        AssertColumn<OrganizationPositionAssignmentRow>(context, "organization_position_assignments", nameof(OrganizationPositionAssignmentRow.EffectiveTo), "effective_to");
        var assignmentEntity = context.Model.FindEntityType(typeof(OrganizationPositionAssignmentRow));
        Assert.Contains(assignmentEntity!.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(OrganizationEmployeeRow) &&
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([nameof(OrganizationPositionAssignmentRow.OrganizationId), nameof(OrganizationPositionAssignmentRow.EmployeeId)]));
    }

    [Fact(DisplayName = "FR-001 FR-002 FR-013 FR-036 migration declares isolation and append-only protections")]
    public void Organization_authorization_migrations_declare_fk_isolation_and_append_only_protections()
    {
        var sql = string.Join("\n", KpiMigrationManifest.Scripts.Select(script => script.Sql));

        Assert.Contains("CREATE TABLE IF NOT EXISTS organizations", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("organization_id", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("REFERENCES organizations", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FOREIGN KEY (organization_id, employee_id) REFERENCES organization_employees(organization_id, id)", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FOREIGN KEY (organization_id, position_id) REFERENCES organization_positions(organization_id, id)", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("effective_from timestamptz", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("effective_interval tstzrange", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("organization_baseline_applicability_segments", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("organization_baselines_append_only", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EXCLUDE USING gist", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("REVOKE UPDATE, DELETE, TRUNCATE ON audit_records", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "FR-013 FR-033 FR-036 migration protects audit evidence and effective history")]
    public void Organization_authorization_migrations_declare_audit_evidence_and_effective_range_indexes()
    {
        var sql = string.Join("\n", KpiMigrationManifest.Scripts.Select(script => script.Sql));

        Assert.Contains("resource_revision", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("capability_id", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("authorization_evidence_json", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("organization_assignments_effective_gist_idx", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("organization_reporting_effective_gist_idx", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("baseline_segments_one_open_tail_uq", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("REVOKE UPDATE, DELETE, TRUNCATE ON organization_baselines", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "FR-001 runtime composition isolates KpiRuntime from KpiMigration")]
    public void Runtime_composition_uses_runtime_connection_and_ignores_migration_connection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:KpiRuntime"] = "Host=runtime;Database=kpi_lab;Username=runtime",
                ["ConnectionStrings:KpiMigration"] = "Host=migration;Database=kpi_lab;Username=migration"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddKpiPostgres(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<KpiDbContext>();
        Assert.Equal("Host=runtime;Database=kpi_lab;Username=runtime", context.Database.GetDbConnection().ConnectionString);
        Assert.DoesNotContain("migration", context.Database.GetDbConnection().ConnectionString, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "FR-001 migration-only composition does not register runtime context")]
    public void Migration_only_configuration_does_not_register_runtime_context()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:KpiMigration"] = "Host=migration;Database=kpi_lab;Username=migration"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddKpiPostgres(configuration);

        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(KpiDbContext));
    }

    [Fact(DisplayName = "FR-036 migration ledger remains forward-only and ordered")]
    public void Organization_authorization_migration_is_appended_in_ledger_order()
    {
        var ids = KpiMigrationManifest.ProductMigrations;
        Assert.Equal(ids.OrderBy(id => id, StringComparer.Ordinal), ids);
    }

    private static void AssertColumn<TRow>(KpiDbContext context, string tableName, string propertyName, string expectedColumn)
        where TRow : class
    {
        var entity = context.Model.FindEntityType(typeof(TRow));
        Assert.NotNull(entity);
        var storeObject = StoreObjectIdentifier.Table(tableName, null);
        var property = entity!.FindProperty(propertyName);
        Assert.NotNull(property);
        Assert.Equal(expectedColumn, property!.GetColumnName(storeObject));
    }
}
