using Kpi.Application.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kpi.IntegrationTests.Composition;

public sealed class DevelopmentIdentityCompositionTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact(DisplayName = "FR-048 development composition registers an explicit platform identity adapter")]
    public void Development_web_composition_exposes_platform_identity_port()
    {
        using var scope = factory.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetService<IPlatformIdentityReader>());
    }
}
