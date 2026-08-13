namespace Kpi.Domain.Organizations;

/// <summary>Organization identity and lifecycle head shared by all governed facts.</summary>
public sealed class Organization
{
    private Organization(Guid id, string code, string name, string timeZoneId, bool operationallyExposed)
    {
        Id = id;
        Code = code;
        Name = name;
        TimeZoneId = timeZoneId;
        OperationallyExposed = operationallyExposed;
    }

    public Guid Id { get; }
    public string Code { get; }
    public string Name { get; private set; }
    public string TimeZoneId { get; }
    public OrganizationStatus Status { get; private set; } = OrganizationStatus.Active;
    public bool OperationallyExposed { get; }
    public RevisionToken Revision { get; private set; } = RevisionToken.Start;

    public static Organization Create(string code, string name, string timeZoneId = "UTC", bool operationallyExposed = false)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Organization code is required.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Organization name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ArgumentException("Organization time zone is required.", nameof(timeZoneId));

        return new(Guid.NewGuid(), code.Trim(), name.Trim(), timeZoneId.Trim(), operationallyExposed);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Organization name is required.", nameof(name));

        Name = name.Trim();
        Revision = Revision.Next();
    }

    public void Deactivate()
    {
        Status = OrganizationStatus.Inactive;
        Revision = Revision.Next();
    }

    public void Activate()
    {
        Status = OrganizationStatus.Active;
        Revision = Revision.Next();
    }
}

public enum OrganizationStatus
{
    Active,
    Inactive
}
