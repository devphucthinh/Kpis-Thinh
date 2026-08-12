# Baseline Impact Resolution Application Contract

This is an in-process cross-feature contract. It is not an HTTP operation and
does not authorize an administrator to toggle impact status. The later KPI
Planning feature consumes it from the governed KPI Plan Amendment approval
command; the Organization foundation owns the registrar and durable resolution
fact.

## Interfaces

```csharp
public sealed record ApprovedKpiPlanAmendmentReference(
    Guid OrganizationId,
    Guid PlanAmendmentId,
    long PlanAmendmentRevision,
    Guid AppliedBaselineId,
    Guid ApprovalDecisionId,
    string AmendmentContentHash,
    Guid ApprovedByEmployeeId,
    DateTimeOffset ApprovedAt);

public interface IApprovedKpiPlanAmendmentReferenceReader
{
    Task<ApprovedKpiPlanAmendmentReference?> GetApprovedAsync(
        Guid organizationId,
        Guid planAmendmentId,
        long planAmendmentRevision,
        CancellationToken cancellationToken);
}

public sealed record RegisterBaselineImpactResolutionCommand(
    Guid OrganizationId,
    Guid BaselineChangeImpactId,
    Guid PlanAmendmentId,
    long PlanAmendmentRevision,
    Guid ActingEmployeeId,
    string CorrelationId);

public enum BaselineImpactStatus
{
    Detected,
    Resolved
}

public sealed record BaselineImpactResolutionResult(
    Guid ResolutionId,
    Guid BaselineChangeImpactId,
    Guid PlanAmendmentId,
    long PlanAmendmentRevision,
    Guid ApprovalDecisionId,
    BaselineImpactStatus DerivedImpactStatus,
    bool WasExisting);

public interface IBaselineImpactResolutionRegistrar
{
    Task<BaselineImpactResolutionResult> RegisterAsync(
        RegisterBaselineImpactResolutionCommand command,
        CancellationToken cancellationToken);
}
```

`IApprovedKpiPlanAmendmentReferenceReader` is implemented by Planning after its
aggregate exists. Feature 002 supplies only a deterministic contract-test
adapter; no production-looking KPI Plan fixture is exposed through Web.

## Command boundary

The Planning approval Application command is the only production caller. It:

1. authorizes and records the independent KPI Plan Amendment decision;
2. advances the exact amendment revision to `Approved`;
3. calls `RegisterAsync` before the shared Application unit of work commits;
4. commits the amendment approval, `BaselineImpactResolution`, and foundation
   Audit Record atomically.

There is no separately assignable `resolve impact` capability. The human actor
is authorized by the later Planning approval capability and scope; the
registrar is an internal consequence boundary and re-checks Organization,
impact visibility, baseline identity, and separation from unsafe cross-
Organization references. MVC, JSON controllers, Razor, and background jobs do
not call it directly.

## Validation and result rules

The registrar loads the immutable impact, then calls the reader with the exact
Organization/amendment/revision. It accepts only evidence that:

- exists and is independently approved;
- belongs to the same Organization as the impact;
- has `AppliedBaselineId` equal to the impact's `NewBaselineId`;
- carries a non-empty content hash and exact approval decision/actor/time;
- is observed inside the caller's shared unit of work.

On success it inserts one immutable `BaselineImpactResolution` and Audit Record.
The result contains the new resolution plus derived impact status `Resolved`.

Idempotency and concurrency are deterministic:

- retrying the same impact + amendment ID + revision + decision + content hash
  returns the existing result and writes no second Audit Record;
- a different reference for an already resolved impact returns
  `baseline_impact.already_resolved`;
- concurrent different references are serialized by the unique
  `(OrganizationId, BaselineChangeImpactId)` constraint; the loser reloads and
  maps to the same stable conflict;
- one approved amendment may resolve more than one impact when each impact's
  baseline identity matches.

## Failure contract

| Condition | Application outcome |
|---|---|
| Impact absent or unsafe cross-Organization lookup | Safe not-found/deny; no protected identity disclosed. |
| Amendment evidence absent or not approved | `baseline_impact.approved_amendment_required`. |
| Amendment Organization differs | Safe not-found/deny. |
| Applied baseline differs from impact successor | `baseline_impact.baseline_mismatch`. |
| Exact retry | Existing resolution returned; no duplicate write/audit. |
| Different reference after resolution | `baseline_impact.already_resolved`. |
| Provider or transaction failure | Entire Planning approval and resolution unit of work rolls back. |

If a future deployment separates Planning from the foundation process/database,
an ADR must replace this atomic in-process contract with an outbox/idempotent
consumer protocol. Feature 002 does not pre-build that deployment model.

## Required contract tests

Tests use the same registrar and PostgreSQL adapter intended for production and
a deterministic Planning evidence-reader adapter. They prove:

1. missing and unapproved evidence cannot resolve an impact;
2. cross-Organization and baseline-mismatched evidence is rejected safely;
3. exact approved evidence creates one resolution and one Audit Record;
4. exact retry is idempotent;
5. a different or concurrent reference cannot replace the first resolution;
6. resolution and Audit evidence survive a fresh DbContext/Web restart;
7. a simulated consumer transaction marker, resolution, and audit roll back
   together, proving shared-unit-of-work participation without inventing a KPI
   Plan aggregate in feature 002.

The later Planning feature must add the consumer-side integration test proving
that its real amendment approval, the resolution, and the Audit Record commit or
roll back together.
