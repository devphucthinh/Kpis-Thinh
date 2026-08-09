using Kpi.Domain.Kpis;

namespace Kpi.Application.Persistence;

/// <summary>Persistence port for durable Definition/Version snapshots.</summary>
public interface IKpiDefinitionPersistence
{
    void Save(KpiDefinition definition);
}
