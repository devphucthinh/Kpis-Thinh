using Kpi.Infrastructure.Postgres.Persistence;
using Kpi.Infrastructure.Postgres.Migrations;
using Kpi.Infrastructure.Postgres.Stores;
using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Formula;
using Kpi.Domain.Evaluations;
using Kpi.Domain.Periods;
using Kpi.Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Kpi.IntegrationTests.Database;

public sealed class PersistenceModelTests
{
    [Fact(DisplayName = "FR-036 relational model preserves forward migration column names")]
    public void Relational_model_uses_the_forward_migration_column_names()
    {
        var options = new DbContextOptionsBuilder<KpiDbContext>()
            .UseNpgsql("Host=localhost;Database=kpi_lab;Username=ignored;Password=ignored")
            .Options;
        using var context = new KpiDbContext(options);

        AssertColumn<KpiDefinitionRow>(context, "kpi_definitions", nameof(KpiDefinitionRow.Id), "id");
        AssertColumn<KpiDefinitionRow>(context, "kpi_definitions", nameof(KpiDefinitionRow.OrganizationId), "organization_id");
        AssertColumn<KpiDefinitionRow>(context, "kpi_definitions", nameof(KpiDefinitionRow.RowVersion), "xmin");
        AssertColumn<KpiVersionRow>(context, "kpi_versions", nameof(KpiVersionRow.FormulaJson), "formula_json");
        AssertColumn<KpiVersionRow>(context, "kpi_versions", nameof(KpiVersionRow.DeclaredResultType), "declared_result_type");
        AssertColumn<KpiEvaluationRow>(context, "kpi_evaluations", nameof(KpiEvaluationRow.FormulaJson), "formula_snapshot_json");
        AssertColumn<KpiEvaluationRow>(context, "kpi_evaluations", nameof(KpiEvaluationRow.IsCurrent), "is_current_success");
        AssertColumn<AuditRecordRow>(context, "audit_records", nameof(AuditRecordRow.EntityType), "entity_type");
    }

    [Fact(DisplayName = "FR-036 product migration manifest is ordered and complete")]
    public void Product_migration_manifest_is_forward_only_and_has_sql_for_every_slice()
    {
        Assert.Equal(11, KpiMigrationManifest.ProductMigrations.Count);
        Assert.Equal(KpiMigrationManifest.ProductMigrations.Count, KpiMigrationManifest.Scripts.Count);
        Assert.All(KpiMigrationManifest.Scripts, migration =>
        {
            Assert.False(string.IsNullOrWhiteSpace(migration.Id));
            Assert.False(string.IsNullOrWhiteSpace(migration.Sql));
        });
        Assert.Contains("audit_records", KpiMigrationManifest.Scripts[0].Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GRANT SELECT, INSERT, UPDATE, DELETE", KpiMigrationManifest.Scripts[6].Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("REVOKE UPDATE, DELETE, TRUNCATE ON audit_records", KpiMigrationManifest.Scripts[6].Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("latest_effective_revision", KpiMigrationManifest.Scripts[7].Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("selections_json", KpiMigrationManifest.Scripts[7].Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("revisions_json", KpiMigrationManifest.Scripts[7].Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("kpi_period_activations", KpiMigrationManifest.Scripts[3].Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("kpi_evaluations", KpiMigrationManifest.Scripts[5].Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("organization_baselines", KpiMigrationManifest.Scripts[8].Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("authorization_evidence_json", KpiMigrationManifest.Scripts[9].Sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("role_assignments", KpiMigrationManifest.Scripts[10].Sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "FR-033 formula and evaluation snapshots reload without data loss")]
    public async Task Formula_and_evaluation_snapshots_are_reloadable()
    {
        var options = new DbContextOptionsBuilder<KpiDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var definition = new KpiDefinitionRow { Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), Code = "REVENUE", Name = "Revenue", Description = "Demo" };
        await using (var context = new KpiDbContext(options)) { context.Definitions.Add(definition); context.Versions.Add(new KpiVersionRow { Id = Guid.NewGuid(), DefinitionId = definition.Id, VersionNumber = 1, FormulaJson = "{\"source\":\"revenue / target\",\"ast\":{}}" }); await context.SaveChangesAsync(TestContext.Current.CancellationToken); }
        await using (var context = new KpiDbContext(options)) { var loaded = await context.Versions.SingleAsync(TestContext.Current.CancellationToken); Assert.Contains("revenue / target", loaded.FormulaJson, StringComparison.Ordinal); }
    }

    [Fact(DisplayName = "FR-033 definition and version snapshots round-trip through persistence")]
    public void Definition_and_version_round_trip_through_persistence_port()
    {
        var options = new DbContextOptionsBuilder<KpiDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var actor = ActorContext.Demo("creator");
        var original = Kpi.Domain.Kpis.KpiDefinition.Create(actor.OrganizationId, "ROUND_TRIP", "Round trip", "Persistence", actor.ActorId);
        original.CreateVersion("v1", "First", "ROUND(revenue, 2)", new[] { FormulaVariableDefinition.Create("revenue", "Revenue", FormulaValueType.Decimal, displayOrder: 0, description: "Input") }, FormulaResultType.Decimal, "Initial");
        using (var context = new KpiDbContext(options)) new PostgresKpiDefinitionStore(context).Save(original);
        using (var context = new KpiDbContext(options))
        {
            var loaded = Assert.Single(new PostgresKpiDefinitionStore(context).LoadAll(actor.OrganizationId));
            var version = Assert.Single(loaded.Versions);
            Assert.Equal(original.Id, loaded.Id);
            Assert.Equal(original.Code.Value, loaded.Code.Value);
            Assert.Equal(original.OwnerId, loaded.OwnerId);
            Assert.Equal(original.Versions[0].Formula.Source, version.Formula.Source);
            Assert.Equal(original.Versions[0].Variables[0].Code, version.Variables[0].Code);
            Assert.Equal(original.Versions[0].Variables[0].DisplayOrder, version.Variables[0].DisplayOrder);
        }
    }

    [Fact(DisplayName = "FR-033 governed snapshots expose period evaluation and audit state")]
    public void Governed_snapshot_model_exposes_period_activation_amendment_evaluation_and_audit_json()
    {
        var options = new DbContextOptionsBuilder<KpiDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var context = new KpiDbContext(options);
        context.Periods.Add(new KpiPeriodRow { Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), Code = "P1", Name = "Period", Description = "Snapshot", Cadence = "Monthly", StartsAt = DateTimeOffset.UtcNow, EndsAt = DateTimeOffset.UtcNow.AddDays(1), PlannerId = Guid.NewGuid(), Status = "Draft", SelectionsJson = "{}", RevisionsJson = "[]" });
        context.PeriodActivations.Add(new KpiPeriodActivationRow { Id = Guid.NewGuid(), PeriodId = Guid.NewGuid(), DefinitionId = Guid.NewGuid(), VersionId = Guid.NewGuid(), EffectiveRevisionNumber = 0, ActivatedAt = DateTimeOffset.UtcNow });
        context.PeriodAmendments.Add(new KpiPeriodAmendmentRow { Id = Guid.NewGuid(), PeriodId = Guid.NewGuid(), RevisionNumber = 1, BaseRevisionNumber = 0, ProposedStartsAt = DateTimeOffset.UtcNow, ProposedEndsAt = DateTimeOffset.UtcNow.AddDays(1), ProposedSelectionsJson = "{}", Reason = "test", ProposedBy = Guid.NewGuid(), ProposedAt = DateTimeOffset.UtcNow, Status = "InReview" });
        context.Evaluations.Add(new KpiEvaluationRow { Id = Guid.NewGuid(), ActivationId = Guid.NewGuid(), VersionId = Guid.NewGuid(), FormulaJson = "{\"source\":\"1\"}", InputsJson = "{}", OutcomeJson = "{}", EvaluatorActorId = Guid.NewGuid(), EvaluatedAt = DateTimeOffset.UtcNow });
        context.AuditRecords.Add(new AuditRecordRow { Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), ActorId = Guid.NewGuid(), EntityType = "KPI_PERIOD", EntityId = Guid.NewGuid(), EventType = "PeriodChanged", OccurredAt = DateTimeOffset.UtcNow, CorrelationId = "test", Reason = "reason", SummaryJson = "{}" });
        context.SaveChanges();
        Assert.Equal(1, context.Periods.Count());
        Assert.Equal(1, context.PeriodActivations.Count());
        Assert.Equal(1, context.PeriodAmendments.Count());
        Assert.Equal(1, context.Evaluations.Count());
        Assert.Equal(1, context.AuditRecords.Count());
    }

    [Fact(DisplayName = "FR-033 governed store serializes formula input outcome and audit evidence")]
    public void Governed_store_serializes_exact_formula_inputs_outcome_and_audit_snapshots()
    {
        var options = new DbContextOptionsBuilder<KpiDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var organization = Guid.NewGuid(); var actor = Guid.NewGuid(); var periodId = Guid.NewGuid(); var activationId = Guid.NewGuid(); var definitionId = Guid.NewGuid(); var versionId = Guid.NewGuid();
        var formula = FormulaEngine.Compile("revenue * 2", [FormulaVariableDefinition.Create("revenue", "Revenue", FormulaValueType.Decimal)], FormulaResultType.Decimal).Formula!;
        var evaluation = new KpiEvaluation(Guid.NewGuid(), definitionId, versionId, DateTimeOffset.UtcNow, new Dictionary<string, FormulaValue> { ["revenue"] = FormulaValue.Decimal(12.50m) }, new EvaluationSuccess(FormulaValue.Decimal(25m)), ActivationId: activationId, FormulaSnapshot: formula, EvaluatorActorId: actor);
        var audit = AuditRecord.Create(organization, actor, "KPI_EVALUATION", evaluation.Id, AuditEventType.Evaluated, evaluation.EvaluatedAt, "corr", summary: "official");
        using (var context = new KpiDbContext(options))
        {
            var store = new PostgresGovernedStore(context);
            store.SaveEvaluation(organization, evaluation);
            store.SaveAudit(audit);
        }
        using (var context = new KpiDbContext(options))
        {
            var row = Assert.Single(context.Evaluations);
            Assert.Contains("revenue * 2", row.FormulaJson, StringComparison.Ordinal);
            Assert.Contains("12.50", row.InputsJson, StringComparison.Ordinal);
            Assert.Contains("25", row.OutcomeJson, StringComparison.Ordinal);
            Assert.Equal(activationId, row.ActivationId);
            Assert.Equal("official", context.AuditRecords.Single().SummaryJson.Contains("official", StringComparison.Ordinal) ? "official" : null);
        }
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
