using Kpi.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kpi.Web.Development;

/// <summary>Creates only local Development identity reference rows for the PostgreSQL demo profile.</summary>
public sealed class DevelopmentPostgresSeedData(
    IServiceScopeFactory scopeFactory,
    IWebHostEnvironment environment,
    IConfiguration configuration) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment() || !configuration.GetValue<bool>("Kpi:EnableDevelopmentSeed")) return;

        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<KpiDbContext>();
        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO organizations (id, name)
            VALUES ('11111111-1111-1111-1111-111111111111', 'KPI Development Company')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO actors (id, organization_id, display_name, capabilities)
            VALUES
                ('11111111-aaaa-aaaa-aaaa-111111111111', '11111111-1111-1111-1111-111111111111', 'Development Creator', 3),
                ('22222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', 'Development Policy Approver', 20),
                ('33333333-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111111', 'Development Period Planner', 8),
                ('44444444-4444-4444-4444-444444444444', '11111111-1111-1111-1111-111111111111', 'Development Evaluator', 32),
                ('55555555-5555-5555-5555-555555555555', '11111111-1111-1111-1111-111111111111', 'Development Administrator', 192),
                ('66666666-6666-6666-6666-666666666666', '11111111-1111-1111-1111-111111111111', 'Development Observer', 64)
            ON CONFLICT (id) DO NOTHING;
            """,
            cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
