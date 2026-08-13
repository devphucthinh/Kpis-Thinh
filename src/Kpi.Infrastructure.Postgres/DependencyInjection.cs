using Kpi.Infrastructure.Postgres.Configuration;
using Kpi.Application.Authorization;
using Kpi.Application.Persistence;
using Kpi.Infrastructure.Postgres.Authorization;
using Kpi.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kpi.Infrastructure.Postgres;

/// <summary>Registers PostgreSQL persistence from the runtime-only connection.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddKpiPostgres(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("KpiRuntime");
        if (!string.IsNullOrWhiteSpace(connection))
        {
            services.AddDbContext<KpiDbContext>(options => options.UseNpgsql(connection));
            services.AddScoped<Stores.PostgresKpiDefinitionStore>();
            services.AddScoped<Stores.PostgresGovernedStore>();
            services.AddScoped<Kpi.Application.Persistence.IKpiDefinitionPersistence>(sp => sp.GetRequiredService<Stores.PostgresKpiDefinitionStore>());
            services.AddScoped<Kpi.Application.Persistence.IKpiGovernedPersistence>(sp => sp.GetRequiredService<Stores.PostgresGovernedStore>());
            services.AddSingleton(CapabilityCatalogRegistration.Create());
            services.AddScoped<IAuthorizationFactsReader, PostgresAuthorizationFactsReader>();
            services.AddScoped<IAuthorizationDecision, AuthorizationDecisionService>();
            services.AddScoped<IAuditWriter, PostgresAuditWriter>();
            services.AddScoped<IOrganizationTransaction, PostgresOrganizationTransaction>();
        }
        return services;
    }
}
