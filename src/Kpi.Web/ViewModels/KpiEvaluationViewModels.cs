using Kpi.Domain.Evaluations;
using Kpi.Domain.Formula;

namespace Kpi.Web.ViewModels;

public sealed class EvaluationInputModel
{
    public Guid DefinitionId { get; set; }
    public Guid VersionId { get; set; }
    public Guid ActivationId { get; set; }
    public Dictionary<string, string> Inputs { get; set; } = [];
}

public sealed class CorrectionInputModel
{
    public Guid DefinitionId { get; set; }
    public Guid VersionId { get; set; }
    public Guid ActivationId { get; set; }
    public Guid PredecessorId { get; set; }
    public Dictionary<string, string> Inputs { get; set; } = [];
    public string Reason { get; set; } = string.Empty;
}

public sealed record EvaluationAttemptVm(
    Guid Id,
    DateTimeOffset EvaluatedAt,
    string OutcomeKind,
    string OutcomeText,
    bool IsCurrent,
    Guid VersionId,
    Guid? ActivationId,
    Guid? SupersedesId,
    string? CorrectionReason,
    IReadOnlyDictionary<string, string> Inputs,
    string FormulaSource,
    EvaluationCorrectionDiff? CorrectionDiff);

public sealed record KpiEvaluationPageVm(
    Guid DefinitionId,
    string DefinitionCode,
    string DefinitionName,
    Guid VersionId,
    int VersionNumber,
    Guid ActivationId,
    string FormulaSource,
    string AstJson,
    IReadOnlyList<FormulaVariableInputVm> Variables,
    EvaluationAttemptVm? Current,
    IReadOnlyList<EvaluationAttemptVm> Attempts,
    bool CanEvaluate,
    string? Notice);

public sealed record KpiCorrectionPageVm(
    KpiEvaluationPageVm History,
    EvaluationAttemptVm Predecessor,
    bool CanCorrect,
    string? Notice);
