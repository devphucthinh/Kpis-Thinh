namespace Kpi.Domain.Organizations;

public readonly record struct RevisionToken
{
    public RevisionToken(long revision) : this()
    {
        if (revision < 0)
            throw new ArgumentOutOfRangeException(nameof(revision), "Revision cannot be negative.");
        Revision = revision;
    }

    public long Revision { get; }
    public static RevisionToken Start => new(0);
    public RevisionToken Next() => new(checked(Revision + 1));
}

public static class StableOrganizationStatus
{
    public const string Active = "active";
    public const string Inactive = "inactive";
}

public static class StableCapabilityCodes
{
    public const string OrganizationStructureView = "organization.structure.view";
}
