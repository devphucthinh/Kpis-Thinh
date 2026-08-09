using System.Diagnostics;
using Kpi.Infrastructure.Postgres.Migrations;
using Kpi.Infrastructure.Postgres.Persistence;
using Kpi.Migrator.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var stopwatch = Stopwatch.StartNew();
try
{
    var options = MigratorOptions.FromConfiguration(configuration);
    var dbOptions = new DbContextOptionsBuilder<KpiDbContext>()
        .UseNpgsql(options.MigrationConnectionString)
        .Options;

    await using var context = new KpiDbContext(dbOptions);
    var result = await new KpiMigrationRunner(context).ApplyAsync(options.ToPostgresOptions());
    stopwatch.Stop();
    Console.WriteLine($"MIGRATION_TARGET={result.TargetDatabase}");
    Console.WriteLine($"MIGRATION_APPLIED={string.Join(',', result.AppliedIds)}");
    Console.WriteLine($"MIGRATION_SKIPPED={string.Join(',', result.SkippedIds)}");
    Console.WriteLine($"MIGRATION_ELAPSED_MS={stopwatch.ElapsedMilliseconds}");
    return 0;
}
catch (InvalidOperationException ex) when (ex.Message is "MIGRATION_CONFIGURATION_MISSING" or "MIGRATION_TARGET_NOT_ALLOWED" or "MIGRATION_CHECKSUM_MISMATCH")
{
    Console.Error.WriteLine(ex.Message);
    return 2;
}
catch
{
    Console.Error.WriteLine("MIGRATION_APPLY_FAILED");
    return 1;
}
