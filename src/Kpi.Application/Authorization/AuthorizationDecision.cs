namespace Kpi.Application.Authorization;

public readonly record struct KpiCapabilityId
{
    public KpiCapabilityId(string value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
            throw new ArgumentException("Capability id is required.", nameof(value));

        Value = normalized;
    }

    public string Value { get; }
    public override string ToString() => Value;
}

public enum AuthorizationOutcome
{
    Allow,
    Deny
}

public static class AuthorizationDecisionReason
{
    public const string Allowed = "allowed";
    public const string OrganizationMismatch = "organization_mismatch";
    public const string AccountDisabled = "account_disabled";
    public const string AccountUnlinked = "account_unlinked";
    public const string EmploymentInactive = "employment_inactive";
    public const string MissingCapability = "missing_capability";
    public const string ScopeMismatch = "scope_mismatch";
    public const string AuthorityNotEffective = "authority_not_effective";
    public const string BootstrapCapabilityNotGranted = "bootstrap_capability_not_granted";
    public const string BootstrapExpired = "bootstrap_expired";
    public const string BootstrapDelegationForbidden = "bootstrap_delegation_forbidden";
    public const string DelegationNotEffective = "delegation_not_effective";
    public const string DelegationScopeMismatch = "delegation_scope_mismatch";
    public const string SeparationOfDuty = "separation_of_duty";
    public const string BaselineMissing = "baseline_missing";
    public const string ApproverUnresolved = "approver_unresolved";
}

public sealed record ActorIdentity(string SubjectId, Guid? EmployeeId, Guid OrganizationId);

public sealed record AuthorizationResource(
    Guid OrganizationId,
    string ResourceType,
    Guid ResourceId,
    long ResourceRevision,
    Guid? BaselineId = null,
    IReadOnlyList<Guid>? OrganizationUnitPath = null,
    IReadOnlyList<Guid>? ResponsibilityEmployeeIds = null,
    Guid? OwnerId = null,
    Guid? SubmitterId = null,
    Guid? BeneficiaryId = null);

public sealed record RepresentedAuthority(Guid ActorId, Guid DelegationId);

public sealed record AuthorizationDecision(
    AuthorizationOutcome Outcome,
    string ReasonCode,
    Guid OrganizationId,
    KpiCapabilityId CapabilityId,
    string ResourceType,
    Guid ResourceId,
    long ResourceRevision,
    DateTimeOffset EffectiveAt,
    IReadOnlyList<Guid> AssignmentIds,
    IReadOnlyList<string> ScopeEvidence,
    Guid? BootstrapPrincipalId = null,
    Guid? RepresentedAuthorityActorId = null,
    Guid? DelegationId = null)
{
    public static AuthorizationDecision Deny(
        string reasonCode,
        Guid organizationId,
        KpiCapabilityId capabilityId,
        AuthorizationResource resource,
        DateTimeOffset effectiveAt) =>
        new(AuthorizationOutcome.Deny, reasonCode, organizationId, capabilityId, resource.ResourceType,
            resource.ResourceId, resource.ResourceRevision, effectiveAt, Array.Empty<Guid>(), Array.Empty<string>());

    public static AuthorizationDecision Allow(
        Guid organizationId,
        KpiCapabilityId capabilityId,
        AuthorizationResource resource,
        DateTimeOffset effectiveAt,
        IReadOnlyList<Guid>? assignmentIds = null,
        IReadOnlyList<string>? scopeEvidence = null) =>
        new(AuthorizationOutcome.Allow, AuthorizationDecisionReason.Allowed, organizationId, capabilityId,
            resource.ResourceType, resource.ResourceId, resource.ResourceRevision, effectiveAt,
            assignmentIds ?? Array.Empty<Guid>(), scopeEvidence ?? Array.Empty<string>());
}

public sealed record AuthorizationFacts(
    ActorIdentity Actor,
    bool AccountEnabled,
    bool EmploymentActive,
    IReadOnlySet<KpiCapabilityId> Capabilities,
    IReadOnlyList<Guid> AssignmentIds,
    IReadOnlyList<string> ScopeEvidence,
    bool ScopeMatches = false,
    bool BaselineApplicable = false,
    bool SeparationOfDutySatisfied = false,
    bool DelegationValid = false);

public interface IAuthorizationFactsReader
{
    Task<AuthorizationFacts> LoadAsync(ActorIdentity actor, AuthorizationResource resource, DateTimeOffset effectiveAt, CancellationToken cancellationToken);
}
