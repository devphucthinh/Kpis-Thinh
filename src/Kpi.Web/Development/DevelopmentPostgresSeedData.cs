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
            INSERT INTO organizations (id, code, name)
            VALUES ('11111111-1111-1111-1111-111111111111', 'DEV-KPI', 'KPI Development Company')
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

            INSERT INTO organization_employees (id, organization_id, employee_number, display_name, employment_from, account_status)
            VALUES
                ('11111111-aaaa-aaaa-aaaa-111111111111', '11111111-1111-1111-1111-111111111111', 'DEV-CREATOR', 'Development Creator', now() - interval '1 day', 'active'),
                ('22222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', 'DEV-APPROVER', 'Development Policy Approver', now() - interval '1 day', 'active'),
                ('33333333-3333-3333-3333-333333333333', '11111111-1111-1111-1111-111111111111', 'DEV-PLANNER', 'Development Period Planner', now() - interval '1 day', 'active'),
                ('44444444-4444-4444-4444-444444444444', '11111111-1111-1111-1111-111111111111', 'DEV-EVALUATOR', 'Development Evaluator', now() - interval '1 day', 'active'),
                ('55555555-5555-5555-5555-555555555555', '11111111-1111-1111-1111-111111111111', 'DEV-ADMIN', 'Development Administrator', now() - interval '1 day', 'active'),
                ('66666666-6666-6666-6666-666666666666', '11111111-1111-1111-1111-111111111111', 'DEV-OBSERVER', 'Development Observer', now() - interval '1 day', 'active')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO custom_kpi_roles (id, organization_id, name, status)
            VALUES
                ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111', 'Development KPI Creator', 'Active'),
                ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', '11111111-1111-1111-1111-111111111111', 'Development KPI Approver', 'Active'),
                ('cccccccc-cccc-cccc-cccc-cccccccccccc', '11111111-1111-1111-1111-111111111111', 'Development KPI Administrator', 'Active')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO custom_kpi_role_versions (id, organization_id, role_id, version_number, status, created_by)
            VALUES
                ('aaaaaaaa-bbbb-aaaa-bbbb-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 1, 'Active', '11111111-aaaa-aaaa-aaaa-111111111111'),
                ('bbbbbbbb-cccc-bbbb-cccc-bbbbbbbbbbbb', '11111111-1111-1111-1111-111111111111', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 1, 'Active', '22222222-2222-2222-2222-222222222222'),
                ('cccccccc-dddd-cccc-dddd-cccccccccccc', '11111111-1111-1111-1111-111111111111', 'cccccccc-cccc-cccc-cccc-cccccccccccc', 1, 'Active', '55555555-5555-5555-5555-555555555555')
            ON CONFLICT (id) DO NOTHING;

            INSERT INTO custom_kpi_role_capabilities (organization_id, role_version_id, capability_id)
            VALUES
                ('11111111-1111-1111-1111-111111111111', 'aaaaaaaa-bbbb-aaaa-bbbb-aaaaaaaaaaaa', 'kpi.definition.create'),
                ('11111111-1111-1111-1111-111111111111', 'aaaaaaaa-bbbb-aaaa-bbbb-aaaaaaaaaaaa', 'kpi.definition.edit'),
                ('11111111-1111-1111-1111-111111111111', 'aaaaaaaa-bbbb-aaaa-bbbb-aaaaaaaaaaaa', 'kpi.version.submit'),
                ('11111111-1111-1111-1111-111111111111', 'bbbbbbbb-cccc-bbbb-cccc-bbbbbbbbbbbb', 'kpi.version.review'),
                ('11111111-1111-1111-1111-111111111111', 'bbbbbbbb-cccc-bbbb-cccc-bbbbbbbbbbbb', 'kpi.version.activate'),
                ('11111111-1111-1111-1111-111111111111', 'cccccccc-dddd-cccc-dddd-cccccccccccc', 'kpi.definition.admin'),
                ('11111111-1111-1111-1111-111111111111', 'cccccccc-dddd-cccc-dddd-cccccccccccc', 'audit.timeline.view')
            ON CONFLICT DO NOTHING;

            INSERT INTO role_assignments (id, organization_id, employee_id, role_version_id, scope_kind, effective_from, status)
            VALUES
                ('aaaaaaaa-eeee-aaaa-eeee-aaaaaaaaaaaa', '11111111-1111-1111-1111-111111111111', '11111111-aaaa-aaaa-aaaa-111111111111', 'aaaaaaaa-bbbb-aaaa-bbbb-aaaaaaaaaaaa', 'Organization', now() - interval '1 day', 'Effective'),
                ('bbbbbbbb-eeee-bbbb-eeee-bbbbbbbbbbbb', '11111111-1111-1111-1111-111111111111', '22222222-2222-2222-2222-222222222222', 'bbbbbbbb-cccc-bbbb-cccc-bbbbbbbbbbbb', 'Organization', now() - interval '1 day', 'Effective'),
                ('cccccccc-eeee-cccc-eeee-cccccccccccc', '11111111-1111-1111-1111-111111111111', '55555555-5555-5555-5555-555555555555', 'cccccccc-dddd-cccc-dddd-cccccccccccc', 'Organization', now() - interval '1 day', 'Effective')
            ON CONFLICT (id) DO NOTHING;
            """,
            cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
