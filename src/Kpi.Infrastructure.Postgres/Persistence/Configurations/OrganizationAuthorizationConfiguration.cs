using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kpi.Infrastructure.Postgres.Persistence.Configurations;

/// <summary>EF Core mapping for Organization-scoped workforce and baseline facts.</summary>
public static class OrganizationAuthorizationConfiguration
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrganizationRow>(b =>
        {
            b.ToTable("organizations"); b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.Code).HasColumnName("code").IsRequired();
            b.Property(x => x.Name).HasColumnName("name"); b.Property(x => x.TimeZoneId).HasColumnName("time_zone_id");
            b.Property(x => x.Status).HasColumnName("status"); b.Property(x => x.OperationallyExposed).HasColumnName("operationally_exposed");
            b.Property(x => x.Revision).HasColumnName("revision"); b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion();
            b.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<OrganizationUnitRow>(b =>
        {
            ConfigureOrganizationScopedHead(b, "organization_units", "code");
            b.Property(x => x.Name).HasColumnName("name"); b.Property(x => x.ParentUnitId).HasColumnName("parent_unit_id");
            b.Property(x => x.Status).HasColumnName("status"); b.Property(x => x.EffectiveFrom).HasColumnName("effective_from"); b.Property(x => x.EffectiveTo).HasColumnName("effective_to");
            b.HasOne<OrganizationUnitRow>().WithMany().HasForeignKey(x => new { x.OrganizationId, x.ParentUnitId }).HasPrincipalKey(x => new { x.OrganizationId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<OrganizationPositionRow>(b =>
        {
            ConfigureOrganizationScopedHead(b, "organization_positions", "code");
            b.Property(x => x.Name).HasColumnName("name"); b.Property(x => x.OrganizationUnitId).HasColumnName("organization_unit_id");
            b.Property(x => x.Status).HasColumnName("status"); b.Property(x => x.EffectiveFrom).HasColumnName("effective_from"); b.Property(x => x.EffectiveTo).HasColumnName("effective_to");
            b.HasOne<OrganizationUnitRow>().WithMany().HasForeignKey(x => new { x.OrganizationId, x.OrganizationUnitId }).HasPrincipalKey(x => new { x.OrganizationId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<OrganizationEmployeeRow>(b =>
        {
            ConfigureOrganizationScopedHead(b, "organization_employees", "employee_number");
            b.Property(x => x.DisplayName).HasColumnName("display_name"); b.Property(x => x.EmploymentFrom).HasColumnName("employment_from"); b.Property(x => x.EmploymentTo).HasColumnName("employment_to");
            b.Property(x => x.AccountStatus).HasColumnName("account_status");
        });
        modelBuilder.Entity<OrganizationPositionAssignmentRow>(b =>
        {
            ConfigureOrganizationScopedFact(b, "organization_position_assignments");
            b.Property(x => x.EmployeeId).HasColumnName("employee_id"); b.Property(x => x.PositionId).HasColumnName("position_id");
            b.Property(x => x.EffectiveFrom).HasColumnName("effective_from"); b.Property(x => x.EffectiveTo).HasColumnName("effective_to");
            b.Property(x => x.AllocationWeight).HasColumnName("allocation_weight"); b.Property(x => x.IsPrimary).HasColumnName("is_primary");
            b.HasOne<OrganizationEmployeeRow>().WithMany().HasForeignKey(x => new { x.OrganizationId, x.EmployeeId }).HasPrincipalKey(x => new { x.OrganizationId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<OrganizationPositionRow>().WithMany().HasForeignKey(x => new { x.OrganizationId, x.PositionId }).HasPrincipalKey(x => new { x.OrganizationId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<OrganizationReportingRelationshipRow>(b =>
        {
            ConfigureOrganizationScopedFact(b, "organization_reporting_relationships");
            b.Property(x => x.SubordinatePositionId).HasColumnName("subordinate_position_id"); b.Property(x => x.ManagerPositionId).HasColumnName("manager_position_id");
            b.Property(x => x.EffectiveFrom).HasColumnName("effective_from"); b.Property(x => x.EffectiveTo).HasColumnName("effective_to");
            b.Property(x => x.RelationshipType).HasColumnName("relationship_type");
            b.HasOne<OrganizationPositionRow>().WithMany().HasForeignKey(x => new { x.OrganizationId, x.SubordinatePositionId }).HasPrincipalKey(x => new { x.OrganizationId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<OrganizationPositionRow>().WithMany().HasForeignKey(x => new { x.OrganizationId, x.ManagerPositionId }).HasPrincipalKey(x => new { x.OrganizationId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<OrganizationBaselineRow>(b =>
        {
            ConfigureOrganizationScopedFact(b, "organization_baselines");
            b.Property(x => x.SnapshotJson).HasColumnName("snapshot_json").HasColumnType("jsonb");
            b.Property(x => x.EffectiveFrom).HasColumnName("effective_from");
            b.Property(x => x.Status).HasColumnName("status"); b.Property(x => x.EvidenceJson).HasColumnName("evidence_json").HasColumnType("jsonb");
            b.Property(x => x.ContentHash).HasColumnName("content_hash"); b.Property(x => x.PreviousBaselineId).HasColumnName("previous_baseline_id");
            b.HasOne<OrganizationBaselineRow>().WithMany().HasForeignKey(x => new { x.OrganizationId, x.PreviousBaselineId }).HasPrincipalKey(x => new { x.OrganizationId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<BaselineApplicabilitySegmentRow>(b =>
        {
            ConfigureOrganizationScopedFact(b, "organization_baseline_applicability_segments");
            b.Property(x => x.BaselineId).HasColumnName("baseline_id"); b.Property(x => x.EffectiveFrom).HasColumnName("effective_from");
            b.Property(x => x.EffectiveTo).HasColumnName("effective_to");
            b.HasIndex(x => new { x.OrganizationId, x.EffectiveFrom });
            b.HasOne<OrganizationBaselineRow>().WithMany().HasForeignKey(x => new { x.OrganizationId, x.BaselineId }).HasPrincipalKey(x => new { x.OrganizationId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureOrganizationScopedHead<TEntity>(EntityTypeBuilder<TEntity> b, string table, string uniqueColumn)
        where TEntity : OrganizationScopedHeadRow
    {
        b.ToTable(table); b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.OrganizationId).HasColumnName("organization_id"); b.Property(x => x.Code).HasColumnName(uniqueColumn).IsRequired(); b.Property(x => x.Revision).HasColumnName("revision");
        b.Property(x => x.RowVersion).HasColumnName("xmin").IsRowVersion(); b.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique(); b.HasAlternateKey(x => new { x.OrganizationId, x.Id });
        b.HasOne<OrganizationRow>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureOrganizationScopedFact<TEntity>(EntityTypeBuilder<TEntity> b, string table)
        where TEntity : OrganizationScopedFactRow
    {
        b.ToTable(table); b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.OrganizationId).HasColumnName("organization_id"); b.HasAlternateKey(x => new { x.OrganizationId, x.Id });
        b.HasOne<OrganizationRow>().WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
    }
}
