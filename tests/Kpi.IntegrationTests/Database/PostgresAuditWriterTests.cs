using Kpi.Domain.Auditing;
using Kpi.Infrastructure.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Kpi.IntegrationTests.Database;

public sealed class PostgresAuditWriterTests
{
    [Fact(DisplayName = "FR-033 audit writer appends authorization evidence without overwriting history")]
    public async Task Audit_writer_is_append_only_for_one_record_identity()
    {
        var options = new DbContextOptionsBuilder<KpiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var organizationId = Guid.NewGuid();
        var record = AuditRecord.Create(organizationId, Guid.NewGuid(), "KpiPlan", Guid.NewGuid(), AuditEventType.Submitted,
            DateTimeOffset.UtcNow, "corr-1", capabilityId: "organization.structure.view", decision: "allowed");

        await using (var context = new KpiDbContext(options))
        {
            var writer = new PostgresAuditWriter(context);
            await writer.AppendAsync(record, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = new KpiDbContext(options))
        {
            var writer = new PostgresAuditWriter(context);
            var duplicate = new AuditRecord(record.Id, record.OrganizationId, record.ActorId, record.EntityType, record.EntityId,
                record.EventType, record.OccurredAt, record.CorrelationId, reason: "duplicate");
            await writer.AppendAsync(duplicate, TestContext.Current.CancellationToken);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            Assert.Single(await context.AuditRecords.ToListAsync(TestContext.Current.CancellationToken));
        }
    }
}
