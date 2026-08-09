using System.Reflection;
using Xunit;

namespace Kpi.Domain.Tests.Architecture;

public sealed class AssemblyBoundaryTests
{
    [Fact]
    public void Domain_has_no_framework_or_persistence_dependency()
    {
        var references = typeof(Kpi.Domain.AssemblyMarker).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();
        Assert.DoesNotContain(references, name => name is not null && (name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) || name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) || name.StartsWith("Npgsql", StringComparison.Ordinal)));
    }
}
