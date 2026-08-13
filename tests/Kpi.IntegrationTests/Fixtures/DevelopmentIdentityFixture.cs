namespace Kpi.IntegrationTests.Fixtures;

/// <summary>Deterministic non-secret identities used by reference integration tests.</summary>
public sealed class DevelopmentIdentityFixture
{
    public static readonly Guid OrganizationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid EmployeeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid DelegateEmployeeId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    public string ActorSubject => "employee-reference";
    public string DelegateSubject => "delegate-reference";
}
