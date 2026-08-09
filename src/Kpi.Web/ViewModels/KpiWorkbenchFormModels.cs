using Kpi.Domain.Formula;
using Kpi.Web.Queries;

namespace Kpi.Web.ViewModels;

public sealed class CreateKpiModel
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class EditKpiModel
{
    public Guid DefinitionId { get; set; }
    public Guid VersionId { get; set; }
    public string ConcurrencyToken { get; set; } = "0";
    public string VersionName { get; set; } = string.Empty;
    public string VersionDescription { get; set; } = string.Empty;
    public string VariablesJson { get; set; } = "[]";
    public string Variables { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string ChangeSummary { get; set; } = string.Empty;

    public IReadOnlyList<FormulaVariableInputVm> VariableRows => FormulaVariableFormSerializer.Deserialize(VariablesJson, Variables);
}

public sealed class KpiEditPageVm
{
    public KpiWorkbenchVm Workbench { get; }
    public EditKpiModel Form { get; }

    public KpiEditPageVm(KpiWorkbenchVm workbench, EditKpiModel form)
    {
        Workbench = workbench;
        Form = form;
    }
}
