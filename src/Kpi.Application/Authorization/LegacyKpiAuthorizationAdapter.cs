using Kpi.Application.Common;

namespace Kpi.Application.Authorization;

/// <summary>
/// Development-only compatibility adapter for the explicit InMemoryTest profile.
/// The PostgreSQL composition always supplies <see cref="IAuthorizationDecision"/>.
/// </summary>
internal static class LegacyKpiAuthorizationAdapter
{
    public static bool Can(ActorContext actor, KpiCapability capability) => actor.Can(capability);
}
