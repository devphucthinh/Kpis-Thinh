# Phase 0 Research: Organization and Authorization Foundation

## Decision 1: Keep the pinned .NET 9 modular stack

**Decision**: Implement on SDK `9.0.315`, `net9.0`, ASP.NET Core/EF Core
`9.0.16`, Npgsql EF Core `9.0.4`, xUnit v3, and Playwright `1.55.0`. Do not add
an authorization/workflow/graph framework.

**Rationale**: These versions are already centrally pinned, restored in locked
mode, verified by the harness, and match ADR 0002 and the production-porting
constraint. The domain rules are specific enough that a generic workflow or
RBAC framework would expose a larger, shallower interface than the application
needs.

**Alternatives considered**:

- Upgrade to .NET 10: rejected by the approved .NET 9 constitution and target
  compatibility requirement.
- Introduce a third-party authorization/workflow engine: rejected because
  capability, scope, separation of duty, effective baseline, and audit rules
  remain product domain behavior.

## Decision 2: Make authorization resource-based and capability-oriented

**Decision**: Every governed Application command calls one deep
`IAuthorizationDecision` interface with actor, atomic capability identifier,
resource facts, effective time, and same-artifact actors. The result is an
explainable Allow/Deny decision with a stable reason code. Role display names
and navigation state are never inputs.

**Rationale**: ASP.NET Core declarative attributes run before a resource is
loaded and cannot evaluate Organization Unit subtree, assigned responsibility,
effective authority, or separation of duty. Microsoft documents imperative
resource-based authorization for this case. Keeping the authoritative decision
inside Application also makes MVC, JSON, background work, and tests share the
same behavior.

**Alternatives considered**:

- `[Authorize(Roles=...)]`: rejected because roles are dynamic bundles and role
  names are explicitly non-authoritative.
- Controller-local checks: rejected because they duplicate policy and leave
  non-HTTP callers unprotected.
- Put all effective capabilities into identity claims: rejected because scoped
  assignments, employment, account state, delegation, and baselines change
  independently and require current durable facts.

**Primary source**: [Resource-based authorization in ASP.NET Core 9](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resource-based?view=aspnetcore-9.0).

## Decision 3: Use a product-owned fixed capability catalog

**Decision**: Capability definitions are immutable product metadata identified
by stable dotted codes such as `organization.structure.edit` and
`security.role-assignment.approve`. The Domain catalog supplies business area,
risk class, allowed scope kinds, description, and incompatible-combination
warnings. Custom role versions store an ordered set of those identifiers.

**Rationale**: This provides a Microsoft 365 Admin Center-style business-task
experience while preventing Organization administrators from inventing new
authorization semantics. Product changes to the catalog are code/migration
changes and receive normal review.

**Alternatives considered**:

- Bit flags in `ActorContext`: rejected because the catalog must grow without
  integer-width coupling and assignments must be durable, versioned, scoped,
  and independently approved.
- User-created capabilities: rejected by the spec and because arbitrary names
  cannot safely acquire server-side enforcement logic.

## Decision 4: Evaluate a mandatory system security floor before Organization policy

**Decision**: The system floor defines the least restrictive risk level and
largest safe data scope that may avoid independent approval. An Organization
policy may lower the permitted risk and narrow the safe scope, never raise or
widen them. The effective policy is the stricter result field by field.

**Rationale**: A monotonic merge is understandable, testable, and makes it
impossible for an Organization configuration bug to weaken the product floor.

**Alternatives considered**:

- Replace the system policy per Organization: rejected because it permits a
  weaker tenant configuration.
- Hard-code every assignment as privileged: safe but rejected because it adds
  unnecessary review to low-risk, self-scoped work.

## Decision 5: Store effective time as half-open UTC intervals with Organization timezone

**Decision**: Persist effective ranges as `[from, to)` instants in UTC and
persist one IANA/Windows-mappable timezone identifier on Organization for input
and display. An omitted end means infinity. Only one approved baseline may
contain any instant for one Organization.

**Rationale**: Half-open intervals avoid double ownership at boundaries. UTC
instants are unambiguous across daylight-saving changes, while the Organization
timezone preserves business meaning in the UI.

**Alternatives considered**:

- Server-local time: rejected because it changes with host location and is
  ambiguous at daylight-saving boundaries.
- Inclusive end timestamps: rejected because adjacent effective records would
  overlap at the shared boundary.

## Decision 6: Use immutable reviewed revisions and normalized runtime facts

**Decision**: Editing occurs in a structure workspace with an optimistic
revision. Submission freezes an immutable `OrganizationStructureRevision`.
Approval creates an immutable baseline that references that exact revision and
copies query-critical structure members into baseline-owned relational rows.
Selector evidence, approval explanations, and exact reviewed documents are
stored as JSONB snapshots; runtime scope and effective facts remain relational.

**Rationale**: Reviewed content cannot drift, relational members support tree
and scope queries, and JSONB preserves the exact explanation without turning
current authorization into an unindexed document query.

**Alternatives considered**:

- Mutable organization tables plus approval status: rejected because later
  edits could rewrite what was approved.
- JSONB-only baselines: rejected because subtree scope and approver resolution
  are frequent, security-sensitive queries.
- Event sourcing: rejected by ADR 0002 and unnecessary for immutable revisions
  plus append-only audit.

## Decision 7: Enforce cross-row temporal invariants in PostgreSQL

**Decision**: Use PostgreSQL `tstzrange` for approved baseline, assignment, and
delegation effective ranges. A GiST exclusion constraint protects non-overlap
where the rule is absolute (especially one baseline per Organization). Domain
validation produces friendly paths before persistence; named database
constraints remain the race-safe final guard.

**Rationale**: PostgreSQL explicitly recommends `UNIQUE`, `EXCLUDE`, or foreign
keys for cross-row constraints rather than `CHECK` expressions that inspect
other rows. Exclusion constraints are designed for non-overlapping ranges.

**Alternatives considered**:

- Application check only: rejected because two concurrent approvals could both
  observe no conflict and commit overlapping ranges.
- Trigger-only overlap checks: rejected because a declarative named exclusion
  constraint is easier to inspect and verify.

**Primary sources**: [PostgreSQL 18 constraints](https://www.postgresql.org/docs/18/ddl-constraints.html) and [PostgreSQL range types](https://www.postgresql.org/docs/18/rangetypes.html).

## Decision 8: Use optimistic concurrency for editable heads

**Decision**: Mutable workspace heads, Organization policies, role display
metadata, and proposed assignments use PostgreSQL `xmin` mapped to a `uint`
row-version plus a domain revision. Immutable submissions, baselines, role
versions, route snapshots, decisions, and audit rows are append-only and do not
offer update/delete interfaces.

**Rationale**: EF Core uses concurrency tokens to detect changes after a read;
Npgsql documents `xmin` as an automatically changing PostgreSQL concurrency
token and the repository already uses the same mapping for KPI definitions.

**Alternatives considered**:

- Last-write-wins: rejected because it violates stale-revision requirements.
- Application-generated timestamps: rejected because equal or reordered clock
  values are weaker than the database concurrency token.

**Primary sources**: [EF Core concurrency handling](https://learn.microsoft.com/en-us/ef/core/saving/concurrency) and [Npgsql concurrency tokens](https://www.npgsql.org/efcore/modeling/concurrency.html?tabs=fluent-api).

## Decision 9: Snapshot approval routes at submission

**Decision**: Route definitions contain ordered selectors and explicit
fallbacks. Submission resolves them using the applicable approved baseline and
stores candidate/resolved identities, evidence, scope, and baseline revision in
an immutable route snapshot. Later organization changes do not alter it.

**Rationale**: A decision must remain explainable from the facts visible at
submission time. Re-resolving from the current structure would silently change
the approver and destroy audit reproducibility.

**Alternatives considered**:

- Resolve each stage just in time from current structure: rejected because the
  route would drift after manager or Position changes.
- Store only the chosen actor: rejected because it loses selector and fallback
  evidence.

## Decision 10: Treat delegation as constrained represented authority

**Decision**: A delegate decision authorizes against the intersection of the
original authority, delegation responsibility, data scope, effective range,
and delegate account/employment eligibility. Both identities and the delegation
identifier enter the decision and audit record. Delegation never changes the
stored route snapshot.

**Rationale**: Intersection prevents delegation from expanding capability or
scope and keeps the original accountable actor visible.

**Alternatives considered**:

- Copy roles to the delegate: rejected because copied assignments outlive or
  exceed the intended approval duty.
- Replace the approver in the route: rejected because it erases who was
  originally selected.

## Decision 11: Make audit a transactional immutable business fact

**Decision**: Every accepted and rejected governed action writes an Audit
Record in the same transaction as its state change (or denial evidence where no
state changes). Audit stores actor, represented authority, Organization,
resource/revision, capability/action, decision/reason, scope summary,
correlation, and time. PostgreSQL update/delete protection extends the current
append-only model.

**Rationale**: Technical logs are neither durable business evidence nor
transactionally tied to the decision. The timeline can project this immutable
record and apply scope checks without changing it.

**Alternatives considered**:

- Application logs: rejected because retention and transactional guarantees are
  insufficient.
- Mutable timeline rows: rejected because explanations could be rewritten.

## Decision 12: Keep re-cascade calculation deterministic but plan-neutral

**Decision**: The foundation owns a pure proportional allocator that accepts
prior ordered assignments, fixed new weights, and precision. It scales existing
weights into the remainder, floors to precision, allocates residual units by
largest remainder, and breaks ties by prior order then stable assignment ID.
The result is a preview plus proof totals; it does not mutate a KPI Plan.

**Rationale**: The algorithm is required now and must be identical for future
Planning consumers, while KPI Plan amendment persistence is explicitly outside
this feature. Keeping it pure makes the interface reusable and testable.

**Alternatives considered**:

- Round each weight independently: rejected because the sum may not equal 100.
- Give the residual to the largest original weight: rejected because it ignores
  fractional remainder and can bias repeated amendments.
- Apply changes directly to existing KPI data: rejected because no governed
  plan-amendment aggregate exists in this feature.

## Decision 13: Preserve effective boundaries for later segment aggregation

**Decision**: Baseline approval writes a `BaselineChangeImpact` containing old
and new baseline IDs, effective instant, changed unit/position/employee/
assignment IDs, and `RequiresRecascade`. The contract defines immutable
effective segments keyed by baseline/plan revision. Planning later attaches
actual KPI responsibility changes; Evaluation later combines segment outcomes
only through the approved KPI Aggregation Policy.

**Rationale**: The foundation can freeze organization causality now without
pulling Strategy, Planning, or Evaluation implementation into this slice. It
also prevents later modules from recomputing old facts using a new baseline.

**Alternatives considered**:

- Recompute the whole KPI period with the newest organization: rejected because
  it rewrites history.
- Implement complete KPI plan amendments in this feature: rejected by the
  approved feature boundary.

## Decision 14: Keep the production UI C#/Razor-first

**Decision**: Organization tree, role editor, scope picker, approval queue, and
timeline use MVC controllers, strongly typed ViewModels, Razor, CSS, and
server-rendered HTML/SVG. Existing generic theme behavior may remain. No new
business JavaScript is introduced without product-owner approval.

**Rationale**: This matches the approved production direction, keeps backend
rules authoritative, and remains portable to `BSC-KPIs`.

**Alternatives considered**:

- SPA/Blazor rewrite: rejected because it changes the approved delivery stack.
- Client-side authorization: rejected because visibility cannot authorize a
  governed command.

## Resolved unknowns

All Technical Context items are resolved. The following cross-feature ownership
is intentional rather than unresolved:

- This feature creates the baseline boundary, impact fact, and weight preview.
- The later Planning feature applies a governed KPI plan amendment/re-cascade.
- The later Evaluation feature calculates and combines official segment results.
