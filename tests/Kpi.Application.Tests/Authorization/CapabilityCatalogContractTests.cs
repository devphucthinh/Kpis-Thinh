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
}
