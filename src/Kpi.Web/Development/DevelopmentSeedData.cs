using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Formula;

namespace Kpi.Web.Development;

/// <summary>Idempotent sample data only for Development.</summary>
public sealed class DevelopmentSeedData(InMemoryKpiStore store, IServiceScopeFactory scopeFactory, IWebHostEnvironment environment) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() || store.Definitions.Count > 0) return Task.CompletedTask;
        using var scope = scopeFactory.CreateScope();
        var operations = scope.ServiceProvider.GetRequiredService<KpiOperations>();
        var actor = ActorContext.Demo("creator");
        var created = operations.CreateDefinition(actor, "REVENUE_ACHIEVEMENT", "Revenue achievement", "Demo KPI for formula authoring.");
        if (created.IsSuccess)
        {
            var variables = new[] { FormulaVariableDefinition.Create("revenue", "Revenue", FormulaValueType.Decimal), FormulaVariableDefinition.Create("target", "Target", FormulaValueType.Decimal) };
            operations.CreateVersion(actor, created.Value!.Id, "Percentage", "Revenue against target", "ROUND(revenue / target * 100, 2)", variables, FormulaResultType.Decimal, "Initial development example");
        }
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
