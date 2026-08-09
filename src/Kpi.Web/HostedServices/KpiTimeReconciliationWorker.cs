namespace Kpi.Web.HostedServices;

/// <summary>Placeholder worker seam; lifecycle policy remains in Application operations.</summary>
public sealed class KpiTimeReconciliationWorker(ILogger<KpiTimeReconciliationWorker> logger, IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("KPI reconciliation worker started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = scopeFactory.CreateScope()) scope.ServiceProvider.GetRequiredService<Kpi.Application.ReconcileKpiLifecycle>().Execute();
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
