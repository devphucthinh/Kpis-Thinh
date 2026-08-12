namespace Kpi.Application.Organizations;

public sealed record PlatformActor(
    string SubjectId,
    IReadOnlySet<string> CapabilityIds,
    bool IsPlatformSecurityAdministrator = false);
