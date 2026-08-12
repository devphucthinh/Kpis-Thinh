namespace Kpi.Application.Authorization;

public sealed record CapabilityDefinition(
    KpiCapabilityId Id,
    string BusinessArea,
    string DisplayName,
    bool RequiresIndependentApproval,
    IReadOnlySet<string> AllowedScopeKinds);

public sealed class CapabilityCatalog
{
    private readonly IReadOnlyDictionary<string, CapabilityDefinition> _definitions;

    private CapabilityCatalog(IEnumerable<CapabilityDefinition> definitions)
    {
        var items = definitions.ToArray();
        if (items.Length == 0)
            throw new ArgumentException("Capability catalog cannot be empty.", nameof(definitions));

        _definitions = items.ToDictionary(item => item.Id.Value, StringComparer.Ordinal);
        All = items;
    }

    public IReadOnlyList<CapabilityDefinition> All { get; }

    public static CapabilityCatalog Default { get; } = new(CreateDefaultDefinitions());

    public bool TryGet(KpiCapabilityId id, out CapabilityDefinition? definition) =>
        _definitions.TryGetValue(id.Value, out definition);

    private static IEnumerable<CapabilityDefinition> CreateDefaultDefinitions()
    {
        yield return Definition("organization", "organization.structure.view", "View organization structure", false, "Organization", "UnitSubtree", "Self");
        yield return Definition("organization", "organization.structure.edit", "Edit organization structure", true, "Organization");
        yield return Definition("organization", "organization.baseline.submit", "Submit structure baseline", true, "Organization");
        yield return Definition("organization", "organization.baseline.approve", "Approve structure baseline", true, "Organization");
        yield return Definition("workforce", "workforce.employee.view", "View employees", false, "Organization", "UnitSubtree");
        yield return Definition("workforce", "workforce.employee.manage", "Manage employees", true, "Organization", "UnitSubtree");
        yield return Definition("workforce", "workforce.position.manage", "Manage positions", true, "Organization", "UnitSubtree");
        yield return Definition("security", "security.custom-role.view", "View custom roles", false, "Organization");
        yield return Definition("security", "security.custom-role.manage", "Manage custom roles", true, "Organization");
        yield return Definition("security", "security.role-assignment.request", "Request role assignment", false, "Self", "Assigned", "UnitSubtree", "Organization");
        yield return Definition("security", "security.role-assignment.approve", "Approve role assignment", true, "Organization", "UnitSubtree");
        yield return Definition("approval", "approval.group.manage", "Manage approval groups", true, "Organization");
        yield return Definition("approval", "approval.route.manage", "Manage approval routes", true, "Organization");
        yield return Definition("approval", "approval.route.submit", "Submit approval route", true, "Organization");
        yield return Definition("approval", "approval.route.approve", "Approve approval route", true, "Organization");
        yield return Definition("approval", "approval.route.activate", "Activate approval route", true, "Organization");
        yield return Definition("approval", "approval.delegation.request", "Request approval delegation", true, "Self", "Organization");
        yield return Definition("approval", "approval.delegation.approve", "Approve approval delegation", true, "Organization");
        yield return Definition("approval", "approval.decision.make", "Make approval decision", true, "Organization", "UnitSubtree");
        yield return Definition("audit", "audit.timeline.view", "View audit timeline", false, "Organization", "UnitSubtree", "Self");
        yield return Definition("audit", "audit.organization.view", "View organization audit", false, "Organization");
    }

    private static CapabilityDefinition Definition(string area, string id, string displayName, bool approval, params string[] scopes) =>
        new(new KpiCapabilityId(id), area, displayName, approval, new HashSet<string>(scopes, StringComparer.Ordinal));
}
