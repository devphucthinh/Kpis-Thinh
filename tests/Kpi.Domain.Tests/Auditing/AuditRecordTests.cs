using Kpi.Domain.Auditing;
using Xunit;

namespace Kpi.Domain.Tests.Auditing;

public sealed class AuditRecordTests
{
    [Fact]
    public void Audit_record_is_immutable_and_contains_governed_facts()
    {
        var record = AuditRecord.Create(Guid.NewGuid(), Guid.NewGuid(), "KPI", Guid.NewGuid(), AuditEventType.Created, DateTimeOffset.UtcNow, "corr", reason: "reason");
        Assert.Equal("corr", record.CorrelationId); Assert.Equal("reason", record.Reason); Assert.Equal(AuditEventType.Created, record.EventType);
    }
}
