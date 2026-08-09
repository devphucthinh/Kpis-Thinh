# Data Model: Governed KPI Management

**Feature**: `001-kpi-management`  
**Behavior contract**: [spec.md](spec.md)  
**Canonical language**: [`CONTEXT.md`](../../CONTEXT.md)

## Target Model and Migration Sequencing

This document records the target logical end state. It does not require every listed table, constraint, or relationship to be introduced in one migration. The approved additive migration order in [plan.md](plan.md) delivers only the persistence required by each verified vertical behavior; applied migrations move forward and do not rewrite immutable KPI Evaluation or Audit Record history.

## Aggregate and Reference Map

```text
Organization
 ├─ Actors
 ├─ KPI Definitions
 │   └─ KPI Versions
 ├─ KPI Periods
 │   ├─ KPI Period Amendment effective revisions
 │   └─ KPI Period Activations -> exact KPI Version + effective revision
 │        └─ KPI Evaluation attempt stream
 └─ Audit Records
```

| Concept | Boundary | Identity / relationship | Mutable state |
|---|---|---|---|
| Organization | Reference | Stable organization id; one seeded instance in MVP. | None relevant to KPI flow. |
| Actor | Reference | Stable actor id, organization id, demonstrated capability. | Demo persona selection is outside this record. |
| KPI Definition | Aggregate root | Stable id, organization id, immutable KPI Code, current owner id. | Draft metadata, archive state, concurrency token. |
| KPI Version | Entity inside Definition boundary | Stable id, Definition id, sequential version number, optional predecessor id. | Draft content/review/publication lifecycle; content immutable after submission. |
| Formula Variable | Value object inside Version | Canonical code and display order. | Draft-only replacement with Version content. |
| Formula Document | Value object inside Version/Evaluation snapshot | Exact source, server-generated typed AST, language/schema version, checksum. | Never client-authoritative; new semantics create a new Version. |
| KPI Period | Aggregate root | Stable id, organization id, human code, planner id, approver id. | Plan lifecycle, original frozen interval/selections, latest approved effective revision pointer and amendments. |
| KPI Period Amendment | Immutable reviewed revision inside Period boundary | Stable id, Period id, increasing revision number and base revision number. | InReview decision state only; approved/rejected content becomes immutable. |
| KPI Period Activation | Entity resolved from the latest approved Period effective revision | Period id + Definition id, exact Version id and effective revision number. | Active/closed context and Current evaluation reference. |
| KPI Evaluation | Immutable entity in Activation stream | Stable id, activation id, optional supersedes id. | Never edited; only successor can change the stream's Current result. |
| Audit Record | Append-only record | Stable id, organization/entity/actor/correlation ids. | No supported mutation. |

## KPI Definition and KPI Version

### KPI Definition fields

| Field | Rule |
|---|---|
| `id` | Stable UUID identity. |
| `organizationId` | Required company scope. |
| `kpiCode` | Required immutable uppercase snake case; unique per organization, case-insensitively. |
| `ownerActorId` | Current KPI Creator accountable for Draft content. |
| `archivedAt`, `archivedBy` | Present only when archived. |
| `revision` | Opaque optimistic concurrency value for editable metadata. |

### KPI Version fields

| Field | Rule |
|---|---|
| `id`, `definitionId`, `versionNumber` | Stable identity and unique increasing number inside one Definition. |
| `name`, `description` | Required, Vietnamese-capable human explanation. |
| `changeSummary`, `predecessorVersionId` | Change Summary required for successor/clone; predecessor optional for first version. |
| `formulaVariables` | Ordered list; each code/type/default is valid; maximum 100. |
| `formulaDocument` | Exact source plus generated typed AST; source is authoritative. |
| `declaredResultType` | Decimal or Boolean, must match bound formula. |
| `cadence` | Monthly, Quarterly, or Annual. |
| `status` | Draft, InReview, Rejected, Approved, Published, Retired. |
| `effectiveFrom`, `effectiveTo` | Half-open range for published historical applicability. |
| `submitted/approved/rejected/published/retired` facts | Actor/time/comment/reason as relevant. |
| `revision` | Optimistic token while Draft/editable. |

### Version invariants and transitions

```text
Draft --submit--> InReview --approve--> Approved --publish--> Published --retire--> Retired
                         \--reject--> Rejected --return to Draft--> Draft
```

- Only Draft content can change.
- Submit requires valid content; approvers do not edit.
- Publish requires approval, actor capability, and a non-overlapping effective range.
- A successor hand-off closes the predecessor range at its `effectiveFrom`; reconciliation retires predecessor once due.
- Retired Versions are historical only and must be cloned to become a new Draft.
- A Definition is hard-deletable only when its only Version is an unused never-submitted Draft. Any governed history permits archive/restore only.

## Formula Values and Formula Document

### Formula Variable Definition

| Field | Rule |
|---|---|
| `code` | Case-insensitive canonical `snake_case`; unique within Version. |
| `displayName`, `description` | Human-facing localized metadata. |
| `type` | Decimal or Boolean. |
| `required` | If true, an Evaluation Input/default must exist. |
| `defaultValue` | Optional, non-null, compatible with `type`. |
| `displayOrder` | Required and preserved exactly. |

### Formula Document / AST

```json
{
  "source": "IF(revenue > target, ROUND(revenue / target * 100, 2), 0)",
  "ast": {
    "nodeType": "If",
    "resultType": "Decimal",
    "span": { "start": 0, "length": 58 }
  }
}
```

The real AST includes closed node types for literals, variables, unary/binary operations, percentage and calls. Each node has node type, result type and source span. Decimal literals are invariant strings. The client receives AST as read data but does not author it.

### Formula evaluation values

| Value | Representation / rule |
|---|---|
| Decimal | `System.Decimal`; 28 significant digits and at most 10 fractional digits; invariant string at JSON/JSONB boundary. |
| Boolean | Native Boolean with explicit Formula Value type where polymorphic. |
| Null | Never a valid Formula Variable input or successful result. |
| Failure | Stable code, localization arguments, source span when applicable, optional details. |

## KPI Period Plan and Activation

### KPI Period fields

| Field | Rule |
|---|---|
| `id`, `organizationId`, `code` | Stable scoped identity and human code. |
| `name`, `description`, `cadence` | Required human context and Monthly/Quarterly/Annual cadence. |
| `start`, `end` | Required half-open interval interpreted in `Asia/Ho_Chi_Minh`, stored as unambiguous instants. |
| `plannerActorId`, `approverActorId` | Submitter and approving actor; must be different. |
| `status` | Draft, InReview, Rejected, Scheduled, Active, Closed, Cancelled. Rejected is read-only until Planner return to Draft. |
| `selections` | Original approved selection set; one exact eligible Version per Definition and frozen after approval. |
| `latestEffectiveRevisionNumber` | `0` for the original approved plan; advances only when a Scheduled Amendment is approved. |
| `amendments` | Ordered separately reviewed immutable effective revisions; never in-place changes to original selection/date. |
| `revision` | Optimistic token while editable. |

### KPI Period Amendment fields

| Field | Rule |
|---|---|
| `id`, `periodId`, `revisionNumber` | Stable identity and unique increasing revision number within one Period. |
| `baseRevisionNumber` | Latest approved effective revision on which the proposal was based; stale review cannot advance a different base. |
| `proposedStart`, `proposedEnd`, `proposedSelections` | Complete candidate effective plan snapshot, not an in-place delta; the same cadence, eligibility, duplicate and overlap rules apply. |
| `reason`, `proposedBy`, `proposedAt` | Required proposal provenance from the KPI Period Planner. |
| `status` | InReview, Approved or Rejected. |
| `reviewedBy`, `reviewedAt`, `reviewComment` | Required decision provenance from a distinct KPI Period Approver. |

### Activation fields

| Field | Rule |
|---|---|
| `periodId`, `definitionId`, `versionId`, `effectiveRevisionNumber` | Unique Period/Definition pair, exact historical Version reference and the original (`0`) or approved Amendment revision used at activation. |
| `activatedAt`, `closedAt` | Set by idempotent period reconciliation. |
| `currentSuccessfulEvaluationId` | Optional pointer to the latest successful immutable attempt. |

### Period invariants

- One Definition occurs only once in a Period.
- Version cadence matches Period cadence and its effective range covers the planned activation.
- Same-company/same-cadence Period ranges do not overlap.
- A Definition cannot be active in overlapping Periods.
- Rejection moves InReview to Rejected; only the Planner may return the read-only plan to Draft, while rejection evidence remains immutable.
- Approval freezes the original dates and selections. Only Scheduled permits Amendment proposal/review; approval advances `latestEffectiveRevisionNumber` to a new immutable complete snapshot without rewriting the original or an earlier revision.
- Amendment approval revalidates cadence, eligibility and overlap under the Period lock; a stale `baseRevisionNumber` is rejected with no Amendment/Audit partial commit.
- Scheduled → Active atomically resolves the latest approved effective revision and creates/activates all selections from it; Active → Closed blocks ordinary new Evaluations.

## KPI Evaluation and Supersession

| Field | Rule |
|---|---|
| `id`, `activationId`, `versionId` | Stable attempt identity and exact active Version reference. |
| `formulaSnapshot` | Stored Formula Document/version snapshot used for reproducibility. |
| `inputSnapshot` | Ordered resolved inputs after defaults; no null value. |
| `outcome` | Success Decimal/Boolean or structured Failure. |
| `evaluatorActorId`, `evaluatedAt` | Required attempt facts. |
| `supersedesEvaluationId` | Optional predecessor for correction. |
| `correctionReason`, `correctionDiff` | Required for Superseding Evaluation; diff generated server-side. |
| `isCurrentSuccessful` / pointer | True for at most one Success in an Activation stream. |

### Evaluation invariants

- Official Evaluation is valid only for an Active Activation; Test Run has no Evaluation identity or persistence path.
- Every attempt is immutable.
- Failure is historical evidence but never becomes Current.
- Correction retains predecessor and is same-Version only; it creates a complete new input snapshot and mandatory reason/diff.
- Closed Period allows governed correction of an existing successful Evaluation, not an ordinary new Evaluation.

## Audit Record

| Field | Rule |
|---|---|
| `id`, `organizationId`, `actorId`, `occurredAt` | Required immutable provenance. |
| `eventType`, `entityType`, `entityId` | Identifies governed action and target. |
| `reason`, `correlationId` | Required when business rule requires a reason; ties multi-step command work together. |
| `changeSummary` | Concise structured before/after context; tombstone preserves logical identity for hard delete. |

Audit rows are not an event source. They are a user-queryable, append-only explanation of governed decisions and transitions.

## Persistence Relationship and Constraint Summary

```text
organizations 1--* actors
organizations 1--* kpi_definitions 1--* kpi_versions
organizations 1--* kpi_periods 1--* kpi_period_amendments
kpi_periods 1--* kpi_period_activations *--1 kpi_versions
kpi_period_activations 1--* kpi_evaluations
kpi_evaluations 0..1--0..1 kpi_evaluations (supersedes)
organizations 1--* audit_records
```

| Integrity rule | Enforcement |
|---|---|
| Code/version/activation uniqueness | Unique indexes. |
| Amendment revision uniqueness and approved base progression | Unique Period/revision index plus Period lock and stale-base validation. |
| Version effective-range and period cadence-range overlap | PostgreSQL range exclusion constraints. |
| One Current success | Partial unique index plus activation lock. |
| Cross-table Definition active-period overlap | Approval transaction + scoped lock + integration concurrency test. |
| Immutable Evaluation/history relationships | Foreign keys, insert-only application behavior and no update commands. |
| Append-only Audit Record | Runtime grants + trigger + append-only store interface. |
| Stale editable changes | `xmin` concurrency token. |

## Migration Ledger and Runtime Boundary

Schema evolution is tracked separately from product data:

| Table | Columns | Invariant |
|---|---|---|
| `kpi_schema_migrations` | `id` text primary key, `checksum` text not null, `applied_at` timestamptz not null | One row per ordered manifest entry; an applied ID with a different checksum is a hard failure. |

The ledger is created by the explicit `Kpi.Migrator` command in the same
transaction that applies the first product migration. Product migrations are
forward-only and additive. There is no down-migration or `EnsureCreated` path.
The migrator validates the connected database name against the configured
`kpi_lab`/`kpi_lab_test` allow-list before opening a transaction, uses the
privileged migration connection, and reports applied versus already-skipped
manifest entries without exposing credentials. The Web host uses a separate
runtime connection and never invokes the migrator at startup.
