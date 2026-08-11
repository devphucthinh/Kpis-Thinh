# BSC–KPI Reference-First Delivery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prove the complete approved BSC–KPI product journey on an isolated `Kpis-Thinh` reference branch, obtain explicit product-owner approval, then port the verified backend and frontend contracts into `BSC-KPIs-API` and `BSC-KPIs`.

**Architecture:** The reference remains a modular ASP.NET Core application with Domain, Application, PostgreSQL Infrastructure, and MVC/API delivery. Target implementation preserves the backend Area/service/EF conventions and the frontend MVC/BFF/Razor/DynamicTable conventions. The reference branch is a hard experimental boundary; target repositories remain read-only until the Reference Approval Gate passes.

**Tech Stack:** .NET SDK 9.0.315, ASP.NET Core MVC/API 9.0.16, EF Core 9.0.16, Npgsql 9.0.4, PostgreSQL, xUnit, Playwright, Razor, Tabler/Bootstrap, DynamicTable.

## Global Constraints

- Read `AGENTS.md`, `CONTEXT.md`, `docs/architecture.md`, `docs/quality.md`, and `docs/porting/bsc-kpis/kpi-and-period-lifecycle-spec.md` before implementation.
- Create `feature/bsc-kpi-reference-implementation` in `Kpis-Thinh`; never use a branch name containing `codex`.
- Preserve unrelated working-tree changes. Do not stage or commit them.
- During reference phases, inspect but do not edit `BSC-KPIs-API` or `BSC-KPIs`.
- Use TDD for each behavior: focused failing test, observed RED, minimal GREEN, focused pass, full relevant pass, then commit.
- Use `./harness.cmd migrate` as the only schema-writing command and `./harness.cmd check` as the reference verification interface.
- Backend owns every business rule. Razor/UI owns presentation and friendly validation only.
- Use C#/Razor/server-rendered SVG for phase-one UI. Obtain product-owner approval before adding business JavaScript.
- Use exact decimal arithmetic and immutable revision/audit evidence for governed facts.
- A phase completes only after frontend -> API -> PostgreSQL -> restart evidence and product-owner UI/UX approval.
- Target work begins only after the exact phrase `DUYỆT PORT SANG BSC-KPIs-API VÀ BSC-KPIs`.

---

## File map

### Kpis-Thinh reference branch

```text
src/Kpi.Domain/Organizations/           Organization tree, Position, baseline
src/Kpi.Domain/Authorization/           capability/scope/role/delegation invariants
src/Kpi.Domain/Strategy/                Strategic Plan, Annual BSC, Objective, map
src/Kpi.Domain/Kpis/                    Definition, Version, rubric, composite slots
src/Kpi.Domain/Planning/                Plan Items, targets, weights, assignments
src/Kpi.Domain/Periods/                 cadence, calendar, alignment, activation
src/Kpi.Domain/Evaluations/             target/actual pairs, variance, corrections
src/Kpi.Domain/Scoring/                 policies, bands, aggregates, exceptions
src/Kpi.Domain/Pilots/                  issues, exit gates, promotion
src/Kpi.Application/                    commands, queries, capability checks, ports
src/Kpi.Infrastructure.Postgres/        mappings, migrations, transaction adapters
src/Kpi.Web/Api/V1/                     versioned transport contracts/controllers
src/Kpi.Web/Controllers/                MVC/BFF page controllers
src/Kpi.Web/Views/                      Razor UI matching the approved prototype flow
tests/Kpi.Domain.Tests/                 invariant and formula tests
tests/Kpi.Application.Tests/            command, authorization, recompute tests
tests/Kpi.IntegrationTests/             API/PostgreSQL/restart contracts
tests/Kpi.Web.EndToEndTests/             full Playwright journeys
```

### Target backend after approval

```text
Bsc.Kpis.Api/Areas/Kpis/Controllers/
Bsc.Kpis.Api/Areas/Kpis/Models/Requests/
Bsc.Kpis.Api/Areas/Kpis/Models/Responses/
Bsc.Kpis.Api/Areas/Kpis/Services/
Bsc.Kpis.Api/Areas/Kpis/Entities/
Bsc.Kpis.Api/Areas/Kpis/Configurations/
Bsc.Kpis.Api/Areas/Reports/
Bsc.Kpis.Api/Data/ApplicationDbContext.cs
Bsc.Kpis.Api/Migrations/
```

### Target frontend after approval

```text
IDEACrmPlatform/Areas/Strategy/
IDEACrmPlatform/Areas/Bsc/
IDEACrmPlatform/Areas/Kpis/
IDEACrmPlatform/Areas/Performance/
IDEACrmPlatform/Services/AppMenuService.cs
IDEACrmPlatform/ViewModels/AppMenuItemViewModel.cs
```

## Task 1: Establish the isolated reference branch and execution ledger

**Files:**
- Create: `.scratch/bsc-kpi-reference/work-items.md`
- Create: `.scratch/bsc-kpi-reference/evidence.md`
- Modify: `docs/plans/2026-08-11-bsc-kpi-reference-first-delivery.md`

**Interfaces:**
- Consumes: approved `main` and the lifecycle specification.
- Produces: isolated branch, phase checklist, and evidence ledger.

- [ ] Run `git status --short --branch` and record every pre-existing change without modifying it.
- [ ] Run `git switch -c feature/bsc-kpi-reference-implementation` from the approved `main` commit.
- [ ] Run `./harness.cmd status` and `./harness.cmd check`; record the commit and output in the evidence ledger.
- [ ] Create work items grouped by Tasks 2–8 with links to their required tests and product-owner review status.
- [ ] Commit only the intended ledger/plan changes with `docs: start BSC KPI reference implementation`.

Completion criterion: the active branch is exactly `feature/bsc-kpi-reference-implementation`, baseline checks pass, and neither target repository has changed.

## Task 2: Prove Organization, identity, and dynamic authorization

**Files:**
- Create: Domain Organization and Authorization files from the file map.
- Create: Application commands/queries and persistence ports for the same modules.
- Create: PostgreSQL configurations and a forward-only migration.
- Create: API contracts/controllers and MVC organization/security pages.
- Test: focused Domain, Application, Integration, and End-to-End projects.

**Interfaces:**
- Produces: `OrganizationStructureBaseline`, `ActorContext`, fixed `KpiCapability` catalog, `KpiDataScope`, immutable `CustomKpiRole`, effective `RoleAssignment`, `ApprovalDelegation`, and approver resolution.
- Required command behavior: organization-tree validation; baseline approval; role creation; scoped assignment; privileged assignment approval; self-approval rejection.

- [ ] Write Domain tests for generic tree cycles, effective Position Assignments, one primary Position, and baseline completeness.
- [ ] Verify RED against missing Organization/Authorization types.
- [ ] Implement minimal aggregates and exact domain errors.
- [ ] Write Application tests for capability + Organization/UnitSubtree/Assigned/Self scope and runtime separation of duty.
- [ ] Implement command handlers and immutable Custom Role behavior; a changed capability bundle creates a new role.
- [ ] Write PostgreSQL round-trip and concurrency tests for baselines, role assignments, and delegations.
- [ ] Add migration/configurations and transactional adapters; run `./harness.cmd migrate` only against the configured local/test database.
- [ ] Add API contract tests for stable 400/403/404/409 ProblemDetails codes.
- [ ] Build Razor organization tree, role editor warnings, impact display, and approval timeline.
- [ ] Add a Playwright journey: define organization -> positions -> employees -> baseline -> role -> scoped assignment -> rejected self-elevation -> independent approval.
- [ ] Restart Web with PostgreSQL and prove the baseline and assignments remain.
- [ ] Run `./harness.cmd check`, collect screenshots/evidence, obtain Phase 0 UI/UX approval, and commit coherent slices.

Completion criterion: Organization and authorization are durable, runtime-enforced, visible in UI, and approved before Strategy work begins.

## Task 3: Prove Strategic Plan, Annual BSC, perspectives, and Strategy Map

**Files:**
- Create: Strategy Domain/Application/Infrastructure/API/MVC files.
- Test: Strategy lifecycle, graph, persistence, contract, and browser journeys.

**Interfaces:**
- Produces: `StrategicPlan`, `AnnualBscPlan`, `BscPerspectiveCatalog`, `StrategicObjective`, `StrategyMapEdge`, lifecycle commands, and server-rendered map queries.

- [ ] Write lifecycle tests for Draft -> InReview -> Approved -> Active -> Closed and Rejected -> Draft.
- [ ] Write graph tests for cross-perspective edges, cycle paths, immutable approved revisions, and one active annual plan per year.
- [ ] Implement domain aggregates and change-diff contracts.
- [ ] Write and implement capability/data-scope command tests for create, edit, submit, approve, amend, activate, close, and carry-forward.
- [ ] Add PostgreSQL mappings/migration and prove restart persistence for every lifecycle state.
- [ ] Add typed API contracts with concurrency tokens and exact diagnostic paths.
- [ ] Rebuild the prototype's Strategic Objective page and Strategy Map in Razor/server-rendered SVG; edge editing uses forms.
- [ ] Add Playwright journeys for create -> map -> submit -> reject -> revise -> approve -> activate and carry-forward.
- [ ] Run `./harness.cmd check`, collect desktop/mobile/accessibility evidence, obtain Phase 1 UI/UX approval, and commit.

Completion criterion: the full Strategy/BSC journey is durable and product-owner approved without new business JavaScript.

## Task 4: Prove KPI Dictionary, Version, target, and qualitative rubric

**Files:**
- Create/modify: KPI, Planning, Scoring, API, persistence, MVC, and tests.

**Interfaces:**
- Produces: quantitative/qualitative `KpiVersion`, `KpiPlanItem`, `KpiTargetSet`, `TargetAllocationPolicy`, `KpiScoringPolicy`, `EvidencePolicy`, and exact target/actual variable channel definitions.

- [ ] Write tests that preserve immutable KPI code, version predecessor, formula/rubric meaning, and Draft-only edits.
- [ ] Add durable submit/review/publish/retire/reject/return/clone tests using real PostgreSQL and process restart.
- [ ] Implement Target Allocation policies for Equal, Custom, Additive, Average, EndOfPeriod, and constrained Formula cases.
- [ ] Implement qualitative rubric versioning and separate plan-level target/scoring configuration.
- [ ] Add API formula/rubric validation, capabilities discovery, target preview, and plan impact contracts.
- [ ] Rebuild KPI Library, KPI Version, formula/variables, target allocation, and scoring screens from the prototype flow.
- [ ] Add Playwright journeys for quantitative and qualitative KPI authoring, review, publication, target preview, and invalid submission diagnostics.
- [ ] Run `./harness.cmd check`, prove restart durability, obtain the KPI design UI/UX approval, and commit.

Completion criterion: KPI meaning and annual configuration are separated, versioned, durable, and approved.

## Task 5: Prove Position, Employee, Composite KPI, cascade, and three weights

**Files:**
- Create/modify: Planning/Composite Domain, commands, persistence, API, MVC tree, and tests.

**Interfaces:**
- Produces: `PositionKpiTemplate`, `KpiAssignment`, `CompositeInputSlot`, `ChildKpiBinding`, `PeriodAlignmentPolicy`, `KpiAggregationPolicy`, `CascadeContributionWeight`, `ObjectiveKpiWeight`, and `ScorecardKpiWeight`.

- [ ] Write tests for Position Template snapshotting, multiple effective Positions, Accountable/Contributor Assignment, and transfer amendments.
- [ ] Write graph tests for one direct parent per selected cascade layer, descendant validation, approved hierarchy-skip reason, cycle detection, and diagnostic paths.
- [ ] Write tests that require each parent child-binding weight total, each objective KPI weight total, and each scorecard weight total to equal decimal 100 independently.
- [ ] Implement symbolic child slots and exact Plan binding to child Plan Item, KPI Version, Reporting Period, Target channel, Actual channel, and weight.
- [ ] Implement safe `SUM`, `WEIGHTED_SUM`, and `WEIGHTED_AVG` binding context without formula weight literals becoming authoritative.
- [ ] Add Draft diagnostics that block Submit/Approve/Activate for missing bindings, cycles, cadence conflicts, or invalid totals.
- [ ] Persist exact binding/weight snapshots and add impact-preview/amendment APIs.
- [ ] Build Position Template, Assignment, Cascade Tree, weight editors, diff drawer, and highlighted diagnostic paths in Razor.
- [ ] Add Playwright journey: approved organization -> position template -> annual plan -> employee assignment -> multilevel cascade -> invalid cycle -> corrected weights -> approval.
- [ ] Run `./harness.cmd check`, restart, obtain cascade UI/UX approval, and commit.

Completion criterion: the approved Plan deterministically explains every Assignment, child binding, and independent weight.

## Task 6: Prove periods, dual evaluation, variance, correction, and recompute

**Files:**
- Create/modify: Period, Evaluation, Scoring, time-series, recompute, persistence, API, MVC, and tests.

**Interfaces:**
- Produces: Daily/Monthly/Quarterly/Annual periods, Organization Business Calendar, Target/Actual variable pairs, `KpiEvaluationPair`, `KpiVariance`, `KpiTimeSeries`, correction chain, stale dependency state, and Official Aggregate Score.

- [ ] Write calendar and alignment tests for CalendarDay/BusinessDay, holidays, same cadence, daily/monthly/quarterly roll-up, and prohibited silent latest-value use.
- [ ] Write aggregation tests for Sum, WeightedAverage, Last, Min, Max, and constrained Formula with target and actual channels.
- [ ] Write dual-evaluation tests proving the same exact formula version runs with Target inputs and Actual inputs and stores both snapshots.
- [ ] Write missing-channel tests that block official evaluation without implicit zero.
- [ ] Implement Actual Submission Draft/Submitted/Approved/Rejected and evidence policies.
- [ ] Write correction tests that append a revision, preserve old result, mark parent/objective/aggregate state stale, and recompute topologically and idempotently.
- [ ] Implement Performance Band, Attention Flag, completeness exception, and direction-aware variance tolerances.
- [ ] Persist time-series points/revisions and prove restart plus current/full-history queries.
- [ ] Build Actual entry, evidence, review, correction, Target/Actual variable breakdown, Period/YTD chart, status filters, and Change Comparison page in Razor/server SVG.
- [ ] Add Playwright journey from Period planning through Actual approval, dual evaluation, correction, stale propagation, recompute, timeline, and export preview.
- [ ] Run `./harness.cmd check`, restart, obtain operating UI/UX approval, and commit.

Completion criterion: every current KPI, variable, variance, score, and aggregate is reproducible from immutable snapshots after restart.

## Task 7: Prove Pilot, visible exit gate, dashboards, and export

**Files:**
- Create/modify: Pilot, Reports, export, MVC, persistence, API, and tests.

**Interfaces:**
- Produces: `KpiIssue`, `PilotExitGate`, production promotion, filtered dashboard/query/export contracts, and audit-visible exception explanations.

- [ ] Write Pilot Issue lifecycle/severity/ownership/evidence tests.
- [ ] Write Exit Gate tests that keep promotion disabled until every required evidence item is satisfied and Critical/High issues are resolved.
- [ ] Implement production promotion as a new linked Plan/Revision without relabeling Pilot results.
- [ ] Add scoped typed filters for Organization, Unit, Position, Employee, KPI, Period, lifecycle, data status, band, flag, exception, and revision.
- [ ] Add Excel/CSV export tests proving current filters, Data Scope, exception markers, and audit metadata.
- [ ] Build visible Pilot checklist, issue list/detail, dashboard cards, highlights, filters, comparison, timeline, and export preview matching the prototype's visual language.
- [ ] Add Playwright journey: Pilot -> issue -> correction -> exit checklist -> sign-off -> production-plan creation -> official dashboard/export exclusion of Pilot results.
- [ ] Run `./harness.cmd check`, restart, obtain Phase 3 UI/UX approval, and commit.

Completion criterion: the product owner can inspect and approve the entire reference system from the browser with durable backend evidence.

## Task 8: Execute the Reference Approval Gate

**Files:**
- Modify: `.scratch/bsc-kpi-reference/evidence.md`
- Modify: `docs/porting/bsc-kpis/kpi-and-period-lifecycle-spec.md` only for approved clarifications.

**Interfaces:**
- Consumes: Tasks 2–7.
- Produces: explicit port authorization or a recorded rejected gate with actionable findings.

- [ ] Reset the reference test database only through the approved migration/test workflow and apply migrations from empty state.
- [ ] Run `./harness.cmd bootstrap`, `./harness.cmd migrate`, `./harness.cmd status`, `./harness.cmd check`, and `git diff --check`.
- [ ] Run the complete browser journey twice, including a Web/database restart between creation and verification.
- [ ] Record every gate item, command, test count, screenshot, actor, timestamp, and database evidence.
- [ ] Present the prototype-aligned UI and every negative/exception path to the product owner.
- [ ] Keep target repositories unchanged while approval is pending.
- [ ] Proceed only after the product owner provides `DUYỆT PORT SANG BSC-KPIs-API VÀ BSC-KPIs`.

Completion criterion: explicit target-port approval exists in the task history and every reference gate has objective evidence.

## Task 9: Port the approved backend into BSC-KPIs-API

**Precondition:** Task 8 passed. Without the approval phrase, this task is locked.

**Files:** target backend file map above plus a new backend test project and migration artifacts.

**Interfaces:**
- Consumes: approved reference invariants and JSON contracts.
- Produces: production backend API contracts consumed by `BSC-KPIs`.

- [ ] Create a non-`codex` target feature branch after verifying target working-tree state.
- [ ] Add contract and persistence tests before feature entities/services/controllers.
- [ ] Port domain/application behavior into `Areas/Kpis` services/entities/configurations without placing business rules in controllers.
- [ ] Extend `ApplicationDbContext`, Identity role/capability resolution, JWT/current-user context, data scope, migrations, and audit transactions.
- [ ] Preserve target conventions: request/response DTO separation, async cancellation, authenticated base controller, EF configurations, and typed list contracts.
- [ ] Replace startup-only confidence with explicit empty-database migration, PostgreSQL round-trip, restart, concurrency, and authorization evidence.
- [ ] Run `dotnet build Bsc.Kpis.Api/Bsc.Kpis.Api.sln` and every new backend test project.
- [ ] Obtain backend/API/database approval before frontend target work begins.

Completion criterion: target backend behavior matches the approved reference contract and passes real PostgreSQL/restart verification.

## Task 10: Port the approved UI into BSC-KPIs

**Precondition:** Task 9 backend contract is approved and running.

**Files:** target frontend file map above plus frontend contract and browser tests.

**Interfaces:**
- Consumes: verified target backend contracts only.
- Produces: production MVC/BFF UI matching the approved reference experience.

- [ ] Create a non-`codex` target feature branch after verifying target working-tree state.
- [ ] Add Area-specific API service interfaces/implementations and shared typed error/result handling.
- [ ] Add capability/data-scope claims or `/me/capabilities` consumption; menu/action visibility remains non-authoritative.
- [ ] Implement Razor screens and DynamicTable lists in the approved navigation order.
- [ ] Keep formula, lifecycle, cascade, scoring, and export calculations in the backend.
- [ ] Use server-rendered SVG for approved diagrams. Request product-owner approval before new business JavaScript.
- [ ] Add full browser tests against the target backend and PostgreSQL, including rejection, correction, restart, filters, and export.
- [ ] Run `dotnet build IDEACrmPlatform.sln` and all new frontend/browser tests.
- [ ] Conduct final product-owner UI/UX review from Organization setup through Score export.

Completion criterion: the two target repositories reproduce the approved reference end to end without relying on mock or in-memory business state.

## Evidence record

This planning task changes documentation only. Implementation evidence belongs
to `.scratch/bsc-kpi-reference/evidence.md` on the reference branch and must
include exact commands and observed outcomes rather than completion claims.
