using Microsoft.EntityFrameworkCore;

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KpiDefinitionRow>(b => { b.ToTable("kpi_definitions"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique(); b.Property(x => x.RowVersion).IsRowVersion(); });
        modelBuilder.Entity<KpiVersionRow>(b => { b.ToTable("kpi_versions"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.DefinitionId, x.VersionNumber }).IsUnique(); b.Property(x => x.FormulaJson).HasColumnType("jsonb"); b.Property(x => x.VariablesJson).HasColumnType("jsonb"); b.HasOne<KpiDefinitionRow>().WithMany().HasForeignKey(x => x.DefinitionId); });
        modelBuilder.Entity<KpiPeriodRow>(b => { b.ToTable("kpi_periods"); b.HasKey(x => x.Id); b.Property(x => x.SelectionsJson).HasColumnType("jsonb"); b.Property(x => x.RevisionsJson).HasColumnType("jsonb"); b.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique(); });
        modelBuilder.Entity<KpiPeriodActivationRow>(b => { b.ToTable("kpi_period_activations"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.PeriodId, x.DefinitionId }).IsUnique(); });
        modelBuilder.Entity<KpiPeriodAmendmentRow>(b => { b.ToTable("kpi_period_amendments"); b.HasKey(x => x.Id); b.Property(x => x.ProposedSelectionsJson).HasColumnType("jsonb"); b.HasIndex(x => new { x.PeriodId, x.RevisionNumber }).IsUnique(); });
        modelBuilder.Entity<KpiEvaluationRow>(b => { b.ToTable("kpi_evaluations"); b.HasKey(x => x.Id); b.Property(x => x.FormulaJson).HasColumnType("jsonb"); b.Property(x => x.InputsJson).HasColumnType("jsonb"); b.Property(x => x.OutcomeJson).HasColumnType("jsonb"); b.Property(x => x.CorrectionDiffJson).HasColumnType("jsonb"); b.HasIndex(x => new { x.ActivationId, x.IsCurrent }).HasFilter("\"IsCurrent\" = true").IsUnique(); });
        modelBuilder.Entity<AuditRecordRow>(b => { b.ToTable("audit_records"); b.HasKey(x => x.Id); b.Property(x => x.SummaryJson).HasColumnType("jsonb"); b.HasIndex(x => new { x.OrganizationId, x.OccurredAt }); });
    }
}

public sealed class KpiDefinitionRow { public Guid Id { get; set; } public Guid OrganizationId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public Guid OwnerId { get; set; } public bool Archived { get; set; } public long Revision { get; set; } public uint RowVersion { get; set; } }
public sealed class KpiVersionRow { public Guid Id { get; set; } public Guid DefinitionId { get; set; } public int VersionNumber { get; set; } public string Name { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public string ChangeSummary { get; set; } = string.Empty; public Guid? PredecessorVersionId { get; set; } public string Status { get; set; } = string.Empty; public string FormulaJson { get; set; } = "{}"; public string VariablesJson { get; set; } = "[]"; public string DeclaredResultType { get; set; } = string.Empty; public string Cadence { get; set; } = string.Empty; public string? ReviewComment { get; set; } public DateTimeOffset? EffectiveFrom { get; set; } public DateTimeOffset? EffectiveTo { get; set; } public long Revision { get; set; } }
public sealed class KpiPeriodRow { public Guid Id { get; set; } public Guid OrganizationId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public string Cadence { get; set; } = string.Empty; public DateTimeOffset StartsAt { get; set; } public DateTimeOffset EndsAt { get; set; } public Guid PlannerId { get; set; } public Guid? ApproverId { get; set; } public string Status { get; set; } = string.Empty; public string SelectionsJson { get; set; } = "{}"; public string RevisionsJson { get; set; } = "[]"; public int LatestEffectiveRevision { get; set; } public long Revision { get; set; } }
public sealed class KpiPeriodActivationRow { public Guid Id { get; set; } public Guid PeriodId { get; set; } public Guid DefinitionId { get; set; } public Guid VersionId { get; set; } public int EffectiveRevisionNumber { get; set; } public DateTimeOffset ActivatedAt { get; set; } public DateTimeOffset? ClosedAt { get; set; } }
public sealed class KpiPeriodAmendmentRow { public Guid Id { get; set; } public Guid PeriodId { get; set; } public int RevisionNumber { get; set; } public int BaseRevisionNumber { get; set; } public DateTimeOffset ProposedStartsAt { get; set; } public DateTimeOffset ProposedEndsAt { get; set; } public string ProposedSelectionsJson { get; set; } = "{}"; public string Reason { get; set; } = string.Empty; public Guid ProposedBy { get; set; } public DateTimeOffset ProposedAt { get; set; } public string Status { get; set; } = string.Empty; public Guid? ReviewedBy { get; set; } public DateTimeOffset? ReviewedAt { get; set; } public string? ReviewComment { get; set; } }
public sealed class KpiEvaluationRow { public Guid Id { get; set; } public Guid ActivationId { get; set; } public Guid VersionId { get; set; } public string FormulaJson { get; set; } = "{}"; public string InputsJson { get; set; } = "{}"; public string OutcomeJson { get; set; } = "{}"; public Guid EvaluatorActorId { get; set; } public bool IsCurrent { get; set; } public Guid? SupersedesId { get; set; } public string? CorrectionReason { get; set; } public string? CorrectionDiffJson { get; set; } public DateTimeOffset EvaluatedAt { get; set; } }
public sealed class AuditRecordRow { public Guid Id { get; set; } public Guid OrganizationId { get; set; } public Guid ActorId { get; set; } public string EntityType { get; set; } = string.Empty; public Guid EntityId { get; set; } public string EventType { get; set; } = string.Empty; public DateTimeOffset OccurredAt { get; set; } public string CorrelationId { get; set; } = string.Empty; public string? Reason { get; set; } public string SummaryJson { get; set; } = "{}"; }
