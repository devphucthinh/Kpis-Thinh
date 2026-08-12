# Organization KPI Workspace Integration Contract

This contract phases the product-owner-approved workspace without moving KPI
Planning, Cascade, Actual, or Evaluation behavior into the Organization and
Authorization foundation.

## Ownership matrix

| Capability | Feature 002 obligation | Later owner |
|---|---|---|
| Approved Organization tree | Implement approved-baseline lookup, lazy Unit/Position nodes, capability plus KPI Data Scope filtering, safe search, and allowed-action projection. | Reused by every later workspace slice. |
| Position selection | Implement URL-restorable Position, baseline, and effective-time context in MVC/Razor. Unit nodes expand/collapse only. | Later slices add period/segment/filter parameters without changing selection semantics. |
| Baseline context | Return exact `BaselineApplicabilitySegment` and reject a supplied baseline that is stale or not applicable at `effectiveAt`. | Planning/Evaluation add KPI `EffectiveSegment`; they must not rename baseline applicability. |
| KPI neighborhood | Publish the future coarse-grained read shape and one-edge invariant; show an honest unavailable state until providers exist. | Planning provides Plan and Employee responsibility, Cascade provides edges, Actual provides approved observations, Evaluation provides official result projections. |
| KPI details and metrics | Define frontend prohibition and feature-owner references only. | KPI Management/Planning/Actual/Evaluation provide durable definition, Target, Actual, Variance, score, correction, and timeline facts. |

Feature 002 acceptance must not claim the future fields are implemented from
fixtures, frontend calculations, or in-memory-only projections.

## Implemented foundation endpoint

`GET /api/v1/organizations/{organizationId}/organization-tree`

Required query state:

- `effectiveAt` — required instant for baseline, employment, assignment, and
  scope resolution;
- `baselineId` — optional assertion; a mismatch with `effectiveAt` is a stable
  context conflict;
- `parentUnitId` — optional lazy branch root;
- `positionId` — optional selected Position to reveal and restore;
- `search` and `continuationToken` — optional safe search/paging state.

The response is `OrganizationTreeResponse` from `openapi.yaml`. It returns the
applicable approved baseline, exact baseline-applicability segment, only nodes
visible through `organization.structure.view` plus applicable KPI Data Scope,
and a backend-authorized action projection. Hidden matches are omitted without
revealing their identifiers or counts.

The approved baseline supplies structure/workforce context only; it does not
contain Role Assignments. Capability and scope come from current committed
authorization facts (or the still-active fixed bootstrap profile before
handoff) and are re-evaluated for every query/action. A subsequent query must
observe a committed revoke, scope change, baseline change, or bootstrap handoff.

Unit nodes are navigation branches and cannot select KPI context. Position
nodes are selectable. Razor does not traverse the organization graph or infer
scope; it renders this read model.

## Foundation MVC URL state

The server-rendered workspace preserves these parameters:

```text
/organization/kpi-workspace
  ?positionId={positionId}
  &effectiveAt={instant}
  &baselineId={baselineId}
  &parentUnitId={unitId}
  &search={term}
```

Refresh, back, forward, and shared URLs restore the same authorized context.
An out-of-scope Position URL returns a safe forbidden/not-found experience and
does not select the nearest visible Position. At 390 pixels the tree moves into
the **Chọn vị trí** drawer; the same URL remains authoritative.

Period, KPI Effective Segment, result mode, and KPI filters are reserved for
later slices and must not be populated with synthetic values by feature 002.

## Published Effective Segment integration key

`EffectiveSegmentContract` in `openapi.yaml` is an immutable consumer contract:

```text
PeriodId + SegmentId + EffectiveInterval
+ BaselineId
+ PlanRevisionId
+ AssignmentWeightSnapshotId
+ AggregationPolicyId/Version
```

Feature 002 validates contract shape and baseline linkage only. Planning owns
the plan and responsibility snapshots; Evaluation owns segment calculation and
whole-period aggregation. The contract contains no result field.

## Future KPI neighborhood read contract

The later features jointly expose one coarse-grained endpoint after their
durable facts exist:

```http
GET /api/v1/organizations/{organizationId}/positions/{positionId}/kpi-neighborhood
    ?periodId=...
    &segmentId=...
    &resultMode=segment|wholePeriod
```

This endpoint is deliberately not an implemented feature-002 OpenAPI path. Its
versioned response must contain:

```text
OrganizationKpiNeighborhood
  organizationId
  selectedPosition
  period
  baseline
  effectiveSegment
  resultMode
  kpis[]                  # each KPI summary transferred once
  relationshipEdges[]    # directParent | selectedPosition | directChild
  assignmentCounts
  allowedActions[]
  contextToken
```

Every returned parent or child edge is exactly one edge from a KPI owned by the
selected Position. The endpoint never recursively expands grandparents or
grandchildren. The server performs traversal and deduplication.

The response and any detail/assignment disclosure distinguish:

- KPI Plan Weight;
- Child-to-Parent Contribution Weight;
- Employee Responsibility Weight.

They are separate typed fields and invariants, never one generic `weight`.

Official Target, Actual, Variance, score, missing-data classification,
correction, and whole-period aggregation come only from their backend owners.
The frontend does not add, average, prorate, normalize, score, or replace
missing values with zero.

## Context and error contract

All reads use stable Problem Details and a correlation ID.

| Condition | Safe result |
|---|---|
| Missing capability or scope | `403`, or `404` when existence must be hidden. |
| No applicable approved baseline | `422 baseline_missing`. |
| Supplied baseline is not applicable at `effectiveAt` | `409 baseline_context_stale` with the safely visible current baseline/segment identity. |
| Later KPI Effective Segment or Plan revision changed | `409 kpi_workspace_context_stale`; UI offers reload and never merges contexts. |
| No Position selected | Successful instructional UI state, no KPI request. |
| Position has no KPI after later providers exist | Successful explicit empty state, not forbidden and not synthetic data. |
| Official result missing | Explicit missing state, never numeric zero. |

UI action projection is a usability aid. Every direct URL and governed command
is authorized again through the Application decision interface.

## Acceptance split

Feature 002 proves:

1. approved-baseline tree data survives PostgreSQL/Web restart;
2. Units expand only and Positions select context;
3. scope filtering and out-of-scope URLs are safe;
4. Position/baseline/effective-time URL state survives refresh/back/forward;
5. keyboard operation, focus restoration, light/dark states, and the 390-pixel
   drawer work in MVC/Razor;
6. the shell labels the KPI neighborhood unavailable until later providers
   exist and does not show fixture metrics.

Later named features prove the direct parent/current/direct child table,
Employee disclosure, KPI details, three weight types, segment switching,
official metrics, filters, and PostgreSQL evidence for those facts.
