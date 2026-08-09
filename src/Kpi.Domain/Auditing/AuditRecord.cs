namespace Kpi.Domain.Auditing;

/// <summary>Immutable business audit fact, distinct from technical logging.</summary>
public sealed record AuditRecord(Guid Id, Guid OrganizationId, Guid ActorId, string EntityType, Guid EntityId, AuditEventType EventType, DateTimeOffset OccurredAt, string CorrelationId, string? Reason = null, string? Summary = null)
{
    public static AuditRecord Create(Guid organizationId, Guid actorId, string entityType, Guid entityId, AuditEventType eventType, DateTimeOffset occurredAt, string correlationId, string? reason = null, string? summary = null) =>
        new(Guid.NewGuid(), organizationId, actorId, entityType, entityId, eventType, occurredAt, correlationId, reason, summary);
}
