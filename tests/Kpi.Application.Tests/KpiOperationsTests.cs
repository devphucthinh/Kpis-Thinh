using Kpi.Application;
using Kpi.Application.Common;
using Kpi.Application.Persistence;
using Kpi.Application.Authorization;
using Kpi.Domain.Auditing;
using Kpi.Domain.Evaluations;
using Kpi.Domain.Formula;
using Kpi.Domain.Kpis;
using Kpi.Domain.Periods;
using Xunit;

namespace Kpi.Application.Tests;

public sealed class KpiOperationsTests
{
    [Fact(DisplayName = "FR-049 every governed KPI command calls the shared authorization decision seam per action")]
    public void Governed_command_reloads_the_shared_authorization_seam_between_actions()
    {
        var authorization = new RecordingAuthorizationDecision();
        var store = new InMemoryKpiStore();
        var operations = new KpiOperations(store, new FixedClock(), authorization: authorization);
        var actor = ActorContext.Demo("creator");

        var first = operations.CreateDefinition(actor, "AUTH_SEAM", "Authorization seam", "Test");
        authorization.Allow = false;
        var second = operations.CreateDefinition(actor, "AUTH_SEAM_2", "Authorization seam", "Test");

        Assert.True(first.IsSuccess);
        Assert.Equal("AUTHORIZATION_DENIED", second.Error!.Code);
        Assert.Equal(2, authorization.CallCount);
        Assert.Equal("kpi.definition.create", authorization.Capabilities[0]);
        Assert.Equal("kpi.definition.create", authorization.Capabilities[1]);
    }

    [Fact]
    public void Creator_cannot_self_approve_and_audit_is_written_for_create()
    {
        var store = new InMemoryKpiStore(); var clock = new FixedClock(); var operations = new KpiOperations(store, clock); var creator = ActorContext.Demo("creator");
        var created = operations.CreateDefinition(creator, "OPERATIONS", "Operations", "Test");
        Assert.True(created.IsSuccess);
        var version = operations.CreateVersion(creator, created.Value!.Id, "v1", "First", "1", [], FormulaResultType.Decimal, "Initial");
        Assert.True(version.IsSuccess);
        Assert.True(operations.SubmitVersion(creator, created.Value.Id, version.Value!.Id).IsSuccess);
        var selfReview = operations.ReviewVersion(creator, created.Value.Id, version.Value.Id, true, "No");
        Assert.Equal("SELF_APPROVAL_FORBIDDEN", selfReview.Error!.Code);
        Assert.Equal(3, store.Audit.Count);
    }

    [Fact(DisplayName = "FR-033 KPI definition command and audit are committed in one transaction")]
    public void Definition_create_persists_business_and_audit_inside_same_transaction()
    {
        var definitionPersistence = new RecordingDefinitionPersistence();
        var governedPersistence = new RecordingGovernedPersistence(definitionPersistence);
        var operations = new KpiOperations(new InMemoryKpiStore(), new FixedClock(), definitionPersistence, governedPersistence);

        var result = operations.CreateDefinition(ActorContext.Demo("creator"), "ATOMIC", "Atomic", "Test");

        Assert.True(result.IsSuccess);
        Assert.True(governedPersistence.TransactionUsed);
        Assert.True(definitionPersistence.SavedInsideTransaction);
        Assert.True(governedPersistence.AuditSavedInsideTransaction);
    }

    private sealed class FixedClock : IClock { public DateTimeOffset UtcNow => new(2026, 8, 9, 0, 0, 0, TimeSpan.Zero); }

    private sealed class RecordingAuthorizationDecision : IAuthorizationDecision
    {
        public bool Allow { get; set; } = true;
        public int CallCount { get; private set; }
        public List<string> Capabilities { get; } = [];

        public Task<AuthorizationDecision> DecideAsync(ActorIdentity actor, KpiCapabilityId capability, AuthorizationResource resource, DateTimeOffset effectiveAt, RepresentedAuthority? representedAuthority, CancellationToken cancellationToken)
        {
            CallCount++;
            Capabilities.Add(capability.Value);
            return Task.FromResult(Allow
                ? AuthorizationDecision.Allow(actor.OrganizationId, capability, resource, effectiveAt)
                : AuthorizationDecision.Deny(AuthorizationDecisionReason.MissingCapability, actor.OrganizationId, capability, resource, effectiveAt));
        }
    }

    private sealed class RecordingDefinitionPersistence : IKpiDefinitionPersistence
    {
        public bool SavedInsideTransaction { get; private set; }
        public bool InTransaction { get; set; }
        public void Save(KpiDefinition definition) => SavedInsideTransaction = InTransaction;
        public IReadOnlyList<KpiDefinition> LoadAll(Guid? organizationId = null) => [];
    }

    private sealed class RecordingGovernedPersistence : IKpiGovernedPersistence
    {
        private readonly RecordingDefinitionPersistence _definitionPersistence;
        public bool TransactionUsed { get; private set; }
        public bool AuditSavedInsideTransaction { get; private set; }
        private bool _inTransaction;

        public RecordingGovernedPersistence(RecordingDefinitionPersistence definitionPersistence) => _definitionPersistence = definitionPersistence;

        public void ExecuteInTransaction(Action mutation)
        {
            TransactionUsed = true;
            _inTransaction = true;
            _definitionPersistence.InTransaction = true;
            mutation();
            _definitionPersistence.InTransaction = false;
            _inTransaction = false;
        }

        public void SavePeriod(KpiPeriod period) { }
        public void SaveEvaluation(Guid organizationId, KpiEvaluation evaluation) { }
        public void SaveAudit(AuditRecord record) => AuditSavedInsideTransaction = _inTransaction;
    }
}
