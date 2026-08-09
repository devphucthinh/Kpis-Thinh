using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Kpi.Infrastructure.Postgres;

namespace Kpi.Web.Configuration;

/// <summary>Explicitly selects durable runtime persistence or the development-only no-database profile.</summary>
public static class PostgresRuntimeConfiguration
{
    public const string InMemoryTestProfile = "InMemoryTest";

    public static bool HasRuntimeConnection(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(configuration.GetConnectionString("KpiRuntime"));

    public static bool UseInMemoryTestProfile(IConfiguration configuration) =>
        string.Equals(configuration["Kpi:PersistenceProfile"], InMemoryTestProfile, StringComparison.OrdinalIgnoreCase);

    public static void Validate(IConfiguration configuration)
    {
        if (!HasRuntimeConnection(configuration) && !UseInMemoryTestProfile(configuration))
            throw new InvalidOperationException("RUNTIME_CONFIGURATION_MISSING");
        if (HasRuntimeConnection(configuration) && UseInMemoryTestProfile(configuration))
            throw new InvalidOperationException("RUNTIME_PROFILE_CONFLICT");
    }

    public static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        Validate(configuration);
        services.AddKpiPostgres(configuration);
        if (UseInMemoryTestProfile(configuration))
        {
            services.AddSingleton<Kpi.Application.InMemoryKpiStore>();
            services.AddScoped<Kpi.Application.KpiOperations>();
            services.AddScoped<Kpi.Application.PeriodOperations>();
            services.AddScoped<Kpi.Application.EvaluationOperations>();
            services.AddScoped<Kpi.Application.ReconcileKpiLifecycle>();
            services.AddHostedService<Kpi.Web.Development.DevelopmentSeedData>();
            return;
        }

        services.AddSingleton<KpiRuntimeState>();
        services.AddScoped<Kpi.Application.KpiOperations>(sp => new Kpi.Application.KpiOperations(
            sp.GetRequiredService<KpiRuntimeState>().Store,
            sp.GetRequiredService<Kpi.Application.Common.IClock>(),
            sp.GetService<Kpi.Application.Persistence.IKpiDefinitionPersistence>(),
            sp.GetService<Kpi.Application.Persistence.IKpiGovernedPersistence>()));
        services.AddScoped<Kpi.Application.PeriodOperations>(sp => new Kpi.Application.PeriodOperations(
            sp.GetRequiredService<KpiRuntimeState>().Store,
            sp.GetRequiredService<Kpi.Application.Common.IClock>(),
            sp.GetService<Kpi.Application.Persistence.IKpiGovernedPersistence>()));
        services.AddScoped<Kpi.Application.EvaluationOperations>(sp => new Kpi.Application.EvaluationOperations(
            sp.GetRequiredService<KpiRuntimeState>().Store,
            sp.GetRequiredService<Kpi.Application.Common.IClock>(),
            sp.GetService<Kpi.Application.Persistence.IKpiGovernedPersistence>()));
        services.AddScoped<Kpi.Application.ReconcileKpiLifecycle>(sp => new Kpi.Application.ReconcileKpiLifecycle(
            sp.GetRequiredService<KpiRuntimeState>().Store,
            sp.GetRequiredService<Kpi.Application.Common.IClock>(),
            sp.GetService<Kpi.Application.Persistence.IKpiGovernedPersistence>()));
    }
}
