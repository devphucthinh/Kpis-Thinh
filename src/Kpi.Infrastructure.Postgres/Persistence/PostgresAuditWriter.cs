using System.Text.Json;
using Kpi.Application.Persistence;
using Kpi.Domain.Auditing;

namespace Kpi.Infrastructure.Postgres.Persistence;

/// <summary>Appends audit rows to the current DbContext; the surrounding unit of work commits them.</summary>
public sealed class PostgresAuditWriter(KpiDbContext context) : IAuditWriter
{
    public async Task AppendAsync(AuditRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (await context.AuditRecords.FindAsync([record.Id], cancellationToken) is not null)
            return;

        context.AuditRecords.Add(AuditRecordRowMapper.ToRow(record));
    }
}

internal static class AuditRecordRowMapper
{
    public static AuditRecordRow ToRow(AuditRecord record) => new()
    {
        Id = record.Id,
        OrganizationId = record.OrganizationId,
        ActorId = record.ActorId,
        EntityType = record.EntityType,
        EntityId = record.EntityId,
        EventType = record.EventType.ToString(),
        OccurredAt = record.OccurredAt,
        CorrelationId = record.CorrelationId,
        Reason = record.Reason,
        SummaryJson = JsonSerializer.Serialize(new { record.Summary }),
        ResourceRevision = record.ResourceRevision,
        CapabilityId = record.CapabilityId,
        Decision = record.Decision,
        AssignmentIdsJson = JsonSerializer.Serialize(record.AssignmentIds),
        ScopeEvidenceJson = JsonSerializer.Serialize(record.ScopeEvidence),
        AuthorizationEvidenceJson = JsonSerializer.Serialize(new { record.RepresentedAuthorityActorId, record.DelegationId }),
        RepresentedAuthorityActorId = record.RepresentedAuthorityActorId,
        DelegationId = record.DelegationId
    };
}
