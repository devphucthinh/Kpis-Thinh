namespace Kpi.Application.Authorization;

/// <summary>Memoizes authorization decisions only for the lifetime of one application action.</summary>
public sealed class AuthorizationActionContext(IAuthorizationDecision decision)
{
    private readonly Dictionary<AuthorizationCacheKey, Task<AuthorizationDecision>> cache = [];

    public Task<AuthorizationDecision> DecideAsync(
        ActorIdentity actor,
        KpiCapabilityId capability,
        AuthorizationResource resource,
        DateTimeOffset effectiveAt,
        RepresentedAuthority? representedAuthority,
        CancellationToken cancellationToken)
    {
        var key = new AuthorizationCacheKey(
            actor.SubjectId,
            actor.EmployeeId,
            actor.OrganizationId,
            capability.Value,
            resource.ResourceType,
            resource.ResourceId,
            resource.ResourceRevision,
            resource.BaselineId,
            string.Join(',', resource.OrganizationUnitPath ?? []),
            string.Join(',', resource.ResponsibilityEmployeeIds ?? []),
            resource.OwnerId,
            resource.SubmitterId,
            resource.BeneficiaryId,
            effectiveAt,
            representedAuthority?.ActorId,
            representedAuthority?.DelegationId);
        if (cache.TryGetValue(key, out var existing)) return existing;

        var pending = LoadAsync(key, actor, capability, resource, effectiveAt, representedAuthority, cancellationToken);
        cache[key] = pending;
        return pending;
    }

    private async Task<AuthorizationDecision> LoadAsync(
        AuthorizationCacheKey key,
        ActorIdentity actor,
        KpiCapabilityId capability,
        AuthorizationResource resource,
        DateTimeOffset effectiveAt,
        RepresentedAuthority? representedAuthority,
        CancellationToken cancellationToken)
    {
        try { return await decision.DecideAsync(actor, capability, resource, effectiveAt, representedAuthority, cancellationToken); }
        catch { cache.Remove(key); throw; }
    }

    private sealed record AuthorizationCacheKey(
        string SubjectId,
        Guid? EmployeeId,
        Guid OrganizationId,
        string Capability,
        string ResourceType,
        Guid ResourceId,
        long ResourceRevision,
        Guid? BaselineId,
        string OrganizationUnitPath,
        string ResponsibilityEmployees,
        Guid? OwnerId,
        Guid? SubmitterId,
        Guid? BeneficiaryId,
        DateTimeOffset EffectiveAt,
        Guid? RepresentedAuthorityActorId,
        Guid? DelegationId);
}
