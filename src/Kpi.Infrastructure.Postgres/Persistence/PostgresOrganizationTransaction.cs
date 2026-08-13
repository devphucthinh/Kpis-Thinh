using Kpi.Application.Persistence;
using Kpi.Domain.Auditing;
using Microsoft.EntityFrameworkCore;

namespace Kpi.Infrastructure.Postgres.Persistence;

/// <summary>PostgreSQL transaction adapter for one Organization command and its audit facts.</summary>
public sealed class PostgresOrganizationTransaction(KpiDbContext context) : IOrganizationTransaction
{
    public async Task CommitAsync(
        Guid organizationId,
        long expectedRevision,
        IReadOnlyList<AuditRecord> records,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(records);
        if (records.Any(record => record.OrganizationId != organizationId))
            throw new InvalidOperationException("Audit organization does not match the transaction organization.");

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        foreach (var record in records)
        {
            if (await context.AuditRecords.FindAsync([record.Id], cancellationToken) is null)
                context.AuditRecords.Add(AuditRecordRowMapper.ToRow(record));
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
