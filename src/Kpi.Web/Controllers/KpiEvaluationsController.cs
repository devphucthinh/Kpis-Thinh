using Kpi.Application;
using Kpi.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Kpi.Web.Controllers;

public sealed class KpiEvaluationsController(KpiOperations kpis, EvaluationOperations evaluations) : Controller
{
    [HttpGet]
    public IActionResult History(Guid definitionId) => View(new EvaluationHistoryModel { Definition = kpis.List().FirstOrDefault(x => x.Id == definitionId), Current = evaluations.Current(definitionId), Attempts = evaluations.History(definitionId) });
}

public sealed class EvaluationHistoryModel { public Kpi.Domain.Kpis.KpiDefinition? Definition { get; set; } public Kpi.Domain.Evaluations.KpiEvaluation? Current { get; set; } public IReadOnlyList<Kpi.Domain.Evaluations.KpiEvaluation> Attempts { get; set; } = []; }
