using Kpi.Domain.Formula;
using Kpi.Domain.Kpis;
using Xunit;

namespace Kpi.Domain.Tests.Kpis;

public sealed class KpiLifecycleTests
{
    [Fact]
    public void Version_requires_review_before_publish()
    {
        var definition = KpiDefinition.Create(Guid.NewGuid(), "REVENUE", "Revenue", "Revenue KPI", Guid.NewGuid());
        var version = definition.CreateVersion("v1", "First", "revenue", [FormulaVariableDefinition.Create("revenue", "Revenue", FormulaValueType.Decimal)], FormulaResultType.Decimal, "Initial");
        Assert.Throws<KpiDomainException>(() => version.Publish(DateTimeOffset.UtcNow));
        version.Submit();
        version.Approve("Approved");
        version.Publish(DateTimeOffset.UtcNow);
        Assert.Equal(KpiVersionStatus.Published, version.Status);
    }
}
