namespace Kpi.Application.Authorization;

/// <summary>Product-owned atomic capabilities used by KPI command authorization.</summary>
public static class KpiAuthorizationCapabilities
{
    public static readonly KpiCapabilityId DefinitionCreate = new("kpi.definition.create");
    public static readonly KpiCapabilityId DefinitionEdit = new("kpi.definition.edit");
    public static readonly KpiCapabilityId DefinitionAdmin = new("kpi.definition.admin");
    public static readonly KpiCapabilityId VersionSubmit = new("kpi.version.submit");
    public static readonly KpiCapabilityId VersionReview = new("kpi.version.review");
    public static readonly KpiCapabilityId VersionActivate = new("kpi.version.activate");
}
