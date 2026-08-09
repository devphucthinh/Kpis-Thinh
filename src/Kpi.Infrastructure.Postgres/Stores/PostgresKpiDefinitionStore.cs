using System.Text.Json;
using Kpi.Domain.Kpis;
using Kpi.Domain.Formula.Serialization;
using Kpi.Infrastructure.Postgres.Persistence;
using Kpi.Application.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kpi.Infrastructure.Postgres.Stores;

/// <summary>Durable Definition/Version adapter with Formula JSONB snapshots.</summary>
public class PostgresKpiDefinitionStore(KpiDbContext context) : IKpiDefinitionPersistence
{
    public void Save(KpiDefinition definition) => SaveAsync(definition).GetAwaiter().GetResult();
    public async Task SaveAsync(KpiDefinition definition, CancellationToken cancellationToken = default)
    {
        var row = await context.Definitions.FindAsync([definition.Id], cancellationToken);
        if (row is null)
        {
            context.Definitions.Add(new KpiDefinitionRow { Id = definition.Id, OrganizationId = definition.OrganizationId, Code = definition.Code.Value, Name = definition.Name, Description = definition.Description, Archived = definition.Archived });
        }
        else
        {
            row.Name = definition.Name; row.Description = definition.Description; row.Archived = definition.Archived;
        }
        foreach (var version in definition.Versions)
        {
            var versionRow = await context.Versions.FindAsync([version.Id], cancellationToken) ?? new KpiVersionRow { Id = version.Id, DefinitionId = definition.Id, VersionNumber = version.VersionNumber };
            versionRow.Status = version.Status.ToString(); versionRow.FormulaJson = FormulaDocumentSerializer.Serialize(version.Formula); versionRow.EffectiveFrom = version.EffectiveFrom; versionRow.EffectiveTo = version.EffectiveTo;
            if (context.Entry(versionRow).State == EntityState.Detached) context.Versions.Add(versionRow);
        }
        await context.SaveChangesAsync(cancellationToken);
    }
}
