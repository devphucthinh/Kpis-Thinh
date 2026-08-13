using Kpi.Domain.Auditing;
using Xunit;

namespace Kpi.Domain.Tests.Auditing;

public sealed class AuditRecordTests
{
    [Fact(DisplayName = "FR-033 audit records retain immutable governed facts")]
    public void Audit_record_is_immutable_and_contains_governed_facts()
    {
        var record = AuditRecord.Create(Guid.NewGuid(), Guid.NewGuid(), "KPI", Guid.NewGuid(), AuditEventType.Created, DateTimeOffset.UtcNow, "corr", reason: "reason");
        Assert.Equal("corr", record.CorrelationId); Assert.Equal("reason", record.Reason); Assert.Equal(AuditEventType.Created, record.EventType);
    }

    [Fact(DisplayName = "FR-033 audit record preserves authorization decision evidence")]
    public void Audit_record_preserves_immutable_authorization_evidence()
    {
        var assignments = new[] { Guid.NewGuid() };
        var scopeEvidence = new[] { "organization:org-1", "baseline:baseline-1" };
        var record = AuditRecord.Create(
            Guid.NewGuid(), Guid.NewGuid(), "KpiPlan", Guid.NewGuid(), AuditEventType.Rejected,
            DateTimeOffset.UtcNow, "corr-1", reason: "scope mismatch", summary: "safe detail",
            resourceRevision: 7, capabilityId: "organization.structure.view", decision: "denied",
            assignmentIds: assignments, scopeEvidence: scopeEvidence,
            representedAuthorityActorId: Guid.NewGuid(), delegationId: Guid.NewGuid());

        assignments[0] = Guid.Empty;
        scopeEvidence[0] = "tampered";

        Assert.Equal(7, record.ResourceRevision);
        Assert.Equal("organization.structure.view", record.CapabilityId);
        Assert.Equal("denied", record.Decision);
        Assert.NotEqual(Guid.Empty, record.AssignmentIds[0]);
        Assert.Equal("organization:org-1", record.ScopeEvidence[0]);
        Assert.NotNull(record.RepresentedAuthorityActorId);
        Assert.NotNull(record.DelegationId);
    }
}
