using Kpi.Application.Organizations;

namespace Kpi.Application.Persistence;

public interface IPlatformIdentityReader
{
    Task<PlatformActor?> ResolveAsync(string subjectId, CancellationToken cancellationToken);
}

/// <summary>
/// Explicit development/test adapter. Production composition must provide a
/// host-backed implementation and must not silently fall back to this class.
/// </summary>
public sealed class DevelopmentPlatformIdentityAdapter : IPlatformIdentityReader
{
    private static readonly IReadOnlySet<string> PlatformAdminCapabilities =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "platform.organization.provision",
            "platform.bootstrap.recover"
        };

    public Task<PlatformActor?> ResolveAsync(string subjectId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var actor = string.Equals(subjectId?.Trim(), "platform-admin", StringComparison.Ordinal)
            ? new PlatformActor("platform-admin", PlatformAdminCapabilities, true)
            : null;
        return Task.FromResult(actor);
    }
}
