# Specification Analysis Next Actions Implementation Plan

> **For agentic workers:** Use the executing-plans workflow to implement this plan task by task.

**Goal:** Align the Organization Authorization spec, plan, tasks, and PostgreSQL/runtime evidence so the approved workspace foundation and T011/T016 boundaries are explicit and auditable.

**Architecture:** Keep the workspace as a foundation-only Razor/Application query slice; later KPI Planning/Cascade/Actual/Evaluation facts remain outside this feature. Keep PostgreSQL schema ownership in `Kpi.Infrastructure.Postgres/Migrations`; `Kpi.Migrator` remains the explicit composition root. Extend tests at the existing composition and opt-in PostgreSQL seams.

**Tech Stack:** C#/.NET 9 SDK 9.0.315, ASP.NET Core MVC/Razor, EF Core/Npgsql, PostgreSQL 18, xUnit v3, PowerShell harness.

## Global Constraints

- Do not edit `BSC-KPIs-API` or `BSC-KPIs`.
- Keep every Organization and authorization fact Organization-scoped.
- Keep migrations forward-only and run schema writes only through `./harness.cmd migrate`.
- Do not add synthetic KPI Target/Actual/Score facts to the workspace foundation.
- Do not record credentials in repository files or evidence.

### Task 1: Make the workspace foundation explicit in the specification

**Files:** `specs/002-organization-authorization/spec.md`

- [x] Add User Story 6 for the authorized Organization KPI Workspace foundation.
- [x] Add FR-051-FR-057 for approved-baseline tree reads, Unit-expand/Position-select semantics, URL restoration, safe scope denial, server-rendered Razor/accessibility, exact applicability context, and the non-operational future KPI-neighborhood contract.
- [x] Add SC-017-SC-018 for workspace authorization/navigation and keyboard/390/no-synthetic-facts behavior.

### Task 2: Align plan and task traceability

**Files:** `specs/002-organization-authorization/tasks.md`, `specs/002-organization-authorization/plan.md`

- [x] Point T017, T039, and T108 to `src/Kpi.Infrastructure.Postgres/Migrations/`.
- [x] Add FR-051-FR-057 and SC-017-SC-018 rows to the traceability table with T113-T121.
- [x] Explicitly map P0 bootstrap work to T025-T043 while retaining task numbering.
- [x] Remove the duplicated P3 routing line in the incremental delivery list.

### Task 3: Strengthen T011 connection-boundary evidence

**Files:** `tests/Kpi.IntegrationTests/Database/OrganizationAuthorizationSchemaTests.cs`, `specs/002-organization-authorization/tasks.md`

- [x] Add an assertion that KpiRuntime composition uses only the runtime connection and does not use the migration connection.
- [x] Add an assertion that migration-only configuration does not register `KpiDbContext`.
- [x] Reference the composition tests from T011's description and evidence note.

### Task 4: Verify and synchronize status

**Files:** `specs/002-organization-authorization/tasks.md`, `.scratch/bsc-kpi-reference/evidence.md`

- [x] Run focused model/composition tests.
- [x] Run the opt-in PostgreSQL migration/authorization suite after the latest EF mapping refactor when credentials are present in the caller environment.
- [x] Mark T016 complete after the mapping and PostgreSQL suite were green; do not mark T024 until all foundational tasks and evidence are complete.
- [x] Record only redacted command/results and never credentials.
