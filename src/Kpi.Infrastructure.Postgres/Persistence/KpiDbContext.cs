using Microsoft.EntityFrameworkCore;

namespace Kpi.Infrastructure.Postgres.Persistence;

/// <summary>Relational/queryable projection and immutable JSONB snapshots for KPI history.</summary>
public sealed class KpiDbContext(DbContextOptions<KpiDbContext> options) : DbContext(options)
{
    public DbSet<KpiDefinitionRow> Definitions => Set<KpiDefinitionRow>();
    public DbSet<KpiVersionRow> Versions => Set<KpiVersionRow>();
    public DbSet<KpiEvaluationRow> Evaluations => Set<KpiEvaluationRow>();
    public DbSet<AuditRecordRow> AuditRecords => Set<AuditRecordRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KpiDefinitionRow>(b => { b.ToTable("kpi_definitions"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique(); b.Property(x => x.RowVersion).IsRowVersion(); });
        modelBuilder.Entity<KpiVersionRow>(b => { b.ToTable("kpi_versions"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.DefinitionId, x.VersionNumber }).IsUnique(); b.Property(x => x.FormulaJson).HasColumnType("jsonb"); b.HasOne<KpiDefinitionRow>().WithMany().HasForeignKey(x => x.DefinitionId); });
        modelBuilder.Entity<KpiEvaluationRow>(b => { b.ToTable("kpi_evaluations"); b.HasKey(x => x.Id); b.Property(x => x.InputsJson).HasColumnType("jsonb"); b.Property(x => x.OutcomeJson).HasColumnType("jsonb"); b.HasIndex(x => new { x.ActivationId, x.IsCurrent }).HasFilter("\"IsCurrent\" = true").IsUnique(); });
        modelBuilder.Entity<AuditRecordRow>(b => { b.ToTable("audit_records"); b.HasKey(x => x.Id); b.Property(x => x.SummaryJson).HasColumnType("jsonb"); b.HasIndex(x => new { x.OrganizationId, x.OccurredAt }); });
    }
}

public sealed class KpiDefinitionRow { public Guid Id { get; set; } public Guid OrganizationId { get; set; } public string Code { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string Description { get; set; } = string.Empty; public bool Archived { get; set; } public uint RowVersion { get; set; } }
public sealed class KpiVersionRow { public Guid Id { get; set; } public Guid DefinitionId { get; set; } public int VersionNumber { get; set; } public string Status { get; set; } = string.Empty; public string FormulaJson { get; set; } = "{}"; public DateTimeOffset? EffectiveFrom { get; set; } public DateTimeOffset? EffectiveTo { get; set; } }
public sealed class KpiEvaluationRow { public Guid Id { get; set; } public Guid ActivationId { get; set; } public Guid VersionId { get; set; } public string InputsJson { get; set; } = "{}"; public string OutcomeJson { get; set; } = "{}"; public bool IsCurrent { get; set; } public Guid? SupersedesId { get; set; } public DateTimeOffset EvaluatedAt { get; set; } }
public sealed class AuditRecordRow { public Guid Id { get; set; } public Guid OrganizationId { get; set; } public Guid ActorId { get; set; } public string EntityType { get; set; } = string.Empty; public Guid EntityId { get; set; } public string EventType { get; set; } = string.Empty; public DateTimeOffset OccurredAt { get; set; } public string CorrelationId { get; set; } = string.Empty; public string SummaryJson { get; set; } = "{}"; }
