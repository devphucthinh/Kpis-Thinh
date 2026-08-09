using Kpi.Application;

namespace Kpi.Web.Configuration;

/// <summary>Process-local cache used only as an application seam over durable persistence.</summary>
public sealed class KpiRuntimeState
{
    public InMemoryKpiStore Store { get; } = new();
}
