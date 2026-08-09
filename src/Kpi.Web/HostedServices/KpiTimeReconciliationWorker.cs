namespace Kpi.Web.HostedServices;

/// <summary>Placeholder worker seam; lifecycle policy remains in Application operations.</summary>
public sealed class KpiTimeReconciliationWorker(ILogger<KpiTimeReconciliationWorker> logger, Kpi.Application.ReconcileKpiLifecycle reconciliation) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("KPI reconciliation worker started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            reconciliation.Execute();
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
