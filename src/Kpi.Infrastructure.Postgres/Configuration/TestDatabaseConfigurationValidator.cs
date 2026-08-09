namespace Kpi.Infrastructure.Postgres.Configuration;

/// <summary>Prevents destructive integration setup from targeting an unintended database.</summary>
public static class TestDatabaseConfigurationValidator
{
    public static void EnsureSafe(string databaseName)
    {
        if (!string.Equals(databaseName, "kpi_lab_test", StringComparison.Ordinal))
            throw new InvalidOperationException("Destructive test operations require database kpi_lab_test.");
    }
}
