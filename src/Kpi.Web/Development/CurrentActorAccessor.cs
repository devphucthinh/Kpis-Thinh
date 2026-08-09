using Kpi.Application.Common;

namespace Kpi.Web.Development;

/// <summary>Development-only persona input; command capability checks remain authoritative.</summary>
public sealed class CurrentActorAccessor(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment) : ICurrentActor
{
    public ActorContext Current
    {
        get
        {
            var role = httpContextAccessor.HttpContext?.Request.Query["persona"].FirstOrDefault() ?? "creator";
            if (!DevelopmentPersonaCatalog.Personas.ContainsKey(role)) throw new InvalidOperationException("Unknown Development persona.");
            if (!environment.IsDevelopment() && httpContextAccessor.HttpContext?.Request.Query.ContainsKey("persona") == true)
                throw new InvalidOperationException("Development persona switching is disabled outside Development.");
            return ActorContext.Demo(role);
        }
    }
}
