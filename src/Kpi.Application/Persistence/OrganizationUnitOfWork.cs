using Kpi.Domain.Auditing;

namespace Kpi.Application.Persistence;

/// <summary>Collects one Organization command's audit facts and commits them atomically.</summary>
public sealed class OrganizationUnitOfWork(
    Guid organizationId,
    long expectedRevision,
    IOrganizationTransaction transaction) : IOrganizationUnitOfWork
{
    private readonly List<AuditRecord> records = [];

    public Guid OrganizationId { get; } = organizationId;
    public long ExpectedRevision { get; } = expectedRevision;

    public void RecordAudit(AuditRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (record.OrganizationId != OrganizationId)
            throw new InvalidOperationException("Audit organization does not match the unit of work.");

        records.Add(record);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = records.ToArray();
        await transaction.CommitAsync(OrganizationId, ExpectedRevision, snapshot, cancellationToken);
        records.Clear();
    }

    public async Task CommitAsync(Func<CancellationToken, Task> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var snapshot = records.ToArray();
        await transaction.CommitAsync(OrganizationId, ExpectedRevision, command, snapshot, cancellationToken);
        records.Clear();
    }
}
