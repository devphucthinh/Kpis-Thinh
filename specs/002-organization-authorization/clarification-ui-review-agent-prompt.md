# Prompt: Independent Review of Approval Clarifications and Organization KPI Workspace

Copy the prompt below into an independent reviewing agent while its working
directory is the root of `Kpis-Thinh`.

---

You are the independent specification, architecture, and UI/UX contract
reviewer for feature `002-organization-authorization` in repository
`Kpis-Thinh`.

The product owner has approved five new Approval Route clarifications and the
Organization KPI Workspace UI/UX design. Determine whether the repository
artifacts are internally consistent and whether the existing implementation
plan is ready to be updated. Review only. Do not edit files, implement code,
create migrations, commit, push, merge, or modify `BSC-KPIs-API` or `BSC-KPIs`.

## Repository and branch

- Repository: `C:\Users\TD-996\1. Azure DevOps\Kpis-Thinh`
- Required branch: `feature/bsc-kpi-reference-implementation`
- Review the current remote branch HEAD, including the commits that add the
  Organization KPI Workspace design and the governed Approval Route
  clarifications.
- Treat repository files as authoritative. Chat history is not evidence.

If the required commits are unavailable on the remote branch, return `BLOCK`
and identify the missing repository evidence instead of reviewing an older
state.

## Read in this order

1. `AGENTS.md`
2. `README.md`
3. `CONTEXT.md`
4. `docs/architecture.md`
5. `docs/quality.md`
6. `.specify/memory/constitution.md`
7. `docs/porting/bsc-kpis/implementation-agent-prompt.md`
8. `docs/plans/2026-08-11-bsc-kpi-reference-first-delivery.md`
9. `docs/decisions/0003-capability-scope-authorization-and-effective-baselines.md`
10. `specs/002-organization-authorization/spec.md`
11. `specs/002-organization-authorization/checklists/requirements.md`
12. `specs/002-organization-authorization/plan.md`
13. `specs/002-organization-authorization/research.md`
14. `specs/002-organization-authorization/data-model.md`
15. Every file under `specs/002-organization-authorization/contracts/`
16. `specs/002-organization-authorization/quickstart.md`
17. `docs/superpowers/specs/2026-08-12-organization-kpi-workspace-design.md`
18. Relevant files under `specs/001-kpi-management/`, `src/`, and `tests/`
    only where needed to verify an asserted dependency or current capability.

## Approved decisions to trace

Trace each decision below through the current spec and every affected planning
artifact. Do not assume an artifact is covered merely because a related noun
appears.

### A. Approval Route governance

1. Every new or changed Approval Route version requires independent approval
   by a different eligible actor before activation. The creator or editor
   cannot approve or activate the same version. Decision reason, capability,
   scope, and timeline evidence are preserved.
2. `Organization Unit Head` explicitly identifies an Employee for the relevant
   unit. Resolution does not infer the head from Position title or reporting
   rank, and verifies active employment, unit context, eligibility, and scope
   against the applicable approved baseline.
3. `Direct Manager` starts from the Position context carried by the governed
   artifact. It falls back to the Employee's applicable primary Position only
   when the artifact has no Position context. The selected Position and
   fallback evidence are snapshotted.
4. `Named Group` references an Organization-scoped internal Approval Group
   with effective-dated Employee memberships. Eligible members are frozen in
   the submission's route snapshot.
5. The only active Approval Route for an artifact type cannot be retired until
   an independently approved replacement is ready. Replacement activation and
   prior-route retirement are atomic and do not alter existing snapshots.

For each decision, verify coverage in:

- requirements and acceptance scenarios;
- lifecycle and state transitions;
- entity fields, uniqueness, effective dating, and concurrency rules;
- Application authorization and separation-of-duty seams;
- OpenAPI operations, request/response schemas, Problem Details, and `409`
  behavior;
- UI journey, timeline evidence, negative tests, PostgreSQL/restart tests, and
  quickstart proof.

Pay particular attention to whether an existing direct `activate` or `retire`
operation bypasses the newly approved governance, whether Approval Group is a
real modeled aggregate, and whether a route can have conflicting definitions
of “one active version” versus “one active route.”

### B. Organization KPI Workspace

The approved UI design requires:

1. Organization Unit nodes only expand/collapse; Position nodes select KPI
   context.
2. Desktop uses tree plus detail; mobile uses a Position drawer and full-screen
   KPI content. URL state preserves Position, period, baseline/segment, result
   mode, and filters.
3. The selected Position is the center of an exactly one-edge KPI neighborhood:
   direct parent KPIs, selected-Position KPIs, and direct child KPIs. No
   recursive ancestor or descendant expansion is part of this table.
4. KPI detail preserves the selected Position. Only **Đi tới vị trí sở hữu**
   changes the tree selection.
5. A Position KPI is one summary row. Effective-dated Employee assignments,
   responsibility weights, Target, Actual, and Variance appear in a disclosure.
6. KPI Plan Weight, child-to-parent contribution weight, and Employee
   responsibility weight remain distinct.
7. Frontend does not calculate hierarchy, Target, Actual, Variance, segment
   aggregation, score, authorization, or weights. Backend page/read contracts
   are authoritative.
8. Baseline and Effective Segment context cannot be mixed; whole-period mode
   shows only official backend aggregation.
9. The experience remains .NET 9 MVC/Razor-first, Vietnamese-first,
   keyboard-operable, light/dark capable, and usable at 390 pixels without a
   new SPA or unapproved business-JavaScript layer.
10. The Organization and Authorization foundation owns only the authorized
    tree, Position selection, baseline context, and scope decisions. KPI
    Planning, Cascade, Actual, and Evaluation features must later own the real
    KPI neighborhood and result facts.

Verify that this UI design neither contradicts feature 002 nor silently moves
later Planning/Evaluation behavior into the foundation. Identify which parts
belong in the revised feature-002 plan now, which require an explicit
cross-feature contract now, and which must be deferred to named later specs.

## Review dimensions

### 1. Specification consistency

- New clarifications have exactly one testable interpretation.
- Existing requirements, scenarios, entities, success criteria, and scope
  boundaries contain no obsolete alternative.
- Terms such as Approval Route Definition, Approval Route Version, Approval
  Group, Organization Unit Head, Position context, route snapshot, KPI owner,
  KPI parent/child, and Effective Segment are used consistently.

### 2. Plan and data-model readiness

- Every approved decision has an owning aggregate, invariant, command/query
  seam, persistence rule, concurrency strategy, audit fact, and test layer.
- Independent route approval cannot be bypassed through an activation endpoint.
- Effective-dated Approval Group membership and immutable membership snapshots
  are feasible and Organization-isolated.
- Atomic replacement prevents a routing gap under concurrent activation or
  retirement attempts.
- Position-context manager resolution is representable on every governed
  artifact that consumes it.

### 3. Contract readiness

- OpenAPI can express propose, independently approve, activate/switch, reject,
  retire, and stale-conflict behavior without ambiguous shortcuts.
- Required selectors have complete, mutually valid discriminator schemas.
- Organization-tree and future KPI-neighborhood read models are coarse-grained,
  scope-filtered, and do not create an N+1 API design.
- Error contracts distinguish forbidden, safe not-found, domain conflict,
  stale concurrency, unresolved approver, and stale baseline/segment context.

### 4. Authorization and audit safety

- Capability plus KPI Data Scope is evaluated at the Application boundary.
- Runtime separation of duty blocks route creator/editor self-approval even
  when a custom role contains both task capabilities.
- UI action visibility remains advisory; direct URLs and mutations are
  reauthorized.
- Timeline and snapshot evidence explain actor, represented authority,
  selector, Position context, group membership, fallback, scope, decision,
  reason, route version, and baseline revision without leaking out-of-scope
  facts.

### 5. UI/UX feasibility and accessibility

- The master-detail design is feasible in the approved MVC/Razor architecture.
- Unit and Position interaction semantics are keyboard-accessible.
- Responsive behavior preserves all required actions and context at 390 pixels.
- Loading, no Position, no KPI, no filter result, missing official result,
  forbidden/out-of-scope, context conflict, and API failure states are defined.
- The design does not depend on color, indentation, or JavaScript alone.

### 6. Cross-feature and delivery gates

- Feature 002 implements only foundation behavior it can prove.
- Later feature ownership is explicit for KPI relationship graph, Plan weights,
  Employee KPI assignment, Target, Actual, Variance, score, and official
  cross-segment aggregation.
- `BSC-KPIs-API` and `BSC-KPIs` remain read-only while `TARGET LOCK` is active.
- The next planning action will not accidentally authorize implementation or
  target-repository porting.

## Finding rules

Report only actionable findings supported by repository evidence. Each finding
must include:

- Severity: `P0 Blocker`, `P1 High`, `P2 Medium`, or `P3 Low`.
- Short title.
- Repository-relative `file:line` evidence from every conflicting artifact.
- Approved decision, `FR-*`, `SC-*`, constitution principle, or architecture
  rule affected.
- Concrete implementation or acceptance risk.
- The smallest documentation or design correction.

Do not treat an expected future implementation task as a documentation defect
when the current plan already owns it explicitly. Do treat a missing state,
field, operation, invariant, test seam, or feature owner as a defect when it
would force an implementer to invent behavior.

## Required output

Return one Markdown review with exactly these sections:

1. `Verdict` — one of `APPROVE`, `APPROVE WITH CHANGES`, or `BLOCK`, followed
   by a two-sentence rationale.
2. `Findings` — actionable findings ordered by severity, then file location;
   write `None` if there are none.
3. `Approval Clarification Traceability` — one row for each of the five
   decisions, with Spec, Plan, Data Model, OpenAPI, UI/Quickstart, Tests, and
   status (`Covered`, `Partial`, `Contradicted`, or `Missing`).
4. `Organization KPI Workspace Traceability` — one row for each of the ten UI
   decisions, with current feature owner, required contract/artifact, and
   status.
5. `Constitution and Architecture Check` — pass/fail for each relevant rule.
6. `Cross-feature Boundary Check` — what feature 002 owns now, what contract it
   publishes now, and what named later feature owns implementation.
7. `Open Questions` — only decisions impossible to derive from repository
   evidence; otherwise `None`.
8. `Recommended Next Step` — choose exactly one:
   - update current artifacts and rerun this review;
   - rerun `$speckit-plan` to synchronize approved decisions;
   - proceed to `$speckit-tasks`.

Completion criterion: all five Approval Route decisions, all ten Organization
KPI Workspace decisions, every review dimension, and every finding rule are
accounted for with repository citations, and the verdict follows from that
evidence rather than a summary impression.

---
