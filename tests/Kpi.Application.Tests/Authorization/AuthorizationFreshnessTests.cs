using Kpi.Application.Authorization;
using Xunit;

namespace Kpi.Application.Tests.Authorization;

public sealed class AuthorizationFreshnessTests
{
    [Fact]
    public async Task Each_governed_action_reloads_facts_and_does_not_reuse_a_prior_decision()
    {
        var actor = new ActorIdentity("employee-1", Guid.NewGuid(), Guid.NewGuid());
        var resource = new AuthorizationResource(actor.OrganizationId, "KpiPlan", Guid.NewGuid(), 1);
        var reader = new MutableFactsReader(actor, accountEnabled: true);
        var service = new AuthorizationDecisionService(reader);
        var capability = new KpiCapabilityId("organization.structure.view");

        var allowed = await service.DecideAsync(actor, capability, resource, DateTimeOffset.UtcNow, null, CancellationToken.None);
        reader.AccountEnabled = false;
        var denied = await service.DecideAsync(actor, capability, resource, DateTimeOffset.UtcNow, null, CancellationToken.None);

        Assert.Equal(AuthorizationOutcome.Allow, allowed.Outcome);
        Assert.Equal(AuthorizationDecisionReason.AccountDisabled, denied.ReasonCode);
        Assert.Equal(2, reader.LoadCount);
    }

    [Fact]
    public async Task Missing_scope_evidence_is_denied_closed()
    {
        var actor = new ActorIdentity("employee-1", Guid.NewGuid(), Guid.NewGuid());
        var resource = new AuthorizationResource(actor.OrganizationId, "KpiPlan", Guid.NewGuid(), 1);
        var service = new AuthorizationDecisionService(new MutableFactsReader(actor, accountEnabled: true) { ScopeMatches = false });

        var decision = await service.DecideAsync(actor, new KpiCapabilityId("organization.structure.view"), resource, DateTimeOffset.UtcNow, null, CancellationToken.None);

        Assert.Equal(AuthorizationDecisionReason.ScopeMismatch, decision.ReasonCode);
    }

    private sealed class MutableFactsReader(ActorIdentity actor, bool accountEnabled) : IAuthorizationFactsReader
    {
        public bool AccountEnabled { get; set; } = accountEnabled;
        public bool ScopeMatches { get; set; } = true;
        public int LoadCount { get; private set; }

        public Task<AuthorizationFacts> LoadAsync(ActorIdentity requestedActor, AuthorizationResource resource, DateTimeOffset effectiveAt, CancellationToken cancellationToken)
        {
            LoadCount++;
            IReadOnlySet<KpiCapabilityId> capabilities = new HashSet<KpiCapabilityId> { new("organization.structure.view") };
            return Task.FromResult(new AuthorizationFacts(actor, AccountEnabled, true, capabilities, [], ["organization"], ScopeMatches, true, true));
        }
    }
}
