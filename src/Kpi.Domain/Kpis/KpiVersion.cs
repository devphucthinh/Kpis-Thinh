using Kpi.Domain.Formula;

namespace Kpi.Domain.Kpis;

/// <summary>Versioned KPI content and governed lifecycle.</summary>
public sealed class KpiVersion
{
    private KpiVersion(Guid id, int number, string name, string description, FormulaDocument formula, IReadOnlyList<FormulaVariableDefinition> variables, FormulaResultType resultType, string changeSummary)
    { Id = id; VersionNumber = number; Name = name; Description = description; Formula = formula; Variables = variables; DeclaredResultType = resultType; ChangeSummary = changeSummary; }
    public Guid Id { get; }
    public int VersionNumber { get; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public FormulaDocument Formula { get; private set; }
    public IReadOnlyList<FormulaVariableDefinition> Variables { get; private set; }
    public FormulaResultType DeclaredResultType { get; }
    public string ChangeSummary { get; }
    public KpiVersionStatus Status { get; private set; } = KpiVersionStatus.Draft;
    public DateTimeOffset? EffectiveFrom { get; private set; }
    public DateTimeOffset? EffectiveTo { get; private set; }
    public string? ReviewComment { get; private set; }

    public static KpiVersion CreateDraft(int number, string name, string description, string source, IReadOnlyList<FormulaVariableDefinition> variables, FormulaResultType resultType, string changeSummary)
    {
        var compilation = FormulaEngine.Compile(source, variables, resultType);
        if (!compilation.IsSuccess) throw new KpiDomainException(string.Join("; ", compilation.Diagnostics.Select(x => x.ToString())));
        return new(Guid.NewGuid(), number, Required(name, "name"), Required(description, "description"), compilation.Formula!, variables.OrderBy(x => x.DisplayOrder).ToArray(), resultType, Required(changeSummary, "changeSummary"));
    }

    public void UpdateDraft(string name, string description, string source, IReadOnlyList<FormulaVariableDefinition> variables)
    {
        Ensure(KpiVersionStatus.Draft); var compilation = FormulaEngine.Compile(source, variables, DeclaredResultType);
        if (!compilation.IsSuccess) throw new KpiDomainException(string.Join("; ", compilation.Diagnostics.Select(x => x.ToString())));
        Name = Required(name, "name"); Description = Required(description, "description"); Formula = compilation.Formula!; Variables = variables.OrderBy(x => x.DisplayOrder).ToArray();
    }
    public void Submit() { Ensure(KpiVersionStatus.Draft); Status = KpiVersionStatus.InReview; }
    public void Approve(string comment) { Ensure(KpiVersionStatus.InReview); ReviewComment = Required(comment, "comment"); Status = KpiVersionStatus.Approved; }
    public void Reject(string comment) { Ensure(KpiVersionStatus.InReview); ReviewComment = Required(comment, "comment"); Status = KpiVersionStatus.Rejected; }
    public void ReturnToDraft() { Ensure(KpiVersionStatus.Rejected); Status = KpiVersionStatus.Draft; }
    public void Publish(DateTimeOffset effectiveFrom) { Ensure(KpiVersionStatus.Approved); EffectiveFrom = effectiveFrom; Status = KpiVersionStatus.Published; }
    public void Retire(DateTimeOffset at) { if (Status != KpiVersionStatus.Published) throw new KpiDomainException("Only Published versions can retire."); EffectiveTo = at; Status = KpiVersionStatus.Retired; }

    private void Ensure(KpiVersionStatus expected) { if (Status != expected) throw new KpiDomainException($"Version must be {expected}; it is {Status}."); }
    private static string Required(string value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException($"{name} is required.", name) : value.Trim();
}

public enum KpiVersionStatus { Draft, InReview, Rejected, Approved, Published, Retired }

/// <summary>Expected domain validation error.</summary>
public sealed class KpiDomainException(string message) : Exception(message);
