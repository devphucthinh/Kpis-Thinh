namespace Kpi.Infrastructure.Postgres.Persistence;

/// <summary>Named Draft persistence seam retained for the vertical migration plan.</summary>
public sealed class PostgresDraftStore(KpiDbContext context) : Stores.PostgresKpiDefinitionStore(context);
