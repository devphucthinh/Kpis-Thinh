using Kpi.Domain.Formula;
using Kpi.Domain.Periods;

namespace Kpi.Domain.Kpis;

/// <summary>Versioned KPI content and governed lifecycle.</summary>
public sealed class KpiVersion
{
    private KpiVersion(Guid id, int number, string name, string description, FormulaDocument formula, IReadOnlyList<FormulaVariableDefinition> variables, FormulaResultType resultType, string changeSummary, Guid? predecessorVersionId, KpiCadence cadence)
    { Id = id; VersionNumber = number; Name = name; Description = description; Formula = formula; Variables = variables; DeclaredResultType = resultType; ChangeSummary = changeSummary; PredecessorVersionId = predecessorVersionId; Cadence = cadence; }
    public Guid Id { get; }
    public int VersionNumber { get; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public FormulaDocument Formula { get; private set; }
    public IReadOnlyList<FormulaVariableDefinition> Variables { get; private set; }
    public FormulaResultType DeclaredResultType { get; }
    public string ChangeSummary { get; }
    public Guid? PredecessorVersionId { get; }
    public KpiCadence Cadence { get; private set; }
    public KpiVersionStatus Status { get; private set; } = KpiVersionStatus.Draft;
    public DateTimeOffset? EffectiveFrom { get; private set; }
    public DateTimeOffset? EffectiveTo { get; private set; }
    public string? ReviewComment { get; private set; }
    public long Revision { get; private set; }

    public static KpiVersion CreateDraft(int number, string name, string description, string source, IReadOnlyList<FormulaVariableDefinition> variables, FormulaResultType resultType, string changeSummary, Guid? predecessorVersionId = null, KpiCadence cadence = KpiCadence.Monthly)
    {
        var compilation = FormulaEngine.Compile(source, variables, resultType);
        if (!compilation.IsSuccess) throw new KpiDomainException(string.Join("; ", compilation.Diagnostics.Select(x => x.ToString())));
        return new(Guid.NewGuid(), number, Required(name, "name"), Required(description, "description"), compilation.Formula!, variables.OrderBy(x => x.DisplayOrder).ToArray(), resultType, Required(changeSummary, "changeSummary"), predecessorVersionId, cadence);
    }

    public void UpdateDraft(string name, string description, string source, IReadOnlyList<FormulaVariableDefinition> variables)
    {
        Ensure(KpiVersionStatus.Draft); var compilation = FormulaEngine.Compile(source, variables, DeclaredResultType);
        if (!compilation.IsSuccess) throw new KpiDomainException(string.Join("; ", compilation.Diagnostics.Select(x => x.ToString())));
        Name = Required(name, "name"); Description = Required(description, "description"); Formula = compilation.Formula!; Variables = variables.OrderBy(x => x.DisplayOrder).ToArray(); Revision++;
    }
    public void Submit() { Ensure(KpiVersionStatus.Draft); Status = KpiVersionStatus.InReview; Revision++; }
    public void Approve(string comment) { Ensure(KpiVersionStatus.InReview); ReviewComment = Required(comment, "comment"); Status = KpiVersionStatus.Approved; Revision++; }
    public void Reject(string comment) { Ensure(KpiVersionStatus.InReview); ReviewComment = Required(comment, "comment"); Status = KpiVersionStatus.Rejected; Revision++; }
    public void ReturnToDraft() { Ensure(KpiVersionStatus.Rejected); Status = KpiVersionStatus.Draft; Revision++; }
    public void Publish(DateTimeOffset effectiveFrom) { Ensure(KpiVersionStatus.Approved); EffectiveFrom = effectiveFrom; Status = KpiVersionStatus.Published; Revision++; }
    public void Retire(DateTimeOffset at) { if (Status != KpiVersionStatus.Published) throw new KpiDomainException("Only Published versions can retire."); EffectiveTo = at; Status = KpiVersionStatus.Retired; Revision++; }
    public void SetEffectiveTo(DateTimeOffset effectiveTo)
    {
        if (EffectiveFrom is null || effectiveTo <= EffectiveFrom) throw new KpiDomainException("Effective end must be after effective start.");
        if (EffectiveTo is not null && effectiveTo > EffectiveTo) throw new KpiDomainException("Effective range cannot be extended after it was closed.");
        EffectiveTo = effectiveTo; Revision++;
    }

    public static KpiVersion Rehydrate(Guid id, int number, string name, string description, FormulaDocument formula, IReadOnlyList<FormulaVariableDefinition> variables, FormulaResultType resultType, string changeSummary, Guid? predecessorVersionId, KpiCadence cadence, KpiVersionStatus status, DateTimeOffset? effectiveFrom, DateTimeOffset? effectiveTo, string? reviewComment, long revision = 0)
    {
        return new(id, number, Required(name, "name"), Required(description, "description"), formula, variables.OrderBy(x => x.DisplayOrder).ToArray(), resultType, Required(changeSummary, "changeSummary"), predecessorVersionId, cadence)
        {
            Status = status,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            ReviewComment = reviewComment, Revision = revision
        };
    }

    private void Ensure(KpiVersionStatus expected) { if (Status != expected) throw new KpiDomainException($"Version must be {expected}; it is {Status}."); }
    private static string Required(string value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"{name} is required.", name) : value.Trim();
}

public enum KpiVersionStatus { Draft, InReview, Rejected, Approved, Published, Retired }

/// <summary>Expected domain validation error.</summary>
public sealed class KpiDomainException(string message) : Exception(message);
