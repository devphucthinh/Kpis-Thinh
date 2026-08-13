using Kpi.Application.Persistence;
using Kpi.Domain.Auditing;
using Xunit;

namespace Kpi.Application.Tests.Persistence;

public sealed class OrganizationUnitOfWorkTests
{
    [Fact(DisplayName = "FR-033 one organization commit carries its immutable audit evidence")]
    public async Task Commit_forwards_recorded_audit_facts_once()
    {
        var organizationId = Guid.NewGuid();
        var transaction = new RecordingTransaction();
        var unitOfWork = new OrganizationUnitOfWork(organizationId, 4, transaction);
        var record = AuditRecord.Create(organizationId, Guid.NewGuid(), "KpiPlan", Guid.NewGuid(), AuditEventType.Submitted,
            DateTimeOffset.UtcNow, "corr-1", capabilityId: "organization.structure.view", decision: "allowed");

        unitOfWork.RecordAudit(record);
        await unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(organizationId, transaction.OrganizationId);
        Assert.Equal(4, transaction.ExpectedRevision);
        Assert.Single(transaction.Records);
        Assert.Same(record, transaction.Records[0]);
    }

    [Fact(DisplayName = "FR-001 cross-organization audit records are rejected before commit")]
    public void Cross_organization_audit_record_is_rejected()
    {
        var organizationId = Guid.NewGuid();
        var unitOfWork = new OrganizationUnitOfWork(organizationId, 0, new RecordingTransaction());
        var foreignRecord = AuditRecord.Create(Guid.NewGuid(), Guid.NewGuid(), "KpiPlan", Guid.NewGuid(), AuditEventType.Submitted,
            DateTimeOffset.UtcNow, "corr-2");

        Assert.Throws<InvalidOperationException>(() => unitOfWork.RecordAudit(foreignRecord));
    }

    private sealed class RecordingTransaction : IOrganizationTransaction
    {
        public Guid OrganizationId { get; private set; }
        public long ExpectedRevision { get; private set; }
        public IReadOnlyList<AuditRecord> Records { get; private set; } = [];

        public Task CommitAsync(Guid organizationId, long expectedRevision, IReadOnlyList<AuditRecord> records, CancellationToken cancellationToken = default)
        {
            OrganizationId = organizationId;
            ExpectedRevision = expectedRevision;
            Records = records;
            return Task.CompletedTask;
        }
    }
}
