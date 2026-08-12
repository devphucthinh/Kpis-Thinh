# Implementation Plan: Organization and Authorization Foundation

**Branch**: `feature/bsc-kpi-reference-implementation` | **Date**: 2026-08-12 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/002-organization-authorization/spec.md`

## Summary

Build the first governed vertical slice required before BSC/KPI planning: an
effective-dated organization structure, immutable approved structure baselines,
employees and positions, a fixed atomic KPI Capability catalog, versioned custom
roles, effective scoped role assignments, independent privilege approval,
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
baseline change creates an immutable impact fact and deterministic re-cascade
preview. This feature behaviorally proves the baseline eligibility gate,
gapless applicability chain, impact, preview, and Effective Segment contract.
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

**Performance Goals**: For the first-company design envelope, cached authorization decisions complete within 50 ms p95 after resource facts are loaded; structure validation for 10,000 employees and 2,000 units completes within 2 seconds; paged administration reads and authorized organization-tree branch queries (up to 200 returned nodes) complete within 500 ms p95 under local acceptance load.

**Constraints**: One operational Organization in the first release while every fact remains Organization-scoped; exact decimal weights; half-open UTC effective intervals interpreted using the Organization timezone; no silent authorization fallback; no schema writes from Web startup/bootstrap/check; no business-rule JavaScript; no edits to `BSC-KPIs-API` or `BSC-KPIs` before the reference approval gate.

**Scale/Scope**: One active Organization initially, logically multi-Organization; design envelope of 2,000 Organization Units, 10,000 Employees, 20,000 effective Position Assignments, 500 custom role versions, 500 Approval Groups, 2,000 effective group memberships, 200 Approval Route definitions/versions, and an append-only audit history. Bulk import, production identity integration, Strategy/BSC content, real KPI-neighborhood facts, actual KPI re-cascade persistence, and official segment aggregation are outside this feature; the navigator, gates, and integration contracts they must consume are inside it and require executable acceptance tests.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

### Pre-design gate

| Constitution rule | Result | Evidence |
|---|---|---|
| Discoverable repository context | PASS | `AGENTS.md`, `README.md`, `CONTEXT.md`, architecture, quality, ADR 0002, the reference-first delivery plan, spec, and constitution were read before design. |
| One deterministic verification path | PASS | All setup, migration, lint, test, and completion commands remain behind `harness.cmd`; no alternate verification path is introduced. |
| Behavior-first vertical slices | PASS | The design is partitioned by independently testable P1/P2/P3 user journeys and exposes Domain/Application seams before adapters and UI. |
| Explicit boundaries and decisions | PASS | Dependencies stay `Web -> Application -> Domain`; PostgreSQL remains an adapter; authorization/baseline governance is captured in ADR 0003 and Phase 0 records independent route review, activation-slot serialization, Approval Group membership, and the workspace boundary. |
| Minimal, safe, reviewable change | PASS | The feature extends the existing four production projects and four test projects, adds no framework, and keeps production repositories read-only. |
| PostgreSQL migration/runtime isolation | PASS | Only `Kpi.Migrator` writes schema with `KpiMigration`; Web uses `KpiRuntime` under the explicit `Postgres` profile. |

No pre-design gate violation requires a complexity exception.

### Post-design gate

| Constitution rule | Result | Design evidence |
|---|---|---|
| Discoverability | PASS | Decisions are in `research.md`, entities and transitions in `data-model.md`, interfaces in `contracts/`, and runnable evidence in `quickstart.md`. |
| Deterministic verification | PASS | `quickstart.md` uses only `harness.cmd`, the `Thinh-KPI-TEST` launch profile, and declared opt-in PostgreSQL settings. |
| Behavior-first slices | PASS | Contract and quickstart scenarios map directly to User Stories 1-5 plus the approved foundation-only Organization KPI Workspace journey and keep each slice independently testable. |
| Dependency direction | PASS | Domain has no ASP.NET/EF references; authorization enforcement is an Application module; controllers and Razor pages are adapters. |
| Reviewability and safety | PASS | Effective and approved facts are append-only/revisioned, route versions require independent review, artifact-type activation is serialized without a routing gap, stale writes use optimistic concurrency, audit is transactional, and UI visibility is explicitly non-authoritative. |

The post-design gate passes. Full cross-segment KPI calculation remains owned by
the later Planning/Evaluation feature. This foundation must nevertheless prove
the executable eligibility gate, contiguous baseline-chain transaction,
immutable impact, deterministic allocation preview, and segment contract that
those modules consume; document-only declarations do not satisfy those tests.

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
|   |-- authorization-decision.md
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
|   |-- Organizations/               # effective structure, revisions, baseline, impact
|   |-- Authorization/               # capability, scope, custom role, assignment, policy
|   |-- Approvals/                    # group membership, route review/activation, snapshots
|   `-- Auditing/                     # immutable governed-action evidence
|-- Kpi.Application/
|   |-- Organizations/               # structure/baseline commands and authorized tree query
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

**Structure Decision**: Extend the existing four-project modular monolith. The
deep authorization module presents one decision interface to every command;
in-memory and PostgreSQL adapters sit behind the same Application persistence
seams. Web exposes presentation/transport only. No new project or horizontal
pass-through layer is introduced.

## Delivery Slices

1. **P1 structure/workforce**: effective organization graph, Employees,
   Positions, validation, immutable revision, independent baseline approval,
   baseline lookup, and durable restart evidence.
2. **P2 capability authorization**: fixed catalog, custom role versioning,
   security floor, scoped Role Assignment approval, runtime decision reasons,
   and denied-action audit.
3. **P3 routing/delegation/timeline**: effective-dated internal Approval Groups;
   versioned route-definition API and UI; separate submit, independent review,
   and activation states; one activation slot per artifact type; typed selector
   resolution from explicit unit-head Employee, artifact Position context, or
   frozen group membership; immutable route snapshots; non-expanding
   delegation; scoped audit visibility; and responsive Razor evidence.
4. **Mid-period contract slice**: unique effective baseline, structural impact
   fact, deterministic largest-remainder re-cascade preview, and executable
   effective-segment contract tests for later Planning/Evaluation integration.
5. **Organization KPI Workspace foundation slice**: approved-baseline and
   scope-filtered lazy Organization tree, Unit expand-only and Position-select
   semantics, restorable Position/baseline/effective-time URL state, baseline-
   applicability context, empty/forbidden/conflict states, MVC/Razor keyboard
   and 390-pixel drawer evidence, plus the versioned future KPI-neighborhood
   contract. No KPI/result fixture counts as acceptance.

## Cross-feature Acceptance Boundary

| Behavior | Feature 002 acceptance | Later feature obligation |
|---|---|---|
| Baseline dependency | Execute the allow/deny decision matrix through one Application gate before/after the first baseline. | Every Planning/Evaluation command consumes the gate. |
| Mid-period structure change | Commit a gapless successor boundary, immutable impact, changed responsibility inputs, and unresolved downstream state. | Planning registers an approved amendment reference and applies KPI responsibility/weight changes. |
| Weight redistribution | Execute and verify the deterministic allocation preview, including rounding and exact 100 percent total. | Planning persists the approved preview in an immutable plan/assignment revision. |
| Effective segments | Produce and validate the exact baseline/plan/weight/policy integration key without an official result. | Evaluation calculates each segment and aggregates the official whole-period result. |
| Workspace organization navigator | Query the approved baseline, filter Unit/Position nodes and allowed actions by capability plus KPI Data Scope, preserve Position/effective context in the URL, and prove responsive Razor behavior. | Later UI slices reuse the navigator rather than querying editable structure or rebuilding authorization. |
| One-edge KPI neighborhood | Publish the page/read contract, exact relationship-layer vocabulary, distinct weight names, context-conflict semantics, and feature-owner map; show an honest unavailable state in the foundation shell. | Planning supplies plan/Employee responsibility; Cascade supplies parent/child edges; Actual supplies approved observations; Evaluation supplies Target/Actual/Variance/score and whole-period results. |

Tasks for this feature must not mark downstream Planning/Evaluation obligations
complete. Conversely, those later tasks must reuse these interfaces rather than
reimplementing baseline, allocation, or segment rules.

Each slice must prove Domain -> Application -> API -> Razor -> PostgreSQL ->
restart before the next slice starts. Target repository work remains locked
until the reference UI/UX, backend, and database gate is explicitly approved.

## Complexity Tracking

No constitution violation or additional project/framework is required.
