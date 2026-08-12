# Prompt: Independent Review of the Organization and Authorization Plan

Copy the prompt below into the reviewing agent while its working directory is
the root of `Kpis-Thinh`.

---

You are the independent architecture and specification reviewer for feature
`002-organization-authorization` in repository `Kpis-Thinh`.

Your task is to determine whether the technical plan is complete, internally
consistent, implementable in this repository, and faithful to the approved
product requirements. Review only. Do not implement code, create migrations,
change the plan/spec/contracts, edit either production repository, commit, or
push.

## Read in this order

1. `AGENTS.md`
2. `README.md`
3. `CONTEXT.md`
4. `docs/architecture.md`
5. `docs/quality.md`
6. `.specify/memory/constitution.md`
7. `docs/decisions/0002-kpi-application-stack.md`
8. `docs/decisions/0003-capability-scope-authorization-and-effective-baselines.md`
9. `docs/plans/2026-08-11-bsc-kpi-reference-first-delivery.md`
10. `specs/002-organization-authorization/spec.md`
11. `specs/002-organization-authorization/checklists/requirements.md`
12. `specs/002-organization-authorization/plan.md`
13. `specs/002-organization-authorization/research.md`
14. `specs/002-organization-authorization/data-model.md`
15. Every file under `specs/002-organization-authorization/contracts/`
16. `specs/002-organization-authorization/quickstart.md`
17. Relevant current implementation files under `src/`, `tests/`,
    `.harness/harness.json`, `global.json`, `Directory.Build.props`, and
    `Directory.Packages.props` needed to verify feasibility and stack accuracy.

Treat chat history as non-authoritative. Repository artifacts are the evidence.

## Review method

Build a requirement traceability matrix for every `FR-001` through `FR-050`
and `SC-001` through `SC-016`. For each item, identify the exact plan,
data-model, contract, quickstart, or test seam that addresses it. Mark an item
`Covered`, `Partially covered`, `Contradicted`, or `Missing`.

Then review these dimensions:

### 1. Scope and product fidelity

- The first operational release exposes one Organization but every governed
  fact remains safely Organization-scoped for later multi-company support.
- Organization structure is approved before baseline-dependent BSC/KPI
  planning, assignment, routing, cascade, and operation.
- Employee, sign-in account, Position, business responsibility, Role
  Assignment, and KPI Assignment remain distinct concepts.
- Atomic capabilities and task groups support a Microsoft 365 Admin Center-like
  administration experience; role display names, pages, and menu entries are
  not authorization units.
- Custom roles may contain any capability combination after warnings, while
  runtime separation of duty and the mandatory system security floor remain
  enforceable.
- KPI Data Scope covers Organization, UnitSubtree, Assigned, and Self and is
  evaluated together with effective capability.
- Approval routing, independent privilege approval, delegation, and timeline
  visibility preserve both authority and explanation.
- Two distinct temporary Bootstrap Principals cross the first-baseline boundary
  with product-fixed non-delegable grants and SoD. The baseline freezes
  structure/workforce only; governed Role Assignments follow it; one atomic
  handoff expires bootstrap authority only after both replacement duties exist.
- Recovery replaces only one unavailable principal after two distinct eligible
  Platform Security Administrator approvals; partial/rejected/expired attempts
  change no authority and platform authority remains outside KPI roles.
- Every governed action reloads current committed authorization facts; only
  identical checks inside one action may be memoized.

### 2. Mid-period organization change

- Exactly one approved baseline applies at each instant.
- A replacement baseline preserves prior facts and creates an immutable
  effective boundary and impact.
- Existing weights are proportionally scaled to make room for fixed new
  weights; largest-remainder rounding is deterministic, preserves prior order,
  and totals exactly 100 percent.
- The plan clearly separates what this foundation implements from what later
  KPI Planning and Evaluation features consume.
- Flag any requirement or success criterion that cannot actually be completed
  inside the stated feature boundary. Do not accept a document-only contract as
  full behavioral coverage unless the spec explicitly permits it.

### 3. Architecture and module depth

- Dependency direction remains `Web -> Application -> Domain`, with PostgreSQL
  as an adapter and `Kpi.Migrator` as the only schema writer.
- Domain invariants remain framework- and persistence-independent.
- Authorization has one deep Application interface shared by MVC, JSON,
  background work, and tests; it is not duplicated in controllers or UI.
- Interfaces hide complexity instead of exposing persistence or workflow
  internals to every caller.
- New seams have at least two real adapters or a concrete variation reason.
- No generic workflow, event-sourcing, microservice, SPA, or business-JavaScript
  expansion is introduced without an approved requirement.

### 4. Data and database safety

- Effective ranges use an unambiguous interval convention and Organization
  timezone.
- Submitted revisions, approved baselines, role versions, route snapshots,
  decisions, bootstrap provisioning/recovery/handoff, delegation evidence,
  impact facts, and Audit Records are immutable.
- Database constraints defend race-sensitive invariants such as non-overlapping
  baselines; application validation still supplies friendly diagnostic paths.
- Optimistic concurrency protects every editable head and maps stale writes to
  stable HTTP 409 responses.
- Every cross-entity query and foreign-key path prevents cross-Organization
  data leakage.
- Accepted and rejected governed actions write adequate immutable audit
  evidence transactionally.
- `ConnectionStrings:KpiMigration` and `ConnectionStrings:KpiRuntime` remain
  isolated; Web startup, bootstrap, and check perform no schema writes.

### 5. Interface quality

- `contracts/openapi.yaml` is structurally valid and defines stable transport
  shapes, concurrency tokens, Problem Details, data scopes, reasons, and
  lifecycle actions without leaking Domain or EF types.
- HTTP status mappings distinguish malformed input, safe not-found, forbidden,
  stale conflict, and domain-policy failure.
- The reduced `/me/actions` projection is explicitly non-authoritative.
- Approval and authorization failures disclose enough information to correct an
  in-scope configuration without exposing protected facts.
- Contract names and fields agree with `CONTEXT.md`, the spec, and data model.

### 6. UI/UX and acceptance evidence

- UI remains C#/MVC/Razor-first and Vietnamese-first.
- The organization tree, role editor, scope assignment, approval queue,
  delegation, impact preview, and timeline are keyboard-operable and preserve
  required actions/evidence at a 390-pixel viewport.
- Warnings differ from blockers and status is not conveyed by color alone.
- The quickstart runs Web with launch profile `Thinh-KPI-TEST` and explicitly
  proves frontend -> backend -> PostgreSQL -> restart persistence.
- Automated evidence and mandatory human UI/UX approval are clearly separated.
- `BSC-KPIs-API` and `BSC-KPIs` remain read-only until the reference approval
  gate is explicitly passed.

### 7. Toolchain and verification

- The plan consistently uses .NET 9, SDK `9.0.315`, and the centrally pinned
  package versions present in the repository.
- All setup, migration, lint, and test instructions use `harness.cmd` and the
  declared opt-in PostgreSQL test profile.
- Every independent user story has Domain, Application, API, PostgreSQL/restart,
  and Playwright evidence proportional to its risk.
- No placeholder, unresolved clarification, invented dependency, credential,
  or alternate schema-writing path remains.

## Finding rules

Report only actionable findings supported by repository evidence. For each
finding provide:

- Severity: `P0 Blocker`, `P1 High`, `P2 Medium`, or `P3 Low`.
- Short title.
- Evidence using repository-relative `file:line` references.
- Affected `FR-*`, `SC-*`, constitution principle, or architecture rule.
- Why the issue matters during implementation or production porting.
- The smallest recommended documentation/design correction.

Do not report preferences as defects. Separate definite contradictions from
risks that implementation tasks merely need to address. If no actionable
finding exists for a dimension, say `No finding`.

## Required output

Return one Markdown review with exactly these sections:

1. `Verdict` — one of `APPROVE`, `APPROVE WITH CHANGES`, or `BLOCK` plus a
   two-sentence rationale.
2. `Findings` — ordered by severity, then evidence location.
3. `Requirement Traceability` — all `FR-001..FR-050` and `SC-001..SC-016`.
4. `Constitution and Architecture Check` — pass/fail for every relevant rule.
5. `Cross-feature Boundary Check` — foundation versus later Planning/Evaluation.
6. `Open Questions` — only questions that cannot be resolved from repository
   evidence; otherwise `None`.
7. `Recommended Next Step` — either fix listed documentation gaps, proceed to
   `$speckit-tasks`, or return to `$speckit-clarify` for a true product decision.

Completion criterion: every functional requirement, success criterion, review
dimension, and finding rule above is accounted for, and the verdict follows
from cited evidence rather than summary impressions.

---
