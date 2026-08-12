using Kpi.Application.Authorization;
using System.Globalization;
using Xunit;

namespace Kpi.Application.Tests.Authorization;

public sealed class AuthorizationDecisionContractTests
{
    [Fact]
    public void Decision_denied_for_missing_capability_contains_stable_context()
    {
        var decision = AuthorizationDecision.Deny(
            AuthorizationDecisionReason.MissingCapability,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            new KpiCapabilityId("organization.structure.view"),
            new AuthorizationResource(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Organization", Guid.Empty, 3),
            DateTimeOffset.Parse("2026-01-01T00:00:00Z", CultureInfo.InvariantCulture));

        Assert.Equal(AuthorizationOutcome.Deny, decision.Outcome);
        Assert.Equal("missing_capability", decision.ReasonCode);
        Assert.Equal("organization.structure.view", decision.CapabilityId.Value);
        Assert.Equal(3, decision.ResourceRevision);
    }

    [Fact]
    public void Capability_id_is_trimmed_and_rejects_empty_values()
    {
        Assert.Equal("audit.timeline.view", new KpiCapabilityId(" audit.timeline.view ").Value);
        Assert.Throws<ArgumentException>(() => new KpiCapabilityId(" "));
    }
}
