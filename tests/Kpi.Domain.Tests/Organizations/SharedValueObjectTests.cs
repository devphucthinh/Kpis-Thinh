using Kpi.Domain.Organizations;
using Xunit;

namespace Kpi.Domain.Tests.Organizations;

public sealed class SharedValueObjectTests
{
    [Fact(DisplayName = "FR-001 FR-002 revision tokens are monotonic and reject invalid values")]
    public void Revision_token_advances_only_for_a_new_revision()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RevisionToken(-1));
        var first = RevisionToken.Start;
        var second = first.Next();

        Assert.Equal(0, first.Revision);
        Assert.Equal(1, second.Revision);
        Assert.NotEqual(first, second);
    }

    [Fact(DisplayName = "FR-017 stable status and capability codes are machine-safe")]
    public void Stable_status_and_capability_codes_are_non_empty_and_machine_safe()
    {
        Assert.Equal("active", StableOrganizationStatus.Active);
        Assert.Equal("organization.structure.view", StableCapabilityCodes.OrganizationStructureView);
        Assert.All(new[] { StableOrganizationStatus.Active, StableOrganizationStatus.Inactive, StableCapabilityCodes.OrganizationStructureView },
            value => Assert.DoesNotContain(' ', value));
    }

    [Fact(DisplayName = "FR-001 FR-002 effective intervals are UTC half-open values")]
    public void Effective_interval_is_half_open_and_rejects_invalid_end()
    {
        var from = new DateTimeOffset(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddDays(1);
        var interval = new EffectiveInterval(from, to);

        Assert.True(interval.Contains(from));
        Assert.False(interval.Contains(to));
        Assert.Throws<ArgumentException>(() => new EffectiveInterval(from, from));
    }

    [Fact(DisplayName = "FR-001 FR-002 organization identity is normalized and status is explicit")]
    public void Organization_identity_requires_scope_and_exposes_explicit_status()
    {
        var organization = Organization.Create(" org-1 ", " Example ", " UTC ");

        Assert.Equal("org-1", organization.Code);
        Assert.Equal("Example", organization.Name);
        Assert.Equal("UTC", organization.TimeZoneId);
        Assert.Equal(OrganizationStatus.Active, organization.Status);

        organization.Deactivate();
        Assert.Equal(OrganizationStatus.Inactive, organization.Status);
        Assert.Throws<ArgumentException>(() => Organization.Create(" ", "Example"));
    }
}
