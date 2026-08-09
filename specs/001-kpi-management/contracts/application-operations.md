# Application Operation Contract

**Purpose**: Define public behavior seams between delivery and governed KPI behavior. These operations are command/query contracts, not direct persistence access.

## Common command envelope

Every governed command carries:

- `ActorContext` — actor id, organization id, demonstrated capability, correlation id;
- requested action data;
- opaque concurrency token when changing an editable resource;
- command timestamp/clock supplied by the application boundary.

The operation returns either a typed success/read model or a stable failure. The delivery layer maps these results to localized HTTP/UI responses without reimplementing rules.

## KPI Definition and Version operations

| Operation | Required behavior |
|---|---|
| Create Definition | Validate company-unique KPI Code and Creator capability; create initial Draft context and Audit Record. |
| Update Draft | Require owner/capability and fresh token; reject published/submitted content mutation. |
| Create/clone Version | Allocate sequential number; clone retired behavior only into a new Draft with Change Summary. |
| Submit/approve/reject | Enforce valid Draft, Policy Approver decision-only role and review comment. |
| Publish/retire | Enforce approval, legal effective range and predecessor/successor transition in one transaction. |
| Archive/restore/delete/transfer | Enforce Draft deletion eligibility, audit tombstone, no automatic reactivation and approved ownership transfer reason. |

## Formula operations

| Operation | Required behavior |
|---|---|
| Validate Formula | Compile source and return generated Formula Document or diagnostics. |
| Formula Test Run | Compile/evaluate a Draft with test inputs; return outcome only; no Evaluation/Audit persistence. |

## KPI Period operations

| Operation | Required behavior |
|---|---|
| Create/update Draft Plan | Validate cadence/date/selection rules with fresh token. |
| Submit/approve/reject/cancel | Enforce Period Planner/Approver separation and explicit lifecycle. |
| Amend | Propose a separately governed amendment without mutating frozen plan. |
| Reconcile time | Idempotently activate/close only due state-qualified Periods and audit actual transitions. |

## Evaluation and Audit operations

| Operation | Required behavior |
|---|---|
| Create official Evaluation | Require Active Activation, resolve defaults, call formula seam, persist immutable attempt and Current success atomically. |
| Correct Evaluation | Require existing successful same-Version predecessor, full new input snapshot and reason; persist successor/diff atomically. |
| Read Current/history | Resolve one Current successful result plus all immutable attempts. |
| Read Audit | Filter append-only Audit Records by entity, actor, event type and date. |

## Failure families

`VALIDATION`, `LIFECYCLE_CONFLICT`, `AUTHORIZATION_DENIED`, `SELF_APPROVAL_FORBIDDEN`, `EFFECTIVE_RANGE_CONFLICT`, `PERIOD_ELIGIBILITY_CONFLICT`, `FORMULA_*`, `EVALUATION_*`, `CORRECTION_CONFLICT`, `CONCURRENCY_CONFLICT`, and `INFRASTRUCTURE_FAILURE` are distinguishable. Only the final family is unexpected/technical; all other families are expected business behavior.
