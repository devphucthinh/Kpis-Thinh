namespace Kpi.Application.Common;

/// <summary>Application transaction boundary independent of EF or HTTP.</summary>
public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
