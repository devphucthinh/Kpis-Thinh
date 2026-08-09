namespace Kpi.Domain.Auditing;

/// <summary>Governed event categories retained in the append-only audit stream.</summary>
public enum AuditEventType
{
    Created,
    DraftUpdated,
    Submitted,
    Approved,
    Rejected,
    Published,
    Retired,
    Archived,
    Restored,
    Evaluated,
    Corrected,
    PeriodChanged
}
