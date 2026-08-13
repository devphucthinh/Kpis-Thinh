using Kpi.Application.Authorization;
using Kpi.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kpi.Infrastructure.Postgres.Authorization;

/// <summary>
/// Loads the committed workforce, role, scope, baseline, and delegation facts
/// for one authorization action. Every query is scoped to the actor's
/// Organization and effective instant; no facts are retained between actions.
/// </summary>
public sealed class PostgresAuthorizationFactsReader(KpiDbContext context) : IAuthorizationFactsReader
{
    public async Task<AuthorizationFacts> LoadAsync(
        ActorIdentity actor,
        AuthorizationResource resource,
        DateTimeOffset effectiveAt,
        CancellationToken cancellationToken)
        => await LoadAsync(actor, resource, effectiveAt, null, null, cancellationToken);

    public async Task<AuthorizationFacts> LoadAsync(
        ActorIdentity actor,
        AuthorizationResource resource,
        DateTimeOffset effectiveAt,
        RepresentedAuthority? representedAuthority,
        KpiCapabilityId? capability,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var actorEmployee = actor.EmployeeId is null
            ? null
            : await context.OrganizationEmployees.SingleOrDefaultAsync(
                row => row.OrganizationId == actor.OrganizationId && row.Id == actor.EmployeeId.Value,
                cancellationToken);
        var instant = effectiveAt.ToUniversalTime();
        var employmentActive = actorEmployee is not null && actorEmployee.EmploymentFrom <= instant &&
            (actorEmployee.EmploymentTo is null || instant < actorEmployee.EmploymentTo.Value);
        var accountEnabled = actorEmployee is not null && string.Equals(actorEmployee.AccountStatus, "active", StringComparison.OrdinalIgnoreCase);
        var authorityEmployeeId = representedAuthority?.ActorId ?? actor.EmployeeId;
        var authorityEmployee = authorityEmployeeId is null || authorityEmployeeId == actor.EmployeeId
            ? actorEmployee
            : await context.OrganizationEmployees.AsNoTracking().SingleOrDefaultAsync(
                row => row.OrganizationId == actor.OrganizationId && row.Id == authorityEmployeeId.Value,
                cancellationToken);
        var authorityCurrent = authorityEmployee is not null && authorityEmployee.EmploymentFrom <= instant &&
            (authorityEmployee.EmploymentTo is null || instant < authorityEmployee.EmploymentTo.Value) &&
            string.Equals(authorityEmployee.AccountStatus, "active", StringComparison.OrdinalIgnoreCase);
        var assignments = authorityEmployeeId is null
            ? []
            : await context.RoleAssignments.AsNoTracking()
                .Where(row => row.OrganizationId == actor.OrganizationId && row.EmployeeId == authorityEmployeeId.Value &&
                    (row.Status == "Effective" || row.Status == "Scheduled") && row.EffectiveFrom <= instant &&
                    (row.EffectiveTo == null || instant < row.EffectiveTo))
                .ToListAsync(cancellationToken);
        var roleVersionIds = assignments.Select(row => row.RoleVersionId).Distinct().ToArray();
        var activeRoleVersionIds = roleVersionIds.Length == 0
            ? []
            : await context.CustomKpiRoleVersions.AsNoTracking()
                .Where(row => row.OrganizationId == actor.OrganizationId && roleVersionIds.Contains(row.Id) && row.Status == "Active")
                .Select(row => row.Id)
                .ToListAsync(cancellationToken);
        assignments = assignments.Where(row => activeRoleVersionIds.Contains(row.RoleVersionId)).ToList();
        var capabilityIds = activeRoleVersionIds.Count == 0
            ? []
            : await context.CustomKpiRoleCapabilities.AsNoTracking()
                .Where(row => row.OrganizationId == actor.OrganizationId && activeRoleVersionIds.Contains(row.RoleVersionId))
                .Select(row => row.CapabilityId)
                .Distinct()
                .ToListAsync(cancellationToken);
        var capabilities = capabilityIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => new KpiCapabilityId(id))
            .Where(id => CapabilityCatalog.Default.TryGet(id, out _))
            .ToHashSet();
        var baselineApplicable = true;
        if (resource.BaselineId is not null)
        {
            var approvedBaseline = await context.OrganizationBaselines.AsNoTracking().AnyAsync(row =>
                row.OrganizationId == actor.OrganizationId && row.Id == resource.BaselineId && row.Status == "Approved", cancellationToken);
            var applicableSegment = await context.BaselineApplicabilitySegments.AsNoTracking().AnyAsync(row =>
                row.OrganizationId == actor.OrganizationId && row.BaselineId == resource.BaselineId && row.EffectiveFrom <= instant &&
                (row.EffectiveTo == null || instant < row.EffectiveTo), cancellationToken);
            baselineApplicable = approvedBaseline && applicableSegment;
        }
        var scopeMatches = assignments.Any(row => ScopeMatches(row.ScopeKind, row.ScopeTargetId, row.BaselineId, authorityEmployeeId, resource, baselineApplicable));
        var delegationValid = false;
        var delegationScopeMatches = false;
        if (representedAuthority is not null && actor.EmployeeId is not null)
        {
            var delegation = await context.ApprovalDelegations.AsNoTracking().SingleOrDefaultAsync(row =>
                row.OrganizationId == actor.OrganizationId && row.Id == representedAuthority.DelegationId &&
                row.OriginalActorId == representedAuthority.ActorId && row.DelegateActorId == actor.EmployeeId.Value &&
                row.Status == "Active" && row.EffectiveFrom <= instant && (row.EffectiveTo == null || instant < row.EffectiveTo), cancellationToken);
            delegationValid = delegation is not null && (capability is null || string.Equals(delegation.CapabilityId, capability.Value.Value, StringComparison.Ordinal));
            delegationScopeMatches = delegation is not null && ScopeMatches(delegation.ScopeKind, delegation.ScopeTargetId, delegation.BaselineId, authorityEmployeeId, resource, baselineApplicable);
        }

        return new AuthorizationFacts(
            actor,
            accountEnabled,
            employmentActive,
            capabilities,
            assignments.Select(row => row.Id).ToArray(),
            assignments.Where(row => ScopeMatches(row.ScopeKind, row.ScopeTargetId, row.BaselineId, authorityEmployeeId, resource, baselineApplicable)).Select(row => $"{row.ScopeKind}:{row.ScopeTargetId}").ToArray(),
            ScopeMatches: scopeMatches,
            BaselineApplicable: baselineApplicable,
            SeparationOfDutySatisfied: true,
            DelegationValid: delegationValid,
            AuthorityEffective: assignments.Count > 0 && authorityCurrent,
            DelegationScopeMatches: delegationScopeMatches,
            RepresentedAuthorityActorId: representedAuthority?.ActorId,
            DelegationId: representedAuthority?.DelegationId,
            ResourceRevisionCurrent: true);
    }

    private static bool ScopeMatches(string kind, Guid? targetId, Guid? baselineId, Guid? authorityEmployeeId, AuthorizationResource resource, bool baselineApplicable) => kind switch
    {
        "Organization" => true,
        "UnitSubtree" => baselineApplicable && resource.BaselineId == baselineId && targetId is not null && resource.OrganizationUnitPath?.Contains(targetId.Value) == true,
        "Assigned" => authorityEmployeeId is not null && resource.ResponsibilityEmployeeIds?.Contains(authorityEmployeeId.Value) == true,
        "Self" => authorityEmployeeId is not null && (resource.OwnerId == authorityEmployeeId || resource.SubmitterId == authorityEmployeeId || resource.BeneficiaryId == authorityEmployeeId),
        _ => false
    };
}
