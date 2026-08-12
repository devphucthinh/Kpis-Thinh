using Kpi.Application.Organizations;
using Kpi.Application.Persistence;
using Xunit;

namespace Kpi.Application.Tests.Organizations;

public sealed class PlatformIdentityContractTests
{
    [Fact]
    public async Task Development_adapter_returns_explicit_platform_actor_for_known_subject()
    {
        var adapter = new DevelopmentPlatformIdentityAdapter();

        var actor = await adapter.ResolveAsync("platform-admin", CancellationToken.None);

        Assert.NotNull(actor);
        Assert.Equal("platform-admin", actor.SubjectId);
        Assert.Contains("platform.organization.provision", actor.CapabilityIds);
        Assert.Contains("platform.bootstrap.recover", actor.CapabilityIds);
    }

    [Fact]
    public async Task Development_adapter_does_not_fallback_for_unknown_subject()
    {
        var adapter = new DevelopmentPlatformIdentityAdapter();

        var actor = await adapter.ResolveAsync("unknown", CancellationToken.None);

        Assert.Null(actor);
    }
}
