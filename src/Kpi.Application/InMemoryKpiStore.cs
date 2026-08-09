using Kpi.Domain.Auditing;
using Kpi.Domain.Evaluations;
using Kpi.Domain.Kpis;
using Kpi.Domain.Periods;

namespace Kpi.Application;

/// <summary>Deterministic local store used by the prototype and replaced by the PostgreSQL adapter in production.</summary>
public sealed class InMemoryKpiStore
{
    private readonly object _gate = new();
    private readonly List<KpiDefinition> _definitions = [];
    private readonly List<AuditRecord> _audit = [];
    private readonly Dictionary<Guid, EvaluationStream> _streams = [];
    private readonly List<KpiPeriod> _periods = [];

    public IReadOnlyList<KpiDefinition> Definitions { get { lock (_gate) return _definitions.ToArray(); } }
    public IReadOnlyList<AuditRecord> Audit { get { lock (_gate) return _audit.OrderBy(x => x.OccurredAt).ToArray(); } }
    public IReadOnlyList<KpiPeriod> Periods { get { lock (_gate) return _periods.ToArray(); } }
    public KpiDefinition AddDefinition(KpiDefinition definition, AuditRecord audit) { lock (_gate) { _definitions.Add(definition); _audit.Add(audit); return definition; } }
    public void AddAudit(AuditRecord audit) { lock (_gate) _audit.Add(audit); }
    public KpiDefinition? Find(Guid id) { lock (_gate) return _definitions.FirstOrDefault(x => x.Id == id); }
    public KpiDefinition? FindByCode(string code, Guid? organizationId = null) { lock (_gate) return _definitions.FirstOrDefault(x => string.Equals(x.Code.Value, code, StringComparison.OrdinalIgnoreCase) && (organizationId is null || x.OrganizationId == organizationId.Value)); }
    public EvaluationStream Stream(Guid definitionId) { lock (_gate) { if (!_streams.TryGetValue(definitionId, out var stream)) _streams[definitionId] = stream = new EvaluationStream(); return stream; } }
    public KpiPeriod AddPeriod(KpiPeriod period, AuditRecord audit) { lock (_gate) { _periods.Add(period); if (_audit.All(x => x.Id != audit.Id)) _audit.Add(audit); return period; } }
    public KpiPeriod? FindPeriod(Guid id) { lock (_gate) return _periods.FirstOrDefault(x => x.Id == id); }
    public (KpiPeriod Period, KpiPeriodActivation Activation)? FindActivation(Guid activationId)
    {
        lock (_gate)
        {
            foreach (var period in _periods)
            {
                var activation = period.Activations.FirstOrDefault(x => x.Id == activationId);
                if (activation is not null) return (period, activation);
            }
            return null;
        }
    }
    public void Clear() { lock (_gate) { _definitions.Clear(); _periods.Clear(); _audit.Clear(); _streams.Clear(); } }
    public void ReplaceDefinitions(IEnumerable<KpiDefinition> definitions)
    {
        lock (_gate)
        {
            _definitions.Clear();
            _definitions.AddRange(definitions);
        }
    }

    public void ReplacePeriods(IEnumerable<KpiPeriod> periods)
    {
        lock (_gate)
        {
            _periods.Clear();
            _periods.AddRange(periods);
        }
    }

    public void ReplaceEvaluations(Guid definitionId, IEnumerable<KpiEvaluation> evaluations)
    {
        lock (_gate)
        {
            var stream = new EvaluationStream();
            stream.Replace(evaluations);
            _streams[definitionId] = stream;
        }
    }
}
