namespace Kpi.Application.Persistence;

public interface IOrganizationUnitOfWork
{
    Guid OrganizationId { get; }
    long ExpectedRevision { get; }
    Task CommitAsync(CancellationToken cancellationToken = default);
}
