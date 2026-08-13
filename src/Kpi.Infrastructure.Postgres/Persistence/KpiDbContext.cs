using Microsoft.EntityFrameworkCore;
using Kpi.Infrastructure.Postgres.Persistence.Configurations;

namespace Kpi.Infrastructure.Postgres.Persistence;

/// <summary>Relational/queryable projection and immutable JSONB snapshots for KPI history.</summary>
public sealed class KpiDbContext(DbContextOptions<KpiDbContext> options) : DbContext(options)
{
    public DbSet<KpiDefinitionRow> Definitions => Set<KpiDefinitionRow>();
    public DbSet<KpiVersionRow> Versions => Set<KpiVersionRow>();
    public DbSet<KpiEvaluationRow> Evaluations => Set<KpiEvaluationRow>();
    public DbSet<KpiPeriodRow> Periods => Set<KpiPeriodRow>();
    public DbSet<KpiPeriodActivationRow> PeriodActivations => Set<KpiPeriodActivationRow>();
    public DbSet<KpiPeriodAmendmentRow> PeriodAmendments => Set<KpiPeriodAmendmentRow>();
    public DbSet<AuditRecordRow> AuditRecords => Set<AuditRecordRow>();
    public DbSet<OrganizationRow> Organizations => Set<OrganizationRow>();
    public DbSet<OrganizationUnitRow> OrganizationUnits => Set<OrganizationUnitRow>();
    public DbSet<OrganizationPositionRow> OrganizationPositions => Set<OrganizationPositionRow>();
    public DbSet<OrganizationEmployeeRow> OrganizationEmployees => Set<OrganizationEmployeeRow>();
    public DbSet<OrganizationPositionAssignmentRow> OrganizationPositionAssignments => Set<OrganizationPositionAssignmentRow>();
    public DbSet<OrganizationReportingRelationshipRow> OrganizationReportingRelationships => Set<OrganizationReportingRelationshipRow>();
    public DbSet<OrganizationBaselineRow> OrganizationBaselines => Set<OrganizationBaselineRow>();
    public DbSet<BaselineApplicabilitySegmentRow> BaselineApplicabilitySegments => Set<BaselineApplicabilitySegmentRow>();
    public DbSet<CustomKpiRoleRow> CustomKpiRoles => Set<CustomKpiRoleRow>();
    public DbSet<CustomKpiRoleVersionRow> CustomKpiRoleVersions => Set<CustomKpiRoleVersionRow>();
    public DbSet<CustomKpiRoleCapabilityRow> CustomKpiRoleCapabilities => Set<CustomKpiRoleCapabilityRow>();
    public DbSet<RoleAssignmentRow> RoleAssignments => Set<RoleAssignmentRow>();
    public DbSet<ApprovalDelegationRow> ApprovalDelegations => Set<ApprovalDelegationRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KpiDefinitionRow>(b =>
        {
            b.ToTable("kpi_definitions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.OrganizationId).HasColumnName("organization_id");
            b.Property(x => x.Code).HasColumnName("code");
            b.Property(x => x.Name).HasColumnName("name");
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.OwnerId).HasColumnName("owner_id");
            b.Property(x => x.Archived).HasColumnName("archived");
            b.Property(x => x.Revision).HasColumnName("revision");
            b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
        });
        modelBuilder.Entity<KpiVersionRow>(b =>
        {
            b.ToTable("kpi_versions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.DefinitionId, x.VersionNumber }).IsUnique();
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.DefinitionId).HasColumnName("definition_id");
            b.Property(x => x.VersionNumber).HasColumnName("version_number");
            b.Property(x => x.Name).HasColumnName("name");
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.ChangeSummary).HasColumnName("change_summary");
            b.Property(x => x.PredecessorVersionId).HasColumnName("predecessor_version_id");
            b.Property(x => x.Status).HasColumnName("status");
            b.Property(x => x.FormulaJson).HasColumnName("formula_json").HasColumnType("jsonb");
            b.Property(x => x.VariablesJson).HasColumnName("variables_json").HasColumnType("jsonb");
            b.Property(x => x.DeclaredResultType).HasColumnName("declared_result_type");
            b.Property(x => x.Cadence).HasColumnName("cadence");
            b.Property(x => x.ReviewComment).HasColumnName("review_comment");
            b.Property(x => x.EffectiveFrom).HasColumnName("effective_from");
            b.Property(x => x.EffectiveTo).HasColumnName("effective_to");
            b.Property(x => x.Revision).HasColumnName("revision");
            b.HasOne<KpiDefinitionRow>().WithMany().HasForeignKey(x => x.DefinitionId);
        });
        modelBuilder.Entity<KpiPeriodRow>(b =>
        {
            b.ToTable("kpi_periods");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.OrganizationId).HasColumnName("organization_id");
            b.Property(x => x.Code).HasColumnName("code");
            b.Property(x => x.Name).HasColumnName("name");
            b.Property(x => x.Description).HasColumnName("description");
            b.Property(x => x.Cadence).HasColumnName("cadence");
            b.Property(x => x.StartsAt).HasColumnName("starts_at");
            b.Property(x => x.EndsAt).HasColumnName("ends_at");
            b.Property(x => x.PlannerId).HasColumnName("planner_id");
            b.Property(x => x.ApproverId).HasColumnName("approver_id");
            b.Property(x => x.Status).HasColumnName("status");
            b.Property(x => x.LatestEffectiveRevision).HasColumnName("latest_effective_revision");
            b.Property(x => x.Revision).HasColumnName("revision");
            b.Property(x => x.SelectionsJson).HasColumnName("selections_json").HasColumnType("jsonb");
            b.Property(x => x.RevisionsJson).HasColumnName("revisions_json").HasColumnType("jsonb");
            b.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
        });
        modelBuilder.Entity<KpiPeriodActivationRow>(b =>
        {
            b.ToTable("kpi_period_activations");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.PeriodId).HasColumnName("period_id");
            b.Property(x => x.DefinitionId).HasColumnName("definition_id");
            b.Property(x => x.VersionId).HasColumnName("version_id");
            b.Property(x => x.EffectiveRevisionNumber).HasColumnName("effective_revision_number");
            b.Property(x => x.ActivatedAt).HasColumnName("activated_at");
            b.Property(x => x.ClosedAt).HasColumnName("closed_at");
            b.HasIndex(x => new { x.PeriodId, x.DefinitionId }).IsUnique();
        });
        modelBuilder.Entity<KpiPeriodAmendmentRow>(b =>
        {
            b.ToTable("kpi_period_amendments");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.PeriodId).HasColumnName("period_id");
            b.Property(x => x.RevisionNumber).HasColumnName("revision_number");
            b.Property(x => x.BaseRevisionNumber).HasColumnName("base_revision_number");
            b.Property(x => x.ProposedStartsAt).HasColumnName("proposed_starts_at");
            b.Property(x => x.ProposedEndsAt).HasColumnName("proposed_ends_at");
            b.Property(x => x.ProposedSelectionsJson).HasColumnName("proposed_selections_json").HasColumnType("jsonb");
            b.Property(x => x.Reason).HasColumnName("reason");
            b.Property(x => x.ProposedBy).HasColumnName("proposed_by");
            b.Property(x => x.ProposedAt).HasColumnName("proposed_at");
            b.Property(x => x.Status).HasColumnName("status");
            b.Property(x => x.ReviewedBy).HasColumnName("reviewed_by");
            b.Property(x => x.ReviewedAt).HasColumnName("reviewed_at");
            b.Property(x => x.ReviewComment).HasColumnName("review_comment");
            b.HasIndex(x => new { x.PeriodId, x.RevisionNumber }).IsUnique();
        });
        modelBuilder.Entity<KpiEvaluationRow>(b =>
        {
            b.ToTable("kpi_evaluations");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.ActivationId).HasColumnName("activation_id");
            b.Property(x => x.VersionId).HasColumnName("version_id");
            b.Property(x => x.FormulaJson).HasColumnName("formula_snapshot_json").HasColumnType("jsonb");
            b.Property(x => x.InputsJson).HasColumnName("inputs_json").HasColumnType("jsonb");
            b.Property(x => x.OutcomeJson).HasColumnName("outcome_json").HasColumnType("jsonb");
            b.Property(x => x.EvaluatorActorId).HasColumnName("evaluator_actor_id");
            b.Property(x => x.IsCurrent).HasColumnName("is_current_success");
            b.Property(x => x.SupersedesId).HasColumnName("supersedes_id");
            b.Property(x => x.CorrectionReason).HasColumnName("correction_reason");
            b.Property(x => x.CorrectionDiffJson).HasColumnName("correction_diff_json").HasColumnType("jsonb");
            b.Property(x => x.EvaluatedAt).HasColumnName("evaluated_at");
            b.HasIndex(x => new { x.ActivationId, x.IsCurrent }).HasFilter("is_current_success").IsUnique();
        });
        modelBuilder.Entity<AuditRecordRow>(b =>
        {
            b.ToTable("audit_records");
            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");
            b.Property(x => x.OrganizationId).HasColumnName("organization_id");
            b.Property(x => x.ActorId).HasColumnName("actor_id");
            b.Property(x => x.EntityType).HasColumnName("entity_type");
            b.Property(x => x.EntityId).HasColumnName("entity_id");
            b.Property(x => x.EventType).HasColumnName("event_type");
            b.Property(x => x.OccurredAt).HasColumnName("occurred_at");
            b.Property(x => x.CorrelationId).HasColumnName("correlation_id");
            b.Property(x => x.Reason).HasColumnName("reason");
            b.Property(x => x.SummaryJson).HasColumnName("summary_json").HasColumnType("jsonb");
            b.Property(x => x.ResourceRevision).HasColumnName("resource_revision");
            b.Property(x => x.CapabilityId).HasColumnName("capability_id");
            b.Property(x => x.Decision).HasColumnName("decision");
            b.Property(x => x.AssignmentIdsJson).HasColumnName("assignment_ids_json").HasColumnType("jsonb");
            b.Property(x => x.ScopeEvidenceJson).HasColumnName("scope_evidence_json").HasColumnType("jsonb");
            b.Property(x => x.AuthorizationEvidenceJson).HasColumnName("authorization_evidence_json").HasColumnType("jsonb");
            b.Property(x => x.RepresentedAuthorityActorId).HasColumnName("represented_authority_actor_id");
            b.Property(x => x.DelegationId).HasColumnName("delegation_id");
            b.HasIndex(x => new { x.OrganizationId, x.OccurredAt });
        });
        OrganizationAuthorizationConfiguration.Apply(modelBuilder);
    }
}

public sealed class KpiDefinitionRow { public Guid Id { get; set; } public Guid OrganizationId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public Guid OwnerId { get; set; } public bool Archived { get; set; } public long Revision { get; set; } public uint RowVersion { get; set; } }
public sealed class KpiVersionRow { public Guid Id { get; set; } public Guid DefinitionId { get; set; } public int VersionNumber { get; set; } public string Name { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public string ChangeSummary { get; set; } = string.Empty; public Guid? PredecessorVersionId { get; set; } public string Status { get; set; } = string.Empty; public string FormulaJson { get; set; } = "{}"; public string VariablesJson { get; set; } = "[]"; public string DeclaredResultType { get; set; } = string.Empty; public string Cadence { get; set; } = string.Empty; public string? ReviewComment { get; set; } public DateTimeOffset? EffectiveFrom { get; set; } public DateTimeOffset? EffectiveTo { get; set; } public long Revision { get; set; } }
public sealed class KpiPeriodRow { public Guid Id { get; set; } public Guid OrganizationId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public string Cadence { get; set; } = string.Empty; public DateTimeOffset StartsAt { get; set; } public DateTimeOffset EndsAt { get; set; } public Guid PlannerId { get; set; } public Guid? ApproverId { get; set; } public string Status { get; set; } = string.Empty; public string SelectionsJson { get; set; } = "{}"; public string RevisionsJson { get; set; } = "[]"; public int LatestEffectiveRevision { get; set; } public long Revision { get; set; } }
public sealed class KpiPeriodActivationRow { public Guid Id { get; set; } public Guid PeriodId { get; set; } public Guid DefinitionId { get; set; } public Guid VersionId { get; set; } public int EffectiveRevisionNumber { get; set; } public DateTimeOffset ActivatedAt { get; set; } public DateTimeOffset? ClosedAt { get; set; } }
public sealed class KpiPeriodAmendmentRow { public Guid Id { get; set; } public Guid PeriodId { get; set; } public int RevisionNumber { get; set; } public int BaseRevisionNumber { get; set; } public DateTimeOffset ProposedStartsAt { get; set; } public DateTimeOffset ProposedEndsAt { get; set; } public string ProposedSelectionsJson { get; set; } = "{}"; public string Reason { get; set; } = string.Empty; public Guid ProposedBy { get; set; } public DateTimeOffset ProposedAt { get; set; } public string Status { get; set; } = string.Empty; public Guid? ReviewedBy { get; set; } public DateTimeOffset? ReviewedAt { get; set; } public string? ReviewComment { get; set; } }
public sealed class KpiEvaluationRow { public Guid Id { get; set; } public Guid ActivationId { get; set; } public Guid VersionId { get; set; } public string FormulaJson { get; set; } = "{}"; public string InputsJson { get; set; } = "{}"; public string OutcomeJson { get; set; } = "{}"; public Guid EvaluatorActorId { get; set; } public bool IsCurrent { get; set; } public Guid? SupersedesId { get; set; } public string? CorrectionReason { get; set; } public string? CorrectionDiffJson { get; set; } public DateTimeOffset EvaluatedAt { get; set; } }
public sealed class AuditRecordRow
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ActorId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string SummaryJson { get; set; } = "{}";
    public long? ResourceRevision { get; set; }
    public string? CapabilityId { get; set; }
    public string? Decision { get; set; }
    public string AssignmentIdsJson { get; set; } = "[]";
    public string ScopeEvidenceJson { get; set; } = "[]";
    public string AuthorizationEvidenceJson { get; set; } = "{}";
    public Guid? RepresentedAuthorityActorId { get; set; }
    public Guid? DelegationId { get; set; }
}
public abstract class OrganizationScopedHeadRow { public Guid Id { get; set; } public Guid OrganizationId { get; set; } public long Revision { get; set; } public uint RowVersion { get; set; } public string Code { get; set; } = string.Empty; }
public abstract class OrganizationScopedFactRow { public Guid Id { get; set; } public Guid OrganizationId { get; set; } }
public sealed class OrganizationRow { public Guid Id { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string TimeZoneId { get; set; } = "UTC"; public string Status { get; set; } = "active"; public bool OperationallyExposed { get; set; } public long Revision { get; set; } public uint RowVersion { get; set; } }
public sealed class OrganizationUnitRow : OrganizationScopedHeadRow { public string Name { get; set; } = string.Empty; public Guid? ParentUnitId { get; set; } public string Status { get; set; } = "active"; public DateTimeOffset EffectiveFrom { get; set; } public DateTimeOffset? EffectiveTo { get; set; } }
public sealed class OrganizationPositionRow : OrganizationScopedHeadRow { public string Name { get; set; } = string.Empty; public Guid OrganizationUnitId { get; set; } public string Status { get; set; } = "active"; public DateTimeOffset EffectiveFrom { get; set; } public DateTimeOffset? EffectiveTo { get; set; } }
public sealed class OrganizationEmployeeRow : OrganizationScopedHeadRow { public string DisplayName { get; set; } = string.Empty; public DateTimeOffset EmploymentFrom { get; set; } public DateTimeOffset? EmploymentTo { get; set; } public string AccountStatus { get; set; } = "active"; }
public sealed class OrganizationPositionAssignmentRow : OrganizationScopedFactRow { public Guid EmployeeId { get; set; } public Guid PositionId { get; set; } public DateTimeOffset EffectiveFrom { get; set; } public DateTimeOffset? EffectiveTo { get; set; } public decimal AllocationWeight { get; set; } public bool IsPrimary { get; set; } }
public sealed class OrganizationReportingRelationshipRow : OrganizationScopedFactRow { public Guid SubordinatePositionId { get; set; } public Guid ManagerPositionId { get; set; } public DateTimeOffset EffectiveFrom { get; set; } public DateTimeOffset? EffectiveTo { get; set; } public string RelationshipType { get; set; } = "line"; }
public sealed class OrganizationBaselineRow : OrganizationScopedFactRow { public long StructureRevision { get; set; } public DateTimeOffset EffectiveFrom { get; set; } public string Status { get; set; } = string.Empty; public string SnapshotJson { get; set; } = "{}"; public string EvidenceJson { get; set; } = "{}"; public string ContentHash { get; set; } = string.Empty; public Guid? PreviousBaselineId { get; set; } }
public sealed class BaselineApplicabilitySegmentRow : OrganizationScopedFactRow { public Guid BaselineId { get; set; } public DateTimeOffset EffectiveFrom { get; set; } public DateTimeOffset? EffectiveTo { get; set; } }
public sealed class CustomKpiRoleRow { public Guid Id { get; set; } public Guid OrganizationId { get; set; } public string Name { get; set; } = string.Empty; public string Status { get; set; } = "Active"; public long Revision { get; set; } public uint RowVersion { get; set; } }
public sealed class CustomKpiRoleVersionRow { public Guid Id { get; set; } public Guid OrganizationId { get; set; } public Guid RoleId { get; set; } public int VersionNumber { get; set; } public string Status { get; set; } = "Active"; public Guid CreatedBy { get; set; } public DateTimeOffset CreatedAt { get; set; } }
public sealed class CustomKpiRoleCapabilityRow { public Guid OrganizationId { get; set; } public Guid RoleVersionId { get; set; } public string CapabilityId { get; set; } = string.Empty; }
public sealed class RoleAssignmentRow { public Guid Id { get; set; } public Guid OrganizationId { get; set; } public Guid EmployeeId { get; set; } public Guid RoleVersionId { get; set; } public string ScopeKind { get; set; } = string.Empty; public Guid? ScopeTargetId { get; set; } public Guid? BaselineId { get; set; } public DateTimeOffset EffectiveFrom { get; set; } public DateTimeOffset? EffectiveTo { get; set; } public string Status { get; set; } = string.Empty; public long Revision { get; set; } public uint RowVersion { get; set; } }
public sealed class ApprovalDelegationRow { public Guid Id { get; set; } public Guid OrganizationId { get; set; } public Guid OriginalActorId { get; set; } public Guid DelegateActorId { get; set; } public string CapabilityId { get; set; } = string.Empty; public string ScopeKind { get; set; } = string.Empty; public Guid? ScopeTargetId { get; set; } public Guid? BaselineId { get; set; } public DateTimeOffset EffectiveFrom { get; set; } public DateTimeOffset? EffectiveTo { get; set; } public string Status { get; set; } = string.Empty; }
