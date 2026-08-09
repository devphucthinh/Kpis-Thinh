using Kpi.Infrastructure.Postgres.Configuration;
using Kpi.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kpi.Infrastructure.Postgres;

/// <summary>Registers PostgreSQL persistence when a non-secret connection is configured.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddKpiPostgres(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("Kpi");
        if (!string.IsNullOrWhiteSpace(connection)) { services.AddDbContext<KpiDbContext>(options => options.UseNpgsql(connection)); services.AddScoped<Stores.PostgresKpiDefinitionStore>(); services.AddScoped<Stores.PostgresGovernedStore>(); services.AddScoped<Kpi.Application.Persistence.IKpiDefinitionPersistence>(sp => sp.GetRequiredService<Stores.PostgresKpiDefinitionStore>()); services.AddScoped<Kpi.Application.Persistence.IKpiGovernedPersistence>(sp => sp.GetRequiredService<Stores.PostgresGovernedStore>()); }
        return services;
    }
}
