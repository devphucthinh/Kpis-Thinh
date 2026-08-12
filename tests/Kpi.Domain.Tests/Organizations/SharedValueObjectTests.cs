using Kpi.Domain.Organizations;
using Xunit;

namespace Kpi.Domain.Tests.Organizations;

public sealed class SharedValueObjectTests
{
    [Fact]
    public void Revision_token_advances_only_for_a_new_revision()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RevisionToken(-1));
        var first = RevisionToken.Start;
        var second = first.Next();

        Assert.Equal(0, first.Revision);
        Assert.Equal(1, second.Revision);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Stable_status_and_capability_codes_are_non_empty_and_machine_safe()
    {
        Assert.Equal("active", StableOrganizationStatus.Active);
        Assert.Equal("organization.structure.view", StableCapabilityCodes.OrganizationStructureView);
        Assert.All(new[] { StableOrganizationStatus.Active, StableOrganizationStatus.Inactive, StableCapabilityCodes.OrganizationStructureView },
            value => Assert.DoesNotContain(' ', value));
    }
}
