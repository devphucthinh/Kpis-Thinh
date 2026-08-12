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
and display. An omitted end means infinity. Before the first baseline begins,
dependent operations are denied; after that first instant, applicability
segments form a gapless, non-overlapping chain with one open tail.

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
The reviewed baseline content never changes; a separate applicability segment
may be closed exactly once by the atomic successor approval transaction.

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

**Decision**: Use PostgreSQL `tstzrange` for baseline applicability,
assignment, and delegation effective ranges. A GiST exclusion constraint
protects non-overlap. Successor-baseline approval also locks one Organization
baseline-chain owner row and atomically closes the current open applicability
segment at the exact successor start while inserting the new open segment. No
standalone close operation exists. Domain validation produces friendly paths;
the lock, predecessor link, unique open tail, and named database constraints
remain the race-safe final guards against gaps, overlap, and branching.

**Rationale**: PostgreSQL explicitly recommends `UNIQUE`, `EXCLUDE`, or foreign
keys for cross-row constraints rather than `CHECK` expressions that inspect
other rows. Exclusion constraints are designed for non-overlapping ranges.

**Alternatives considered**:

- Application check only: rejected because two concurrent approvals could both
  observe no conflict and commit overlapping ranges.
- Trigger-only overlap checks: rejected because a declarative named exclusion
  constraint is easier to inspect and verify.
- Non-overlap without serialized chain continuity: rejected because it permits
  gaps that break route, scope, and segment resolution.

**Primary sources**: [PostgreSQL 18 constraints](https://www.postgresql.org/docs/18/ddl-constraints.html) and [PostgreSQL range types](https://www.postgresql.org/docs/18/rangetypes.html).

## Decision 8: Use optimistic concurrency for editable heads

**Decision**: Mutable workspace, Organization policy, Custom Role, Approval
Route, and proposed-assignment heads use PostgreSQL `xmin` mapped to a `uint`
row-version plus a domain revision. Creating a role or route version requires
the current opaque head token and exact base version; stale requests return a
stable HTTP 409 rather than creating branches. Immutable submissions, baseline
content, role/route versions, route snapshots, decisions, and audit rows are
append-only and do not offer update/delete interfaces.

**Rationale**: EF Core uses concurrency tokens to detect changes after a read;
Npgsql documents `xmin` as an automatically changing PostgreSQL concurrency
token and the repository already uses the same mapping for KPI definitions.

**Alternatives considered**:

- Last-write-wins: rejected because it violates stale-revision requirements.
- Application-generated timestamps: rejected because equal or reordered clock
  values are weaker than the database concurrency token.

**Primary sources**: [EF Core concurrency handling](https://learn.microsoft.com/en-us/ef/core/saving/concurrency) and [Npgsql concurrency tokens](https://www.npgsql.org/efcore/modeling/concurrency.html?tabs=fluent-api).

## Decision 9: Independently approve route versions before activation

**Decision**: Route definitions have optimistic mutable heads and immutable
versions containing ordered selectors and explicit fallbacks. A version follows
`Draft -> PendingApproval -> Approved -> Active` or ends as `Rejected`/
`Retired`. Submission freezes its maker and reviewed content. A different actor
with `approval.route.approve` and applicable scope records an immutable decision;
the maker/editor cannot approve or activate that version. Activation requires
an approved version, `approval.route.activate`, a different eligible actor, the
current route-head token, and the current artifact-type activation-slot token.
Versioned JSON contracts expose every state transition and stable stale-head/
stale-slot HTTP 409 behavior.

Submission of a governed artifact resolves only the active version using the
applicable approved baseline and stores candidate/resolved identities, Position
context, group membership, fallback evidence, scope, and baseline revision in an
immutable route snapshot. Later organization or group changes do not alter it.

**Rationale**: Route configuration controls who may authorize product changes,
so direct maker activation would be an indirect privilege-escalation path. A
separate immutable review makes the configuration decision explainable, while
snapshotting preserves the facts visible when each governed artifact was
submitted.

**Alternatives considered**:

- Resolve each stage just in time from current structure: rejected because the
  route would drift after manager or Position changes.
- Store only the chosen actor: rejected because it loses selector and fallback
  evidence.
- Treat validation as approval: rejected because syntax/eligibility validation
  cannot provide independent governance or separation of duty.

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

## Decision 13: Preserve effective boundaries and register immutable resolution

**Decision**: Baseline approval writes an immutable `BaselineChangeImpact`
containing old/new baseline IDs, effective instant, changed unit/position/
employee/assignment IDs, and `RequiresRecascade`. It has no mutable status or
resolution fields. A separate one-per-impact `BaselineImpactResolution` stores
the exact approved KPI Plan Amendment ID/revision, approval decision, content
hash, actor, and time.

Later Planning supplies an `IApprovedKpiPlanAmendmentReferenceReader` and calls
the foundation's in-process `IBaselineImpactResolutionRegistrar` from the
governed amendment approval command before the shared unit of work commits. The
registrar reloads authoritative approved evidence, verifies Organization and
new-baseline identity, writes the resolution plus Audit Record atomically, and
returns the existing fact for an exact retry. Missing/unapproved/cross-
Organization evidence is denied and a different reference for an already
resolved impact is a stable conflict. There is no public resolve endpoint.

This feature executes contract tests using a deterministic Planning-consumer
adapter. Planning later supplies the production reader and applies actual KPI
responsibility changes; Evaluation later combines official segment outcomes
only through the approved KPI Aggregation Policy.

**Rationale**: Separate append-only facts preserve organization causality and
make `Resolved` a provable projection rather than an administrative toggle. An
in-process contract fits the modular monolith and lets Planning and foundation
commit together without publishing an HTTP operation that could bypass Plan
approval. The boundary can be tested now without pulling the KPI Plan aggregate
or official Evaluation behavior into this slice.

**Alternatives considered**:

- Recompute the whole KPI period with the newest organization: rejected because
  it rewrites history.
- Implement complete KPI plan amendments in this feature: rejected by the
  approved feature boundary.
- Mutate `ImpactStatus` and resolution columns on the impact row: rejected
  because the supposedly immutable bridge could then be resolved without
  durable amendment evidence.
- Publish a generic HTTP resolve endpoint: rejected because resolution is a
  consequence of the governed Planning approval command, not an independent
  user task.
- Use asynchronous events/outbox for this first modular-monolith seam: rejected
  as unnecessary when both modules share one Application unit of work; revisit
  only if the deployment boundary changes.

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

## Decision 15: Model effective-dated internal Approval Groups

**Decision**: `NamedGroup` selects an Organization-scoped internal
`ApprovalGroup`. The group is an optimistic mutable head; its Employee
memberships are effective-dated immutable facts with no overlapping interval for
the same group/Employee. Membership changes close or schedule facts rather than
rewriting history. Route resolution filters active, employed, in-scope members
at submission and freezes group ID, membership IDs, Employee IDs, eligibility
evidence, and resolution time in the route snapshot.

**Rationale**: An internal group is auditable during the first release and does
not make approval depend on an external identity-provider lookup. Effective
membership plus a frozen snapshot prevents a later group edit from changing an
already submitted route.

**Alternatives considered**:

- Read a live external identity group: deferred with production identity
  integration because it cannot currently satisfy restart and historical proof.
- Store an unversioned Employee list inside the route: rejected because it
  duplicates workforce identity and silently overwrites membership history.

## Decision 16: Make selector source semantics explicit

**Decision**: Selector contracts use a discriminator with kind-specific shapes.
`OrganizationUnitHead` requires both the relevant Organization Unit and the
explicit configured Employee; resolution verifies an active Position Assignment
in that exact unit plus employment, capability, and scope. `DirectManager`
obtains its subject Position from the governed artifact's Position/business-
responsibility context. Only an artifact with no Position context may fall back
to the subject Employee's applicable primary Position. The source kind,
Position Assignment, subject Position, reporting relationship, manager Position,
candidates, and fallback are frozen in the stage snapshot.

**Rationale**: Free-form selector fields allow invalid combinations and make
multi-Position Employees ambiguous. Kind-specific shapes make invalid
configuration rejectable before submission and preserve an exact explanation.

**Alternatives considered**:

- Infer a unit head from title or reporting-tree rank: rejected by the approved
  product decision and because the inference is not a governed identity.
- Always use the primary Position for Direct Manager: rejected because a
  multi-Position Employee's artifact may belong to another responsibility.

## Decision 17: Serialize route replacement through one activation slot

**Decision**: Each `(OrganizationId, ArtifactType)` owns one optimistic
`ApprovalRouteActivationSlot` with its active route/version and concurrency
token. Activating an independently approved target locks the slot and relevant
route heads, verifies the expected active identity, retires the previous active
version and, when switching definitions, its route definition, then activates
the target in one transaction. The route-specific retire command rejects the
slot's active route with `approval.route.replacement-required`; activating an
approved replacement is the only operation that can remove that active route.

**Rationale**: A slot makes the invariant and concurrency owner explicit. The
transaction cannot expose an instant with no route, and a concurrent activation
or retirement returns a stable conflict instead of silently selecting a winner.

**Alternatives considered**:

- Retire and activate as separate requests: rejected because a failure between
  requests creates an unroutable gap.
- A database query that searches for an arbitrary active version without a
  concurrency owner: rejected because two route definitions could race.

## Decision 18: Split the Organization KPI Workspace by feature ownership

**Decision**: Feature 002 implements the authorized Organization tree read
model, branch loading/search, Position selection, applicable baseline context,
URL-restorable MVC/Razor shell, capability/data-scope filtering, empty/forbidden/
conflict states, keyboard interaction, and 390-pixel drawer behavior. It also
publishes a versioned integration contract for the future one-edge KPI
neighborhood. The shell labels KPI neighborhood, Effective Segment, Target,
Actual, Variance, score, and Employee KPI responsibility as unavailable until
their named Planning/Cascade/Actual/Evaluation owners provide durable facts; it
does not use production-looking fixtures.

The later modules own the real parent/child graph, the three weight kinds,
Employee KPI assignments, KPI Effective Segments, official Target/Actual/
Variance/score, and whole-period aggregation. The future endpoint is coarse-
grained and backend-authoritative; the frontend never traverses or calculates
those facts.

**Rationale**: The approved workspace can establish its navigation and
authorization seam now without violating feature 002's scope. An explicit
contract prevents later modules from inventing incompatible page DTOs while
keeping their business behavior out of the foundation acceptance claim.

**Alternatives considered**:

- Implement full KPI neighborhood in feature 002: rejected because the required
  Plan, Cascade, Actual, and Evaluation aggregates are explicitly out of scope.
- Use mock KPI rows to finish the UI: rejected because mock evidence cannot pass
  the PostgreSQL or reference-port gate.
- Let Razor derive relations or results: rejected because the backend must own
  hierarchy, authorization, calculation, and aggregation.

## Resolved unknowns

All Technical Context items are resolved. The following cross-feature ownership
is intentional rather than unresolved:

- This feature executes the baseline eligibility decision matrix, contiguous
  applicability transaction, impact fact, weight preview, and segment-contract
  validation; these are behavioral seams rather than document-only promises.
- The later Planning feature applies a governed KPI plan amendment/re-cascade.
- Its approval command registers an immutable `BaselineImpactResolution`
  through the published in-process Application contract; feature 002 owns the
  registrar, validation, persistence, idempotency, audit, and contract tests.
- The later Evaluation feature calculates and combines official segment results.
- Feature 002 implements the authorized Organization/Position navigator and
  publishes the Organization KPI Workspace integration contract; later modules
  populate its KPI neighborhood and result projections from durable facts.
