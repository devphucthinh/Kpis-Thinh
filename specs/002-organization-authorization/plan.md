# Implementation Plan: Organization and Authorization Foundation

**Branch**: `feature/bsc-kpi-reference-implementation` | **Date**: 2026-08-12 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/002-organization-authorization/spec.md`

## Summary

Build the first governed vertical slice required before BSC/KPI planning: an
effective-dated organization structure, immutable approved structure baselines,
employees and positions, two distinct temporary Bootstrap Principals, a fixed
atomic KPI Capability catalog, versioned custom roles, effective scoped role
assignments, an auditable bootstrap-authority handoff, independent privilege approval,
delegation, resource-based authorization, and explainable audit timelines.
Approval Route versions pass through independent review before activation;
typed selectors resolve from explicit Employee, Position context, or effective
internal Approval Group facts; and one artifact-type activation slot makes
replacement atomic and gap-free.

The implementation stays inside the existing modular ASP.NET Core application.
Domain modules own organization, approval, authorization, and deterministic
weight-allocation invariants; Application modules expose commands and queries
that always evaluate capability plus KPI Data Scope; PostgreSQL adapters persist
normalized current facts and immutable reviewed snapshots; versioned JSON
controllers and server-rendered Razor pages exercise the complete journey. A
first approved Organization Structure Baseline freezes structure and workforce
only—never Role Assignments. The bootstrap setup and independent-approval
principals create and approve that baseline, after which governed Role
Assignments replace both duties and atomically expire bootstrap authority.
Recovery of one unavailable principal is a time-bounded two-person Platform
Security Administrator action with immutable evidence. Authorization reloads
current committed facts for every governed action; only duplicate checks inside
one action may be memoized. A
baseline change creates an immutable impact fact and deterministic re-cascade
preview. A separate immutable Baseline Impact Resolution is registered only by
the later governed Planning approval command through a published in-process
Application contract; derived status, idempotency, Organization/baseline
validation, audit, and shared-unit-of-work rollback participation are
behaviorally tested here. This feature proves the baseline eligibility gate,
gapless applicability chain,
impact/resolution seam, preview, and Effective Segment contract.
Later Planning and Evaluation features consume those interfaces to apply plan
amendments and calculate official segment results; this feature does not claim
those downstream behaviors complete. The approved Organization KPI Workspace
is split at the same boundary: this feature implements its authorized baseline-
scoped Organization/Position navigator and Razor shell and publishes the future
one-edge KPI-neighborhood contract, while later Planning/Cascade/Actual/
Evaluation features supply real KPI and result facts.

## Technical Context

**Language/Version**: C# on .NET 9 (`net9.0`), SDK `9.0.315`; repository currently sets `LangVersion=preview`, but this feature must not depend on preview-only syntax so it remains portable to the two production repositories.

**Primary Dependencies**: ASP.NET Core MVC/API `9.0.16`, Entity Framework Core `9.0.16`, Npgsql EF Core provider `9.0.4`; no new workflow, authorization, graph, or JavaScript framework.

**Storage**: PostgreSQL 18.x through the explicit runtime/migration connection split. Relational columns own identity, scope, lifecycle, effective ranges, revision, and concurrency. JSONB is limited to immutable reviewed snapshots, selector evidence, warnings, and audit explanations.

**Testing**: xUnit v3 for Domain/Application tests, `Microsoft.AspNetCore.Mvc.Testing` for HTTP and composition tests, real opt-in PostgreSQL migration/round-trip/restart tests, and Playwright `1.55.0` for desktop, keyboard, and 390-pixel UI journeys.

**Target Platform**: Windows development and CI-compatible ASP.NET Core host; Linux-compatible runtime conventions; Vietnamese-first server-rendered browser UI and `/api/v1` JSON.

**Project Type**: Modular server-rendered web application with REST interfaces and a separate explicit database migrator.

**Performance Goals**: For the first-company acceptance envelope, each fresh authorization decision completes within 50 ms p95 after resource facts are loaded; structure validation for 1,000 Employees and 200 Organization Units completes within 2 seconds; paged administration reads and authorized organization-tree branch queries returning at most 200 nodes complete within 500 ms p95 under the declared local acceptance load. All three thresholds are release-blocking.

**Constraints**: One operational Organization in the first release while every fact remains Organization-scoped; exact decimal weights; half-open UTC effective intervals interpreted using the Organization timezone; no silent authorization fallback; no schema writes from Web startup/bootstrap/check; no business-rule JavaScript; no edits to `BSC-KPIs-API` or `BSC-KPIs` before the reference approval gate.

**Scale/Scope**: One active Organization initially, logically multi-Organization; release acceptance covers 200 Organization Units, 1,000 Employees, their effective Position Assignments and scoped Role Assignments, versioned roles/groups/routes, and append-only bootstrap, approval, authorization, and audit history. Bulk import, production identity integration, Strategy/BSC content, real KPI-neighborhood facts, actual KPI re-cascade persistence, and official segment aggregation are outside this feature; the navigator, gates, and integration contracts they must consume are inside it and require executable acceptance tests.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

### Pre-design gate

| Constitution rule | Result | Evidence |
|---|---|---|
| Discoverable repository context | PASS | `AGENTS.md`, `README.md`, `CONTEXT.md`, architecture, quality, ADR 0002, the reference-first delivery plan, spec, and constitution were read before design. |
| One deterministic verification path | PASS | All setup, migration, lint, test, and completion commands remain behind `harness.cmd`; no alternate verification path is introduced. |
| Behavior-first vertical slices | PASS | The design is partitioned into independently testable provisioning, structure/baseline, authorization/handoff, routing, mid-period, and workspace journeys; every slice starts with a failing behavior/contract test before implementation. |
| Explicit boundaries and decisions | PASS | Dependencies stay `Web -> Application -> Domain`; PostgreSQL remains an adapter; ADR 0003 and Phase 0 record bootstrap separation/handoff/recovery, fresh per-action authorization, independent route review, activation-slot serialization, Approval Group membership, immutable impact resolution registration, and the workspace boundary. |
| Minimal, safe, reviewable change | PASS | The feature extends the existing four production projects and four test projects, adds no framework, and keeps production repositories read-only. |
| PostgreSQL migration/runtime isolation | PASS | Only `Kpi.Migrator` writes schema with `KpiMigration`; Web uses `KpiRuntime` under the explicit `Postgres` profile. |

No pre-design gate violation requires a complexity exception.

### Post-design gate

| Constitution rule | Result | Design evidence |
|---|---|---|
| Discoverability | PASS | Decisions are in `research.md`, entities and transitions in `data-model.md`, interfaces in `contracts/`, and runnable evidence in `quickstart.md`. |
| Deterministic verification | PASS | `quickstart.md` uses only `harness.cmd`, the `Thinh-KPI-TEST` launch profile, and declared opt-in PostgreSQL settings; it fixes the SC-002/SC-008 cohorts, first-attempt boundary, assistance/exclusion rules, numerator/denominator, `>= 0.90` calculation, and evidence fields without substituting automation. |
| Behavior-first slices | PASS | Contract and quickstart scenarios map directly to User Stories 1-6 plus provisioning/recovery and the approved foundation-only Organization KPI Workspace journey; each delivery slice requires RED behavior tests before Domain/Application/adapters/UI implementation. FR-043 and FR-051-FR-057 include executable approved/missing/cross-Organization/idempotent/conflict/rollback and workspace authorization cases rather than document-only ownership. |
| Dependency direction | PASS | Domain has no ASP.NET/EF references; authorization enforcement is an Application module; controllers and Razor pages are adapters. |
| Reviewability and safety | PASS | Bootstrap duties are separated and temporary, the first baseline contains no authorization grants, recovery needs two independent platform decisions, every governed action reads current committed authorization facts, effective and approved facts are append-only/revisioned, route versions require independent review, artifact-type activation is serialized without a routing gap, stale writes use optimistic concurrency, audit is transactional, and UI visibility is explicitly non-authoritative. |

The post-design gate passes. Full cross-segment KPI calculation remains owned by
the later Planning/Evaluation feature. This foundation must nevertheless prove
the executable eligibility gate, contiguous baseline-chain transaction,
immutable impact plus approved-amendment resolution registration, deterministic
allocation preview, and segment contract that those modules consume;
document-only declarations do not satisfy those tests.

## Project Structure

### Documentation (this feature)

```text
specs/002-organization-authorization/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- openapi.yaml
|   |-- bootstrap-authority.md
|   |-- authorization-decision.md
|   |-- baseline-impact-resolution.md
|   |-- organization-kpi-workspace.md
|   `-- ui-journeys.md
|-- checklists/
|   `-- requirements.md
`-- tasks.md                         # created later by $speckit-tasks
```

### Source Code (repository root)

```text
src/
|-- Kpi.Domain/
|   |-- Organizations/               # bootstrap, structure, baselines, impact/resolution facts
|   |-- Authorization/               # capability, scope, custom role, assignment, policy
|   |-- Approvals/                    # group membership, route review/activation, snapshots
|   `-- Auditing/                     # immutable governed-action evidence
|-- Kpi.Application/
|   |-- Organizations/               # provision/recover, baseline, impact, and tree use cases
|   |-- Authorization/               # one deep authorization-decision interface
|   |-- Approvals/                    # route resolution and decision commands
|   `-- Persistence/                  # organization/authorization ports and unit of work
|-- Kpi.Infrastructure.Postgres/
|   |-- Persistence/Configurations/   # EF mappings and constraints
|   |-- Stores/                       # adapters for Application persistence interfaces
|   `-- Migrations/                   # forward-only schema and append-only protections
|-- Kpi.Migrator/                     # unchanged schema-writing composition root
`-- Kpi.Web/
    |-- Api/V1/                       # versioned DTOs and thin controllers
    |-- Api/Platform/                 # platform-authorized bootstrap/recovery endpoints
    |-- Controllers/                  # MVC page adapters
    |-- ViewModels/                   # page-specific projections/forms
    `-- Views/
        |-- Organization/
        |   `-- KpiWorkspace.cshtml  # foundation navigator; no synthetic KPI facts
        |-- Security/
        `-- Approvals/

tests/
|-- Kpi.Domain.Tests/
|   |-- Organizations/
|   |-- Authorization/
|   `-- Approvals/
|-- Kpi.Application.Tests/
|   |-- Organizations/
|   |-- Authorization/
|   `-- Approvals/
|-- Kpi.IntegrationTests/
|   |-- Api/
|   |-- Database/
|   `-- Web/
`-- Kpi.Web.EndToEndTests/
    `-- OrganizationAuthorizationJourneyTests.cs
```

**Structure Decision**: Extend the existing four runtime projects
(`Kpi.Web`, `Kpi.Application`, `Kpi.Domain`, and
`Kpi.Infrastructure.Postgres`) plus the separate `Kpi.Migrator` schema-writing
composition root. The deep authorization module presents one decision
interface to every command; in-memory and PostgreSQL adapters sit behind the
same Application persistence seams. Web exposes presentation/transport only.
No new project or horizontal pass-through layer is introduced.

## Delivery Slices

1. **P0 bootstrap provisioning and recovery**: atomically provision one
   Organization plus distinct setup and independent-approval Bootstrap
   Principals with product-fixed, non-delegable grants; prove separation of
   duties, immutable provisioning audit, and time-bounded replacement of only
   one unavailable principal after two distinct Platform Security Administrator
   approvals. The first behavior and transport tests must be RED before any
   Domain, persistence, API, or Razor implementation.
2. **P1 structure/workforce and first baseline**: effective organization graph,
   Employees, Positions and Position Assignments, validation, immutable revision,
   independent baseline approval by the two active Bootstrap Principals, baseline
   lookup, proof that the baseline contains no Role Assignment, and durable
   restart evidence. Bootstrap recovery is included in this MVP; governed Role
   Assignment replacement and the atomic bootstrap handoff are deliberately
   verified in the later P2 slice after role and assignment behavior exists.
   Successor-baseline impact, responsibility-weight allocation, and Effective
   Segment resolution are explicitly excluded from US1 and delivered only in
   the separate Mid-period contract slice after P3.
   Tests for the complete baseline/recovery journey precede its implementation.
   The current checklist combines P0 bootstrap/recovery and P1
   structure/baseline in US1: T025-T030 are the RED gate and T031-T043 are
   the implementation, API, Razor, restart, and evidence checkpoint.
3. **P2 capability authorization and handoff**: fixed catalog, custom role
   versioning, security floor, post-baseline scoped Role Assignment approval,
   runtime decision reasons, denied-action audit, and an atomic immutable
   bootstrap handoff that expires both principals only when effective governed
   assignments replace both duties. Every action reloads committed capability,
   scope, baseline, employment, and delegation facts; only same-action duplicate
   checks may be memoized. Tests precede implementation.
4. **P3 routing/delegation/timeline**: effective-dated internal Approval Groups;
   versioned route-definition API and UI; separate submit, independent review,
   and activation states; one activation slot per artifact type; typed selector
   resolution from explicit unit-head Employee, artifact Position context, or
   frozen group membership; immutable route snapshots; non-expanding
   delegation; scoped audit visibility; and responsive Razor evidence.
5. **Mid-period contract slice**: atomic gapless successor-baseline close-plus-
   insert and effective-time lookup; unique effective baseline; immutable
   structural impact plus one-per-impact resolution fact; EF mappings and
   forward-only PostgreSQL migration for impact, resolution, segment, and weight
   snapshot facts; in-process Planning evidence-reader/registration contract;
   exact-retry idempotency, conflicting-reference and cross-Organization
   rejection, atomic resolution/audit and consumer-transaction rollback
   participation; deterministic largest-remainder re-cascade preview; and
   executable effective-segment contract tests for later Planning/Evaluation
   integration.
6. **Organization KPI Workspace foundation slice**: approved-baseline and
   scope-filtered lazy Organization tree, Unit expand-only and Position-select
   semantics, restorable Position/baseline/effective-time URL state, baseline-
   applicability context, empty/forbidden/conflict states, MVC/Razor keyboard
   and 390-pixel drawer evidence, plus the versioned future KPI-neighborhood
   contract. No KPI/result fixture counts as acceptance.

## Cross-feature Acceptance Boundary

| Behavior | Feature 002 acceptance | Later feature obligation |
|---|---|---|
| Bootstrap authority | Provision two distinct Organization-scoped temporary principals, enforce fixed non-delegable grants and SoD, recover only one unavailable principal after two distinct platform approvals, and persist immutable evidence. | Production hosting supplies the external identity/platform-security adapter without turning platform administrators into KPI roles. |
| First baseline and handoff | Freeze structure/workforce only; create governed roles/assignments only after approval; atomically expire both Bootstrap Principals only when effective assignments replace both duties. | Later features consume ordinary governed Role Assignments and never depend on bootstrap grants. |
| Authorization freshness | Reload current committed capability, scope, employment, baseline, and delegation facts for every governed action; permit memoization only within one action and prove the next action observes a revocation/change. | Every later governed command uses the same decision seam and may not add a cross-action authorization cache. |
| Baseline dependency | Execute the allow/deny decision matrix through one Application gate before/after the first baseline. | Every Planning/Evaluation command consumes the gate. |
| Mid-period structure change | Commit a gapless successor boundary, immutable impact, changed responsibility inputs, and unresolved downstream state. | Planning registers an approved amendment reference and applies KPI responsibility/weight changes. |
| Impact resolution registration | Own the in-process registrar, immutable one-per-impact resolution fact, derived `Detected/Resolved` projection, Organization/baseline validation, idempotency/conflict rules, audit, PostgreSQL/restart proof, and shared-unit-of-work participation contract tests. | Planning implements the approved-amendment evidence reader, calls the registrar from its governed approval command, and proves actual amendment approval + resolution + audit atomicity. |
| Weight redistribution | Execute and verify the deterministic allocation preview, including rounding and exact 100 percent total. | Planning persists the approved preview in an immutable plan/assignment revision. |
| Effective segments | Produce and validate the exact baseline/plan/weight/policy integration key without an official result. | Evaluation calculates each segment and aggregates the official whole-period result. |
| Workspace organization navigator | Query the approved baseline, filter Unit/Position nodes and allowed actions by capability plus KPI Data Scope, preserve Position/effective context in the URL, and prove responsive Razor behavior. | Later UI slices reuse the navigator rather than querying editable structure or rebuilding authorization. |
| One-edge KPI neighborhood | Publish the page/read contract, exact relationship-layer vocabulary, distinct weight names, context-conflict semantics, and feature-owner map; show an honest unavailable state in the foundation shell. | Planning supplies plan/Employee responsibility; Cascade supplies parent/child edges; Actual supplies approved observations; Evaluation supplies Target/Actual/Variance/score and whole-period results. |

Tasks for this feature must not mark downstream Planning/Evaluation obligations
complete. Conversely, those later tasks must reuse these interfaces rather than
reimplementing baseline, allocation, or segment rules.

Each slice starts with failing behavior/contract tests, then proves Domain ->
Application -> API -> Razor -> PostgreSQL -> restart before the next slice
starts. A later Planning test adapter may implement the impact evidence-reader
inside the test assembly; production Feature 002 publishes only the interface
and registrar and must not ship fabricated Planning evidence. Target repository work remains locked
until the reference UI/UX, backend, and database gate is explicitly approved.

## Complexity Tracking

No constitution violation or additional project/framework is required.
