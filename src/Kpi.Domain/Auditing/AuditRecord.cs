namespace Kpi.Domain.Auditing;

/// <summary>Immutable business audit fact, distinct from technical logging.</summary>
public sealed record AuditRecord
{
    public AuditRecord(
        Guid id,
        Guid organizationId,
        Guid actorId,
        string entityType,
        Guid entityId,
        AuditEventType eventType,
        DateTimeOffset occurredAt,
        string correlationId,
        string? reason = null,
        string? summary = null,
        long? resourceRevision = null,
        string? capabilityId = null,
        string? decision = null,
        IReadOnlyList<Guid>? assignmentIds = null,
        IReadOnlyList<string>? scopeEvidence = null,
        Guid? representedAuthorityActorId = null,
        Guid? delegationId = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Audit record id is required.", nameof(id));
        if (organizationId == Guid.Empty)
            throw new ArgumentException("Audit organization id is required.", nameof(organizationId));
        if (actorId == Guid.Empty)
            throw new ArgumentException("Audit actor id is required.", nameof(actorId));
        if (string.IsNullOrWhiteSpace(entityType))
            throw new ArgumentException("Audit entity type is required.", nameof(entityType));
        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("Audit correlation id is required.", nameof(correlationId));

        Id = id;
        OrganizationId = organizationId;
        ActorId = actorId;
        EntityType = entityType.Trim();
        EntityId = entityId;
        EventType = eventType;
        OccurredAt = occurredAt.ToUniversalTime();
        CorrelationId = correlationId.Trim();
        Reason = reason;
        Summary = summary;
        ResourceRevision = resourceRevision;
        CapabilityId = capabilityId;
        Decision = decision;
        AssignmentIds = Array.AsReadOnly((assignmentIds ?? Array.Empty<Guid>()).ToArray());
        ScopeEvidence = Array.AsReadOnly((scopeEvidence ?? Array.Empty<string>()).ToArray());
        RepresentedAuthorityActorId = representedAuthorityActorId;
        DelegationId = delegationId;
    }

    public Guid Id { get; }
    public Guid OrganizationId { get; }
    public Guid ActorId { get; }
    public string EntityType { get; }
    public Guid EntityId { get; }
    public AuditEventType EventType { get; }
    public DateTimeOffset OccurredAt { get; }
    public string CorrelationId { get; }
    public string? Reason { get; }
    public string? Summary { get; }
    public long? ResourceRevision { get; }
    public string? CapabilityId { get; }
    public string? Decision { get; }
    public IReadOnlyList<Guid> AssignmentIds { get; }
    public IReadOnlyList<string> ScopeEvidence { get; }
    public Guid? RepresentedAuthorityActorId { get; }
    public Guid? DelegationId { get; }

    public static AuditRecord Create(
        Guid organizationId,
        Guid actorId,
        string entityType,
        Guid entityId,
        AuditEventType eventType,
        DateTimeOffset occurredAt,
        string correlationId,
        string? reason = null,
        string? summary = null,
        long? resourceRevision = null,
        string? capabilityId = null,
        string? decision = null,
        IReadOnlyList<Guid>? assignmentIds = null,
        IReadOnlyList<string>? scopeEvidence = null,
        Guid? representedAuthorityActorId = null,
        Guid? delegationId = null) =>
        new(Guid.NewGuid(), organizationId, actorId, entityType, entityId, eventType, occurredAt, correlationId,
            reason, summary, resourceRevision, capabilityId, decision, assignmentIds, scopeEvidence,
            representedAuthorityActorId, delegationId);
}
