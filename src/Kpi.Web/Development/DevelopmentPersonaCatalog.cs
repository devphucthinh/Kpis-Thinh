using Kpi.Application.Common;

namespace Kpi.Web.Development;

/// <summary>Six named Development personas used for safe workflow demonstration.</summary>
public static class DevelopmentPersonaCatalog
{
    public static IReadOnlyDictionary<string, KpiCapability> Personas { get; } = new Dictionary<string, KpiCapability>(StringComparer.OrdinalIgnoreCase)
    {
        ["creator"] = KpiCapability.CreateKpi | KpiCapability.EditDraft,
        ["approver"] = KpiCapability.ReviewKpi | KpiCapability.ApprovePeriod,
        ["planner"] = KpiCapability.PlanPeriod,
        ["evaluator"] = KpiCapability.Evaluate,
        ["admin"] = KpiCapability.Administrator | KpiCapability.AuditRead,
        ["observer"] = KpiCapability.AuditRead
    };
}
