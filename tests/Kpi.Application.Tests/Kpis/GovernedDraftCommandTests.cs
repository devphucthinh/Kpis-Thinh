using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Domain.Formula;
using Kpi.Domain.Kpis;
using Xunit;

namespace Kpi.Application.Tests.Kpis;

public sealed class GovernedDraftCommandTests
{
    [Fact]
    public void Administrator_cannot_create_or_edit_content_owned_by_a_creator()
    {
        var store = new InMemoryKpiStore(); var operations = new KpiOperations(store, new Clock()); var creator = ActorContext.Demo("creator"); var admin = new ActorContext(Guid.NewGuid(), creator.OrganizationId, KpiCapability.Administrator | KpiCapability.EditDraft, "admin");
        var definition = operations.CreateDefinition(creator, "OWNER_SCOPE", "Owner", "Test").Value!;
        var createdByAdmin = operations.CreateVersion(admin, definition.Id, "v1", "Version", "1", [], FormulaResultType.Decimal, "No");
        Assert.Equal("AUTHORIZATION_DENIED", createdByAdmin.Error!.Code);
    }

    [Fact]
    public void Owner_can_update_return_clone_and_delete_drafts_with_opaque_concurrency()
    {
        var store = new InMemoryKpiStore(); var operations = new KpiOperations(store, new Clock()); var creator = ActorContext.Demo("creator"); var approver = ActorContext.Demo("approver");
        var definition = operations.CreateDefinition(creator, "DRAFT_COMMANDS", "Draft", "Test").Value!;
        var version = operations.CreateVersion(creator, definition.Id, "v1", "Version", "1", [], FormulaResultType.Decimal, "Initial").Value!;
        var token = operations.ConcurrencyToken(version);
        var updated = operations.UpdateDraft(creator, definition.Id, version.Id, "v1 revised", "Version revised", "2", [], token);
        Assert.True(updated.IsSuccess, updated.Error?.Message);
        var stale = operations.UpdateDraft(creator, definition.Id, version.Id, "stale", "stale", "3", [], token);
        Assert.Equal("CONCURRENCY_CONFLICT", stale.Error!.Code);

        Assert.True(operations.SubmitVersion(creator, definition.Id, version.Id).IsSuccess);
        Assert.True(operations.ReviewVersion(approver, definition.Id, version.Id, false, "Need changes").IsSuccess);
        Assert.True(operations.ReturnVersionToDraft(creator, definition.Id, version.Id).IsSuccess);
        var clone = operations.CloneVersion(creator, definition.Id, version.Id, "Clone for next period");
        Assert.True(clone.IsSuccess, clone.Error?.Message);
        Assert.Equal(version.Id, clone.Value!.PredecessorVersionId);

        var deleteStore = new InMemoryKpiStore(); var deleteOps = new KpiOperations(deleteStore, new Clock()); var deleteDefinition = deleteOps.CreateDefinition(creator, "DELETE_DRAFT", "Delete", "Test").Value!;
        var deleteVersion = deleteOps.CreateVersion(creator, deleteDefinition.Id, "v1", "Version", "1", [], FormulaResultType.Decimal, "Initial").Value!;
        var deleted = deleteOps.DeleteDraft(creator, deleteDefinition.Id, deleteVersion.Id, deleteOps.ConcurrencyToken(deleteVersion));
        Assert.True(deleted.IsSuccess, deleted.Error?.Message);
        Assert.Empty(deleteDefinition.Versions);
        Assert.Contains(deleteStore.Audit, x => x.Summary?.Contains("tombstone", StringComparison.OrdinalIgnoreCase) == true);
    }

    [Fact]
    public void Rejected_version_can_return_only_in_its_organization_and_by_its_owner()
    {
        var store = new InMemoryKpiStore(); var operations = new KpiOperations(store, new Clock()); var creator = ActorContext.Demo("creator"); var approver = ActorContext.Demo("approver");
        var definition = operations.CreateDefinition(creator, "ORG_SCOPE", "Org", "Test").Value!;
        var version = operations.CreateVersion(creator, definition.Id, "v1", "Version", "1", [], FormulaResultType.Decimal, "Initial").Value!;
        Assert.True(operations.SubmitVersion(creator, definition.Id, version.Id).IsSuccess);
        Assert.True(operations.ReviewVersion(approver, definition.Id, version.Id, false, "Fix").IsSuccess);
        var foreign = new ActorContext(Guid.NewGuid(), Guid.NewGuid(), KpiCapability.EditDraft, "foreign");
        Assert.Equal("ORGANIZATION_SCOPE_CONFLICT", operations.ReturnVersionToDraft(foreign, definition.Id, version.Id).Error!.Code);
        Assert.True(operations.ReturnVersionToDraft(creator, definition.Id, version.Id).IsSuccess);
    }

    private sealed class Clock : IClock { public DateTimeOffset UtcNow => new(2026, 8, 9, 0, 0, 0, TimeSpan.Zero); }
}
