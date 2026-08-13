using Kpi.Domain.Auditing;

namespace Kpi.Application.Persistence;

public interface IOrganizationUnitOfWork
{
    Guid OrganizationId { get; }
    long ExpectedRevision { get; }
    void RecordAudit(AuditRecord record);
    Task CommitAsync(CancellationToken cancellationToken = default);
}

/// <summary>Infrastructure-owned atomic commit seam for one Organization command.</summary>
public interface IOrganizationTransaction
{
    Task CommitAsync(
        Guid organizationId,
        long expectedRevision,
        IReadOnlyList<AuditRecord> records,
        CancellationToken cancellationToken = default);
}
