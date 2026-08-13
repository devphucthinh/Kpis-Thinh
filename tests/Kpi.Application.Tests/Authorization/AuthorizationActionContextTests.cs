using Kpi.Application.Authorization;
using Xunit;

namespace Kpi.Application.Tests.Authorization;

public sealed class AuthorizationActionContextTests
{
    [Fact(DisplayName = "FR-049 action-local authorization memoization never crosses command contexts")]
    public async Task Identical_checks_are_memoized_inside_one_action_but_reloaded_for_the_next()
    {
        var actor = new ActorIdentity("employee-1", Guid.NewGuid(), Guid.NewGuid());
        var resource = new AuthorizationResource(actor.OrganizationId, "KpiPlan", Guid.NewGuid(), 1);
        var reader = new CountingFactsReader(actor);
        var service = new AuthorizationDecisionService(reader);
        var capability = new KpiCapabilityId("organization.structure.view");
        var effectiveAt = DateTimeOffset.UtcNow;

        var firstAction = new AuthorizationActionContext(service);
        await firstAction.DecideAsync(actor, capability, resource, effectiveAt, null, TestContext.Current.CancellationToken);
        await firstAction.DecideAsync(actor, capability, resource, effectiveAt, null, TestContext.Current.CancellationToken);
        var secondAction = new AuthorizationActionContext(service);
        await secondAction.DecideAsync(actor, capability, resource, effectiveAt, null, TestContext.Current.CancellationToken);

        Assert.Equal(2, reader.LoadCount);
    }

    private sealed class CountingFactsReader(ActorIdentity actor) : IAuthorizationFactsReader
    {
        public int LoadCount { get; private set; }

        public Task<AuthorizationFacts> LoadAsync(ActorIdentity requestedActor, AuthorizationResource resource, DateTimeOffset effectiveAt, CancellationToken cancellationToken)
        {
            LoadCount++;
            return Task.FromResult(new AuthorizationFacts(actor, true, true,
                new HashSet<KpiCapabilityId> { new("organization.structure.view") }, [], ["Organization"],
                ScopeMatches: true, BaselineApplicable: true, SeparationOfDutySatisfied: true));
        }
    }
}
