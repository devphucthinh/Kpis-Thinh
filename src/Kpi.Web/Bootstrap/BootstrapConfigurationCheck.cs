using Kpi.Infrastructure.Postgres.Configuration;

namespace Kpi.Web.Bootstrap;

/// <summary>Validates safe non-secret configuration before local bootstrap.</summary>
public static class BootstrapConfigurationCheck
{
    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        var section = configuration.GetSection("Kpi");
        var database = section["DatabaseName"] ?? "kpi_lab";
        var testDatabase = section["TestDatabaseName"] ?? "kpi_lab_test";
        if (environment.IsProduction() && string.Equals(testDatabase, database, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Production and test database names must differ.");
        TestDatabaseConfigurationValidator.EnsureSafe("kpi_lab_test");
    }
}
