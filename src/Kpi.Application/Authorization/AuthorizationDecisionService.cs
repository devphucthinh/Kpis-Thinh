namespace Kpi.Application.Authorization;

public sealed class AuthorizationDecisionService(IAuthorizationFactsReader factsReader) : IAuthorizationDecision
{
    public async Task<AuthorizationDecision> DecideAsync(
        ActorIdentity actor,
        KpiCapabilityId capability,
        AuthorizationResource resource,
        DateTimeOffset effectiveAt,
        RepresentedAuthority? representedAuthority,
        CancellationToken cancellationToken)
    {
        if (actor.OrganizationId != resource.OrganizationId)
            return AuthorizationDecision.Deny(AuthorizationDecisionReason.OrganizationMismatch, actor.OrganizationId, capability, resource, effectiveAt);

        var facts = await factsReader.LoadAsync(actor, resource, effectiveAt, cancellationToken);
        if (!facts.AccountEnabled)
            return AuthorizationDecision.Deny(AuthorizationDecisionReason.AccountDisabled, actor.OrganizationId, capability, resource, effectiveAt);
        if (!facts.EmploymentActive)
            return AuthorizationDecision.Deny(AuthorizationDecisionReason.EmploymentInactive, actor.OrganizationId, capability, resource, effectiveAt);
        if (!facts.Capabilities.Contains(capability))
            return AuthorizationDecision.Deny(AuthorizationDecisionReason.MissingCapability, actor.OrganizationId, capability, resource, effectiveAt);
        if (!facts.ScopeMatches)
            return AuthorizationDecision.Deny(AuthorizationDecisionReason.ScopeMismatch, actor.OrganizationId, capability, resource, effectiveAt);
        if (resource.BaselineId is not null && !facts.BaselineApplicable)
            return AuthorizationDecision.Deny(AuthorizationDecisionReason.BaselineMissing, actor.OrganizationId, capability, resource, effectiveAt);
        if (!facts.SeparationOfDutySatisfied)
            return AuthorizationDecision.Deny(AuthorizationDecisionReason.SeparationOfDuty, actor.OrganizationId, capability, resource, effectiveAt);
        if (representedAuthority is not null && (!facts.DelegationValid || facts.Actor.EmployeeId is null))
            return AuthorizationDecision.Deny(AuthorizationDecisionReason.DelegationNotEffective, actor.OrganizationId, capability, resource, effectiveAt);

        return AuthorizationDecision.Allow(actor.OrganizationId, capability, resource, effectiveAt, facts.AssignmentIds, facts.ScopeEvidence);
    }
}
