namespace Kpi.Application.Authorization;

public interface IAuthorizationDecision
{
    Task<AuthorizationDecision> DecideAsync(
        ActorIdentity actor,
        KpiCapabilityId capability,
        AuthorizationResource resource,
        DateTimeOffset effectiveAt,
        RepresentedAuthority? representedAuthority,
        CancellationToken cancellationToken);
}
