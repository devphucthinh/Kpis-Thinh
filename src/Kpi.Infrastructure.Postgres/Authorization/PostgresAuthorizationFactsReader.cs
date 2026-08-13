using Kpi.Application.Authorization;
using Kpi.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kpi.Infrastructure.Postgres.Authorization;

/// <summary>
/// Loads the committed workforce facts available in the foundation schema and
/// fails closed for capabilities/scopes that require later authorization tables.
/// </summary>
public sealed class PostgresAuthorizationFactsReader(KpiDbContext context) : IAuthorizationFactsReader
{
    public async Task<AuthorizationFacts> LoadAsync(
        ActorIdentity actor,
        AuthorizationResource resource,
        DateTimeOffset effectiveAt,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var employee = actor.EmployeeId is null
            ? null
            : await context.OrganizationEmployees.SingleOrDefaultAsync(
                row => row.OrganizationId == actor.OrganizationId && row.Id == actor.EmployeeId.Value,
                cancellationToken);
        var instant = effectiveAt.ToUniversalTime();
        var employmentActive = employee is not null && employee.EmploymentFrom <= instant &&
            (employee.EmploymentTo is null || instant < employee.EmploymentTo.Value);
        var accountEnabled = employee is not null && string.Equals(employee.AccountStatus, "active", StringComparison.OrdinalIgnoreCase);

        return new AuthorizationFacts(
            actor,
            accountEnabled,
            employmentActive,
            new HashSet<KpiCapabilityId>(),
            Array.Empty<Guid>(),
            Array.Empty<string>(),
            ScopeMatches: false,
            BaselineApplicable: resource.BaselineId is null,
            SeparationOfDutySatisfied: true,
            DelegationValid: false,
            AuthorityEffective: false,
            ResourceRevisionCurrent: true);
    }
}
