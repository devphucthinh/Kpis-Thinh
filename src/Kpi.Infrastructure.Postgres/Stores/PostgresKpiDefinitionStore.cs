using System.Text.Json;
using Kpi.Application.Persistence;
using Kpi.Domain.Formula;
using Kpi.Domain.Formula.Serialization;
using Kpi.Domain.Kpis;
using Kpi.Domain.Periods;
using Kpi.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kpi.Infrastructure.Postgres.Stores;

/// <summary>Durable Definition/Version adapter with source-authoritative Formula JSONB snapshots.</summary>
public class PostgresKpiDefinitionStore(KpiDbContext context) : IKpiDefinitionPersistence
{
    public void Save(KpiDefinition definition) => SaveAsync(definition).GetAwaiter().GetResult();

    public IReadOnlyList<KpiDefinition> LoadAll(Guid? organizationId = null) => LoadAllAsync(organizationId).GetAwaiter().GetResult();

    public async Task SaveAsync(KpiDefinition definition, CancellationToken cancellationToken = default)
    {
        var row = await context.Definitions.FindAsync([definition.Id], cancellationToken);
        if (row is null)
        {
            row = new KpiDefinitionRow { Id = definition.Id };
            context.Definitions.Add(row);
        }
        row.OrganizationId = definition.OrganizationId;
        row.Code = definition.Code.Value;
        row.Name = definition.Name;
        row.Description = definition.Description;
        row.OwnerId = definition.OwnerId;
        row.Archived = definition.Archived;
        row.Revision = definition.Revision;

        var existing = await context.Versions.Where(x => x.DefinitionId == definition.Id).ToListAsync(cancellationToken);
        var currentIds = definition.Versions.Select(x => x.Id).ToHashSet();
        context.Versions.RemoveRange(existing.Where(x => !currentIds.Contains(x.Id)));
        foreach (var version in definition.Versions)
        {
            var versionRow = existing.FirstOrDefault(x => x.Id == version.Id);
            if (versionRow is null)
            {
                versionRow = new KpiVersionRow { Id = version.Id, DefinitionId = definition.Id };
                context.Versions.Add(versionRow);
            }
            versionRow.VersionNumber = version.VersionNumber;
            versionRow.Name = version.Name;
            versionRow.Description = version.Description;
            versionRow.ChangeSummary = version.ChangeSummary;
            versionRow.PredecessorVersionId = version.PredecessorVersionId;
            versionRow.Status = version.Status.ToString();
            versionRow.FormulaJson = FormulaDocumentSerializer.Serialize(version.Formula);
            versionRow.VariablesJson = JsonSerializer.Serialize(version.Variables.Select(x => new PersistedVariable(x.Code, x.DisplayName, x.Description, x.Type.ToString(), x.Required, PersistedValue.From(x.DefaultValue), x.DisplayOrder)));
            versionRow.DeclaredResultType = version.DeclaredResultType.ToString();
            versionRow.Cadence = version.Cadence.ToString();
            versionRow.ReviewComment = version.ReviewComment;
            versionRow.EffectiveFrom = version.EffectiveFrom;
            versionRow.EffectiveTo = version.EffectiveTo;
            versionRow.Revision = version.Revision;
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KpiDefinition>> LoadAllAsync(Guid? organizationId = null, CancellationToken cancellationToken = default)
    {
        var query = context.Definitions.AsNoTracking().AsQueryable();
        if (organizationId is not null) query = query.Where(x => x.OrganizationId == organizationId.Value);
        var definitions = await query.OrderBy(x => x.Code).ToListAsync(cancellationToken);
        var ids = definitions.Select(x => x.Id).ToArray();
        var versions = await context.Versions.AsNoTracking().Where(x => ids.Contains(x.DefinitionId)).OrderBy(x => x.VersionNumber).ToListAsync(cancellationToken);
        return definitions.Select(definition => KpiDefinition.Rehydrate(
            definition.Id,
            definition.OrganizationId,
            definition.Code,
            definition.Name,
            definition.Description,
            definition.OwnerId,
            definition.Archived,
            definition.Revision,
            versions.Where(x => x.DefinitionId == definition.Id).Select(RehydrateVersion))).ToArray();
    }

    private static KpiVersion RehydrateVersion(KpiVersionRow row)
    {
        var variables = JsonSerializer.Deserialize<PersistedVariable[]>(row.VariablesJson) ?? [];
        var definitions = variables.Select(x => FormulaVariableDefinition.Create(x.Code, x.DisplayName, Enum.Parse<FormulaValueType>(x.Type), x.Required, x.DefaultValue?.ToDomain(), x.DisplayOrder, x.Description)).ToArray();
        return KpiVersion.Rehydrate(row.Id, row.VersionNumber, row.Name, row.Description, FormulaDocumentSerializer.Deserialize(row.FormulaJson), definitions, Enum.Parse<FormulaResultType>(row.DeclaredResultType), row.ChangeSummary, row.PredecessorVersionId, Enum.Parse<KpiCadence>(row.Cadence), Enum.Parse<KpiVersionStatus>(row.Status), row.EffectiveFrom, row.EffectiveTo, row.ReviewComment, row.Revision);
    }

    private sealed record PersistedVariable(string Code, string DisplayName, string Description, string Type, bool Required, PersistedValue? DefaultValue, int DisplayOrder);
    private sealed record PersistedValue(string Kind, string? Decimal, bool? Boolean)
    {
        public static PersistedValue? From(FormulaValue? value) => value switch { DecimalFormulaValue d => new("Decimal", d.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), null), BooleanFormulaValue b => new("Boolean", null, b.Value), _ => null };
        public FormulaValue ToDomain() => Kind == "Boolean" ? FormulaValue.Boolean(Boolean ?? false) : FormulaValue.Decimal(decimal.Parse(Decimal ?? "0", System.Globalization.CultureInfo.InvariantCulture));
    }
}
