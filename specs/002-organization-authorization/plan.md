# Implementation Plan: Organization and Authorization Foundation

**Branch**: `feature/bsc-kpi-reference-implementation` | **Date**: 2026-08-11 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/002-organization-authorization/spec.md`

## Summary

Build the first governed vertical slice required before BSC/KPI planning: an
effective-dated organization structure, immutable approved structure baselines,
employees and positions, a fixed atomic KPI Capability catalog, versioned custom
roles, effective scoped role assignments, independent privilege approval,
delegation, resource-based authorization, and explainable audit timelines.

The implementation stays inside the existing modular ASP.NET Core application.
Domain modules own organization, approval, authorization, and deterministic
weight-allocation invariants; Application modules expose commands and queries
that always evaluate capability plus KPI Data Scope; PostgreSQL adapters persist
normalized current facts and immutable reviewed snapshots; versioned JSON
controllers and server-rendered Razor pages exercise the complete journey. A
baseline change creates an immutable impact fact and deterministic re-cascade
preview. Later Planning and Evaluation features consume that fact to apply plan
amendments and aggregate effective segments; they do not redefine its rules.

## Technical Context

**Language/Version**: C# on .NET 9 (`net9.0`), SDK `9.0.315`; repository currently sets `LangVersion=preview`, but this feature must not depend on preview-only syntax so it remains portable to the two production repositories.

**Primary Dependencies**: ASP.NET Core MVC/API `9.0.16`, Entity Framework Core `9.0.16`, Npgsql EF Core provider `9.0.4`; no new workflow, authorization, graph, or JavaScript framework.

**Storage**: PostgreSQL 18.x through the explicit runtime/migration connection split. Relational columns own identity, scope, lifecycle, effective ranges, revision, and concurrency. JSONB is limited to immutable reviewed snapshots, selector evidence, warnings, and audit explanations.

**Testing**: xUnit v3 for Domain/Application tests, `Microsoft.AspNetCore.Mvc.Testing` for HTTP and composition tests, real opt-in PostgreSQL migration/round-trip/restart tests, and Playwright `1.55.0` for desktop, keyboard, and 390-pixel UI journeys.

**Target Platform**: Windows development and CI-compatible ASP.NET Core host; Linux-compatible runtime conventions; Vietnamese-first server-rendered browser UI and `/api/v1` JSON.

**Project Type**: Modular server-rendered web application with REST interfaces and a separate explicit database migrator.

**Performance Goals**: For the first-company design envelope, cached authorization decisions complete within 50 ms p95 after resource facts are loaded; structure validation for 10,000 employees and 2,000 units completes within 2 seconds; paged administration reads complete within 500 ms p95 under local acceptance load.

**Constraints**: One operational Organization in the first release while every fact remains Organization-scoped; exact decimal weights; half-open UTC effective intervals interpreted using the Organization timezone; no silent authorization fallback; no schema writes from Web startup/bootstrap/check; no business-rule JavaScript; no edits to `BSC-KPIs-API` or `BSC-KPIs` before the reference approval gate.

**Scale/Scope**: One active Organization initially, logically multi-Organization; design envelope of 2,000 Organization Units, 10,000 Employees, 20,000 effective Position Assignments, 500 custom role versions, and an append-only audit history. Bulk import, production identity integration, Strategy/BSC content, actual KPI re-cascade persistence, and official segment aggregation are outside this feature.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

### Pre-design gate

| Constitution rule | Result | Evidence |
|---|---|---|
| Discoverable repository context | PASS | `AGENTS.md`, `README.md`, `CONTEXT.md`, architecture, quality, ADR 0002, the reference-first delivery plan, spec, and constitution were read before design. |
| One deterministic verification path | PASS | All setup, migration, lint, test, and completion commands remain behind `harness.cmd`; no alternate verification path is introduced. |
| Behavior-first vertical slices | PASS | The design is partitioned by independently testable P1/P2/P3 user journeys and exposes Domain/Application seams before adapters and UI. |
| Explicit boundaries and decisions | PASS | Dependencies stay `Web -> Application -> Domain`; PostgreSQL remains an adapter; the constraining authorization/baseline choice is captured in ADR 0003. |
| Minimal, safe, reviewable change | PASS | The feature extends the existing four production projects and four test projects, adds no framework, and keeps production repositories read-only. |
| PostgreSQL migration/runtime isolation | PASS | Only `Kpi.Migrator` writes schema with `KpiMigration`; Web uses `KpiRuntime` under the explicit `Postgres` profile. |

No pre-design gate violation requires a complexity exception.

### Post-design gate

| Constitution rule | Result | Design evidence |
|---|---|---|
| Discoverability | PASS | Decisions are in `research.md`, entities and transitions in `data-model.md`, interfaces in `contracts/`, and runnable evidence in `quickstart.md`. |
| Deterministic verification | PASS | `quickstart.md` uses only `harness.cmd`, the `Thinh-KPI-TEST` launch profile, and declared opt-in PostgreSQL settings. |
| Behavior-first slices | PASS | Contract and quickstart scenarios map directly to User Stories 1-5 and keep each slice independently testable. |
| Dependency direction | PASS | Domain has no ASP.NET/EF references; authorization enforcement is an Application module; controllers and Razor pages are adapters. |
| Reviewability and safety | PASS | Effective and approved facts are append-only/revisioned, stale writes use optimistic concurrency, audit is transactional, and UI visibility is explicitly non-authoritative. |

The post-design gate passes. Full cross-segment KPI calculation remains owned by
the later Planning/Evaluation feature; this foundation freezes the effective
boundary, impact facts, and deterministic allocation/aggregation contracts that
those modules must consume.

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
|   |-- Approvals/                    # selectors, route snapshot, delegation, decisions
|   `-- Auditing/                     # immutable governed-action evidence
|-- Kpi.Application/
|   |-- Organizations/               # structure/baseline commands and queries
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
3. **P3 routing/delegation/timeline**: selector resolution from the applicable
   baseline, immutable route snapshots, non-expanding delegation, scoped audit
   visibility, and responsive Razor evidence.
4. **Mid-period contract slice**: unique effective baseline, structural impact
   fact, deterministic largest-remainder re-cascade preview, and effective
   segment contract for later Planning/Evaluation integration.

Each slice must prove Domain -> Application -> API -> Razor -> PostgreSQL ->
restart before the next slice starts. Target repository work remains locked
until the reference UI/UX, backend, and database gate is explicitly approved.

## Complexity Tracking

No constitution violation or additional project/framework is required.
