namespace Kpi.Infrastructure.Postgres.Migrations;

/// <summary>Forward-only product migration order used by safe local/test bootstrap.</summary>
public static class KpiMigrationManifest
{
    public static IReadOnlyList<string> ProductMigrations { get; } =
    [
        "202608090001_DraftAuthoring",
        "202608090002_VersionGovernance",
        "202608090003_DefinitionRetention",
        "202608090004_PeriodActivation",
        "202608090005_PeriodAmendments",
        "202608090006_Evaluations"
    ];
}
