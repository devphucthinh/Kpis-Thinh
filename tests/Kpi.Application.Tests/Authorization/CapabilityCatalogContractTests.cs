using Kpi.Application.Authorization;
using Xunit;

namespace Kpi.Application.Tests.Authorization;

public sealed class CapabilityCatalogContractTests
{
    [Fact]
    public void Default_catalog_contains_fixed_business_task_ids_without_duplicates()
    {
        var catalog = CapabilityCatalog.Default;

        Assert.Contains(catalog.All, item => item.Id.Value == "organization.structure.view");
        Assert.Contains(catalog.All, item => item.Id.Value == "approval.route.activate");
        Assert.Contains(catalog.All, item => item.Id.Value == "audit.timeline.view");
        Assert.Equal(catalog.All.Count, catalog.All.Select(item => item.Id.Value).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Unknown_capability_is_not_resolved_from_the_catalog()
    {
        Assert.False(CapabilityCatalog.Default.TryGet(new KpiCapabilityId("custom.capability"), out _));
    }

    [Fact(DisplayName = "FR-017 FR-018 fixed catalog and stable denial codes are complete")]
    public void Initial_catalog_and_denial_code_inventory_are_complete()
    {
        var expectedCapabilityIds = new[]
        {
            "organization.structure.view", "organization.structure.edit", "organization.baseline.submit", "organization.baseline.approve",
            "workforce.employee.view", "workforce.employee.manage", "workforce.position.manage",
            "security.custom-role.view", "security.custom-role.manage", "security.role-assignment.request", "security.role-assignment.approve",
            "approval.group.manage", "approval.route.manage", "approval.route.submit", "approval.route.approve", "approval.route.activate",
            "approval.delegation.request", "approval.delegation.approve", "approval.decision.make",
            "audit.timeline.view", "audit.organization.view"
        };

        Assert.Equal(expectedCapabilityIds, CapabilityCatalog.Default.All.Select(item => item.Id.Value));
        Assert.DoesNotContain(CapabilityCatalog.Default.All, item => item.Id.Value == "security.policy.weakens-system-floor");
        Assert.Contains("missing_capability", AuthorizationDecisionReason.All);
        Assert.Contains("delegation_scope_mismatch", AuthorizationDecisionReason.All);
        Assert.Contains("resource_revision_stale", AuthorizationDecisionReason.All);
    }
}
