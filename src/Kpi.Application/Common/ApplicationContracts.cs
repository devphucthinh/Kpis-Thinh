namespace Kpi.Application.Common;

/// <summary>Capabilities carried by the current actor; UI persona never bypasses these checks.</summary>
[Flags]
public enum KpiCapability
{
    None = 0,
    CreateKpi = 1,
    EditDraft = 2,
    ReviewKpi = 4,
    PlanPeriod = 8,
    ApprovePeriod = 16,
    Evaluate = 32,
    AuditRead = 64,
    Administrator = 128
}

/// <summary>Authenticated-identity adapter contract. Development uses a seeded actor only.</summary>
public sealed record ActorContext(Guid ActorId, Guid OrganizationId, KpiCapability Capabilities, string CorrelationId)
{
    public bool Can(KpiCapability capability) => (Capabilities & capability) == capability;
    public static ActorContext Demo(string role)
    {
        var normalized = role.ToLowerInvariant();
        var id = normalized switch
        {
            "approver" => Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "planner" => Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "evaluator" => Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "admin" => Guid.Parse("55555555-5555-5555-5555-555555555555"),
            "observer" => Guid.Parse("66666666-6666-6666-6666-666666666666"),
            _ => Guid.Parse("11111111-aaaa-aaaa-aaaa-111111111111")
        };
        var capabilities = normalized switch
        {
            "approver" => KpiCapability.ReviewKpi | KpiCapability.ApprovePeriod,
            "planner" => KpiCapability.PlanPeriod,
            "evaluator" => KpiCapability.Evaluate,
            "admin" => KpiCapability.Administrator | KpiCapability.AuditRead,
            "observer" => KpiCapability.AuditRead,
            _ => KpiCapability.CreateKpi | KpiCapability.EditDraft
        };
        return new(id, InMemoryIds.Organization, capabilities, $"demo-{normalized}");
    }
}

public interface ICurrentActor { ActorContext Current { get; } }
public interface IClock { DateTimeOffset UtcNow { get; } }
public sealed class SystemClock : IClock { public DateTimeOffset UtcNow => DateTimeOffset.UtcNow; }
public static class InMemoryIds { public static readonly Guid Organization = Guid.Parse("11111111-1111-1111-1111-111111111111"); }

/// <summary>Stable application failure that delivery maps to localized Problem Details.</summary>
public sealed record ApplicationError(string Code, string Message, int Status = 422);
public sealed record ApplicationResult<T>(T? Value, ApplicationError? Error)
{
    public bool IsSuccess => Error is null;
    public static ApplicationResult<T> Success(T value) => new(value, null);
    public static ApplicationResult<T> Failure(string code, string message, int status = 422) => new(default, new ApplicationError(code, message, status));
}
