using Kpi.Domain.Auditing;
using Kpi.Infrastructure.Postgres.Migrations;
using Kpi.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Kpi.IntegrationTests.Migrations;

namespace Kpi.IntegrationTests.Database;

[Collection("PostgreSQL migration contract")]
public sealed class PostgresOrganizationTransactionTests(MigrationDatabaseFixture fixture)
{
    [Fact(DisplayName = "FR-033 command and audit rollback together when command fails after SaveChanges")]
    public async Task Command_failure_rolls_back_business_rows_and_audit_rows_as_one_postgres_transaction()
    {
        fixture.RequireEnabled();
        await fixture.ResetAsync();
        await fixture.CreateRunner().ApplyAsync(fixture.Options, TestContext.Current.CancellationToken);
        var options = new DbContextOptionsBuilder<KpiDbContext>().UseNpgsql(fixture.ConnectionString).Options;
        var organizationId = Guid.NewGuid();
        var audit = AuditRecord.Create(organizationId, Guid.NewGuid(), "Organization", Guid.NewGuid(), AuditEventType.Created, DateTimeOffset.UtcNow, "atomicity");

        await using (var context = new KpiDbContext(options))
        {
            var transaction = new PostgresOrganizationTransaction(context);
            await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync(
                organizationId,
                0,
                async cancellationToken =>
                {
                    context.Organizations.Add(new OrganizationRow { Id = organizationId, Code = $"ATOMIC-{organizationId:N}", Name = "rolled back" });
                    await context.SaveChangesAsync(cancellationToken);
                    throw new InvalidOperationException("simulate command failure after business SaveChanges");
                },
                [audit],
                TestContext.Current.CancellationToken));
        }

        await using (var verification = new KpiDbContext(options))
        {
            Assert.False(await verification.Organizations.AnyAsync(row => row.Id == organizationId, TestContext.Current.CancellationToken));
            Assert.False(await verification.AuditRecords.AnyAsync(row => row.Id == audit.Id, TestContext.Current.CancellationToken));
        }
    }
}
