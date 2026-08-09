namespace Kpi.Domain.Kpis;

/// <summary>Stable KPI identity with an ordered version stream.</summary>
public sealed class KpiDefinition
{
    private KpiDefinition(Guid id, Guid organizationId, KpiCode code, string name, string description, Guid ownerId)
    { Id = id; OrganizationId = organizationId; Code = code; Name = name; Description = description; OwnerId = ownerId; }
    public Guid Id { get; }
    public Guid OrganizationId { get; }
    public KpiCode Code { get; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Guid OwnerId { get; private set; }
    public bool Archived { get; private set; }
    public long Revision { get; private set; }
    public List<KpiVersion> Versions { get; } = [];

    public static KpiDefinition Create(Guid organizationId, string code, string name, string description, Guid ownerId) => new(Guid.NewGuid(), organizationId, KpiCode.Parse(code), Required(name), Required(description), ownerId);
    public KpiVersion CreateVersion(string name, string description, string source, IReadOnlyList<Formula.FormulaVariableDefinition> variables, Formula.FormulaResultType resultType, string changeSummary)
        => CreateVersion(name, description, source, variables, resultType, changeSummary, Kpi.Domain.Periods.KpiCadence.Monthly);
    public KpiVersion CreateVersion(string name, string description, string source, IReadOnlyList<Formula.FormulaVariableDefinition> variables, Formula.FormulaResultType resultType, string changeSummary, Kpi.Domain.Periods.KpiCadence cadence)
    { var version = KpiVersion.CreateDraft(Versions.Count + 1, name, description, source, variables, resultType, changeSummary, cadence: cadence); Versions.Add(version); Revision++; return version; }
    public void UpdateMetadata(string name, string description) { if (Archived) throw new KpiDomainException("Archived KPI cannot be edited."); Name = Required(name); Description = Required(description); }
    public void Archive() { Archived = true; Revision++; }
    public void Restore() { Archived = false; Revision++; }
    public void TransferOwnership(Guid ownerId) { OwnerId = ownerId; Revision++; }
    public KpiVersion CloneVersion(KpiVersion source, string changeSummary) => CreateVersion(source.Name, source.Description, source.Formula.Source, source.Variables, source.DeclaredResultType, changeSummary, source, source.Cadence);
    public void DeleteEligibleDraft(KpiVersion version) { if (Versions.Count != 1 || version.Status != KpiVersionStatus.Draft) throw new KpiDomainException("Only an unused single Draft Version can be deleted."); Versions.Remove(version); }
    private KpiVersion CreateVersion(string name, string description, string source, IReadOnlyList<Formula.FormulaVariableDefinition> variables, Formula.FormulaResultType resultType, string changeSummary, KpiVersion? predecessor, Kpi.Domain.Periods.KpiCadence cadence = Kpi.Domain.Periods.KpiCadence.Monthly)
    { var version = KpiVersion.CreateDraft(Versions.Count + 1, name, description, source, variables, resultType, changeSummary, predecessor?.Id, cadence); Versions.Add(version); Revision++; return version; }

    public static KpiDefinition Rehydrate(Guid id, Guid organizationId, string code, string name, string description, Guid ownerId, bool archived, long revision, IEnumerable<KpiVersion> versions)
    {
        var definition = new KpiDefinition(id, organizationId, KpiCode.Parse(code), Required(name), Required(description), ownerId) { Archived = archived, Revision = revision };
        definition.Versions.AddRange(versions.OrderBy(x => x.VersionNumber));
        return definition;
    }

    private static string Required(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value is required.") : value.Trim();
}
