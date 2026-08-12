using Kpi.Domain.Auditing;

namespace Kpi.Application.Persistence;

public interface IAuditWriter
{
    Task AppendAsync(AuditRecord record, CancellationToken cancellationToken = default);
}
