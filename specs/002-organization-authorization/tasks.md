---

description: "Dependency-ordered implementation tasks for Organization and Authorization Foundation"
---

# Tasks: Organization and Authorization Foundation

**Input**: Design documents from `/specs/002-organization-authorization/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`,
`contracts/`, and `quickstart.md`.

**Target lock**: Work only in `Kpis-Thinh` on
`feature/bsc-kpi-reference-implementation`. Do not edit `BSC-KPIs-API` or
`BSC-KPIs` until the reference UI/UX, backend, API, database, authorization,
restart, and audit gate is explicitly approved.

**Execution rule**: For every story, execute its RED contract/behavior tests
before the corresponding implementation tasks. A story is not complete until
its Domain -> Application -> API -> Razor -> PostgreSQL -> restart path passes.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the reference implementation seams, test profiles, and
documentation structure without changing target repositories.

- [ ] T001 Record the approved branch, .NET SDK `9.0.315`, PostgreSQL profile, and target-repository lock in `specs/002-organization-authorization/plan.md`
- [ ] T002 [P] Create the feature test directory layout under `tests/Kpi.Domain.Tests/Organizations`, `tests/Kpi.Domain.Tests/Authorization`, `tests/Kpi.Domain.Tests/Approvals`, `tests/Kpi.Application.Tests/Organizations`, `tests/Kpi.Application.Tests/Authorization`, `tests/Kpi.Application.Tests/Approvals`, `tests/Kpi.IntegrationTests/Api`, `tests/Kpi.IntegrationTests/Database`, `tests/Kpi.IntegrationTests/Web`, and `tests/Kpi.Web.EndToEndTests`
- [ ] T003 [P] [Setup] Add deterministic Development platform and Employee/account persona fixtures without credentials in `tests/Kpi.IntegrationTests/Fixtures/DevelopmentIdentityFixture.cs`
- [ ] T004 [P] [Setup] Add the `Thinh-KPI-TEST` PostgreSQL launch/profile assertions in `tests/Kpi.IntegrationTests/Composition/TestProfileContractTests.cs`
- [ ] T005 [P] Add the feature capability-code and stable-problem-code inventory test scaffold in `tests/Kpi.Application.Tests/Authorization/CapabilityCatalogContractTests.cs`
- [ ] T006 [Setup] Add the feature evidence ledger and performance-measurement schema to `.scratch/bsc-kpi-reference/evidence.md`
- [ ] T007 [P] Add OpenAPI/ref/operationId validation to `tests/Kpi.IntegrationTests/Api/OpenApiContractTests.cs`
- [ ] T008 [Setup] Run `./harness.cmd bootstrap` and record the clean baseline build/provisioning result in `.scratch/bsc-kpi-reference/evidence.md`

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Provide persistence, transactions, authorization, audit, and
identity seams required by every story. No story implementation may start until
this phase is complete.

**Gate status**: T011 and T016 are individually verified, but Phase 2 remains
open until T009-T010 and T012-T024 are complete with evidence. Do not start a
User Story implementation or treat the reference gate as open while T024 is
unchecked.

- [ ] T009 Write RED unit tests for Organization-scoped identity, half-open UTC effective intervals, revision tokens, and stable status/capability codes in `tests/Kpi.Domain.Tests/Organizations/SharedValueObjectTests.cs`
- [ ] T010 Write RED Application tests proving every governed action calls the same authorization seam and does not reuse a decision across actions in `tests/Kpi.Application.Tests/Authorization/AuthorizationFreshnessTests.cs`
- [X] T011 Write RED database tests for Organization foreign-key isolation, append-only facts, `xmin` concurrency, and migration/runtime connection separation in `tests/Kpi.IntegrationTests/Database/OrganizationAuthorizationSchemaTests.cs` and `tests/Kpi.IntegrationTests/Database/OrganizationAuthorizationPostgresTests.cs`; use `tests/Kpi.IntegrationTests/Composition/PostgresCompositionTests.cs` and `tests/Kpi.IntegrationTests/Web/PostgresRuntimeSelectionTests.cs` for the runtime/migration composition boundary.
- [ ] T012 [P] Define the immutable audit event/value objects and append-only writer port in `src/Kpi.Domain/Auditing/AuditRecord.cs` and `src/Kpi.Application/Persistence/IAuditWriter.cs`
- [ ] T013 [P] Define the Organization aggregate identity, time-zone, unit-of-work, and concurrency ports in `src/Kpi.Domain/Organizations/Organization.cs` and `src/Kpi.Application/Persistence/IOrganizationUnitOfWork.cs`
- [ ] T014 [P] Define `IAuthorizationDecision`, current-fact loading ports, and stable decision codes in `src/Kpi.Application/Authorization/IAuthorizationDecision.cs` and `src/Kpi.Application/Authorization/AuthorizationDecision.cs`
- [ ] T015 [P] Define platform actor and explicit Development identity-adapter ports in `src/Kpi.Application/Organizations/PlatformIdentity.cs` and `src/Kpi.Application/Persistence/IPlatformIdentityReader.cs`
- [X] T016 Configure EF Core mappings for Organization-scoped keys, effective intervals, JSONB immutable evidence, and concurrency tokens in `src/Kpi.Infrastructure.Postgres/Persistence/Configurations/OrganizationAuthorizationConfiguration.cs`
- [ ] T017 Add forward-only migrator scripts for shared audit, Organization heads, effective-range indexes, and append-only database protections in `src/Kpi.Infrastructure.Postgres/Migrations/`
- [ ] T018 Implement the shared Application transaction/pipeline that commits one command and its Audit Record atomically in `src/Kpi.Application/Persistence/OrganizationUnitOfWork.cs`
- [ ] T019 Implement authoritative current-fact authorization evaluation, including account/employment/role/scope/baseline/delegation loading and action-local-only memoization, in `src/Kpi.Application/Authorization/AuthorizationDecisionService.cs`
- [ ] T020 Add MVC/API Problem Details mapping for stable 400/403/404/409/422 codes, correlation IDs, and current concurrency/baseline context in `src/Kpi.Web/Api/V1/ProblemDetailsFactory.cs`
- [ ] T021 Add the versioned `/api/v1` composition registrations for Domain, Application, PostgreSQL, platform identity, audit, and the explicit `Postgres` persistence profile in `src/Kpi.Web/Program.cs`
- [ ] T022 Add the migrator composition registrations using only `ConnectionStrings:KpiMigration` in `src/Kpi.Migrator/Program.cs`
- [ ] T023 Implement the fixed capability catalog loader and complete initial catalog contract in `src/Kpi.Application/Authorization/CapabilityCatalog.cs` and `src/Kpi.Application/Authorization/CapabilityCatalogRegistration.cs`
- [ ] T024 Run the completed foundational transaction, authorization freshness, isolation, Problem Details, and migration contract tests; record the result in `.scratch/bsc-kpi-reference/evidence.md` before proceeding to User Stories.

## Phase 3: User Story 1 - Approve an Organization Structure Baseline (Priority: P1) -- MVP

**Goal**: Provision an Organization with two distinct temporary Bootstrap
Principals, define and independently approve a structure/workforce baseline
without Role Assignments, preserve immutable effective-dated history, support
break-glass recovery, and persist the approved structure baseline. Mid-period
impact, weight allocation, and Effective Segment behavior are delivered in the
separate phase after approval-route governance.

**Independent Test**: Provision with distinct setup/approval subjects; submit
and independently approve a valid multi-level structure; verify the baseline
contains zero Role Assignments, survives restart, rejects cycles/gaps/overlaps,
and preserves bootstrap/recovery evidence. Bootstrap handoff is verified later
as part of User Story 4 after replacement Role Assignments exist.

### Tests for User Story 1 (write first and verify RED)

- [ ] T025 [P] [US1] Add RED domain tests for two distinct Bootstrap Principals, fixed duty grants, non-delegation, SoD, and the first-baseline no-Role-Assignment invariant in `tests/Kpi.Domain.Tests/Organizations/BootstrapAuthorityTests.cs`
- [ ] T026 [P] [US1] Add RED application tests for atomic/idempotent Organization provisioning and bootstrap status in `tests/Kpi.Application.Tests/Organizations/BootstrapProvisioningTests.cs`
- [ ] T027 [P] [US1] Add RED application tests for two-person recovery, duplicate/ineligible approver, expiry, stale unavailable principal, and replacement-only-one-duty rules in `tests/Kpi.Application.Tests/Organizations/BootstrapRecoveryTests.cs`
- [ ] T028 [P] [US1] Add RED domain tests for unit cycles, missing parents, reporting cycles, effective interval conflicts, primary assignment completeness, and deterministic diagnostics in `tests/Kpi.Domain.Tests/Organizations/StructureValidationTests.cs`
- [ ] T029 [P] [US1] Add RED application/API tests for first baseline submission/approval, Bootstrap approval evidence, no Role Assignment in the snapshot, and self-approval rejection in `tests/Kpi.IntegrationTests/Api/StructureBaselineApiTests.cs`
- [ ] T030 [P] [US1] Add RED PostgreSQL/restart tests for Bootstrap Principals, recovery decisions, baseline applicability, immutable snapshots, and audit history in `tests/Kpi.IntegrationTests/Database/BootstrapBaselineRestartTests.cs`

### Implementation for User Story 1

- [ ] T031 [P] [US1] Implement `BootstrapPrincipal`, `BootstrapRecoveryRequest`, and `BootstrapRecoveryDecision` domain entities and invariants in `src/Kpi.Domain/Organizations/BootstrapAuthority.cs`
- [ ] T032 [P] [US1] Implement Organization, structure workspace/revision, unit, Position, Employee, Position Assignment, and reporting relationship domain entities in `src/Kpi.Domain/Organizations/OrganizationStructure.cs`
- [ ] T033 [P] [US1] Implement immutable Structure Baseline and Baseline Applicability Segment state transitions in `src/Kpi.Domain/Organizations/OrganizationStructureBaseline.cs`
- [ ] T034 [P] [US1] Implement deterministic structure validation and cycle/interval diagnostics in `src/Kpi.Domain/Organizations/StructureValidator.cs`
- [ ] T035 [US1] Implement bootstrap provisioning, fixed grant profiles, and idempotency in `src/Kpi.Application/Organizations/ProvisionOrganizationHandler.cs`
- [ ] T036 [US1] Implement bootstrap recovery request/decision/application with two distinct Platform Security Administrators in `src/Kpi.Application/Organizations/BootstrapRecoveryHandler.cs`
- [ ] T037 [US1] Implement first-baseline submission/independent approval and bootstrap evidence capture in `src/Kpi.Application/Organizations/SubmitStructureBaselineHandler.cs` and `src/Kpi.Application/Organizations/DecideStructureBaselineHandler.cs`
- [ ] T038 [US1] Implement the baseline gate for the first approved baseline in `src/Kpi.Application/Organizations/ApprovedBaselineGate.cs`
- [ ] T039 [P] [US1] Add EF mappings and migration tables for bootstrap principals/recovery/decisions, structure revisions, baseline applicability segments, and immutable snapshots in `src/Kpi.Infrastructure.Postgres/Persistence/Configurations/OrganizationStructureConfiguration.cs` and `src/Kpi.Infrastructure.Postgres/Migrations/`
- [ ] T040 [P] [US1] Add platform bootstrap/recovery and organization structure/baseline endpoints and DTOs from `contracts/bootstrap-authority.md` and `contracts/openapi.yaml` in `src/Kpi.Web/Api/Platform/BootstrapController.cs` and `src/Kpi.Web/Api/V1/OrganizationStructureController.cs`
- [ ] T041 [US1] Add Razor provisioning, recovery, structure editor, validation, baseline review, and baseline timeline pages in `src/Kpi.Web/Controllers/OrganizationController.cs`, `src/Kpi.Web/Controllers/PlatformBootstrapController.cs`, `src/Kpi.Web/ViewModels/`, and `src/Kpi.Web/Views/Organization/`
- [ ] T042 [US1] Add the first-baseline and bootstrap/recovery Playwright journey, including keyboard/390-pixel evidence, in `tests/Kpi.Web.EndToEndTests/OrganizationAuthorizationJourneyTests.cs`
- [ ] T043 [US1] Run `./harness.cmd check`, opt-in PostgreSQL migration/restart tests, and the US1 quickstart sections; record MVP baseline/recovery evidence in `.scratch/bsc-kpi-reference/evidence.md`

**Checkpoint**: MVP is independently usable only when provisioning, first
baseline, bootstrap recovery, immutable audit, PostgreSQL restart, and the US1
acceptance scenarios pass.

## Phase 4: User Story 2 - Govern Employees, Positions, and Effective Assignments (Priority: P1)

**Goal**: Maintain employment and sign-in status independently, support multiple
Positions per Employee, and enforce exactly one applicable primary Position.

**Independent Test**: Create one Employee with multiple non-overlapping Position
Assignments, change employment and account status independently, and verify
historical/current authorization facts and conflict diagnostics.

- [ ] T044 [P] [US2] Add RED domain tests for Employee employment intervals, account status, multiple Positions, primary assignment, and effective-range overlap in `tests/Kpi.Domain.Tests/Organizations/WorkforceAssignmentTests.cs`
- [ ] T045 [P] [US2] Add RED application/API tests for Employee/Position/Position Assignment CRUD, stale revision, disabled account, ended employment, and historical lookup in `tests/Kpi.IntegrationTests/Api/WorkforceAssignmentApiTests.cs`
- [ ] T046 [P] [US2] Add RED PostgreSQL/restart tests for independent Employee, account, Position, and assignment persistence in `tests/Kpi.IntegrationTests/Database/WorkforceRestartTests.cs`
- [ ] T047 [P] [US2] Add RED tests for Organization-tree manager/Position context used by later route and workspace queries in `tests/Kpi.Application.Tests/Organizations/PositionContextQueryTests.cs`
- [ ] T048 [US2] Implement workforce aggregate commands, effective-dated Position Assignment validation, and primary selection in `src/Kpi.Application/Organizations/WorkforceCommandHandlers.cs`
- [ ] T049 [P] [US2] Implement workforce persistence stores and effective-range queries in `src/Kpi.Infrastructure.Postgres/Stores/WorkforceStore.cs`
- [ ] T050 [P] [US2] Add workforce EF mappings, constraints, and indexes without mutating approved baseline snapshots in `src/Kpi.Infrastructure.Postgres/Persistence/Configurations/WorkforceConfiguration.cs`
- [ ] T051 [US2] Add workforce API DTOs/endpoints and safe historical/current response projections in `src/Kpi.Web/Api/V1/WorkforceController.cs`
- [ ] T052 [US2] Add server-rendered Employee, Position, Position Assignment, and reporting relationship forms with validation summaries in `src/Kpi.Web/Controllers/WorkforceController.cs`, `src/Kpi.Web/ViewModels/Workforce/`, and `src/Kpi.Web/Views/Workforce/`
- [ ] T053 [US2] Wire Employee/account/employment checks into `AuthorizationDecisionService` so ended employment and disabled accounts deny interactive actions independently in `src/Kpi.Application/Authorization/AuthorizationDecisionService.cs`
- [ ] T054 [US2] Add US2 Playwright coverage for multiple Positions, primary selection, status changes, keyboard navigation, and safe conflict messages in `tests/Kpi.Web.EndToEndTests/WorkforceJourneyTests.cs`
- [ ] T055 [US2] Run US2 focused tests, PostgreSQL restart verification, and the corresponding quickstart scenarios; record evidence in `.scratch/bsc-kpi-reference/evidence.md`

## Phase 5: User Story 3 - Define Custom Roles from Atomic Capabilities (Priority: P2)

**Goal**: Provide Microsoft 365 Admin Center-style business-task capability
administration with warnings, immutable role versions, Organization isolation,
and no implicit self-grant.

**Independent Test**: Create a risky maker/approver role after acknowledging a
warning, create a second version without moving existing assignments, and verify
role creation grants nothing.

- [ ] T056 [P] [US3] Add RED catalog tests for complete fixed capability IDs, risks, allowed scope kinds, conflict warnings, and no user-created capability names in `tests/Kpi.Application.Tests/Authorization/CapabilityCatalogTests.cs`
- [ ] T057 [P] [US3] Add RED domain tests for custom role identity, immutable versions, warnings, Organization uniqueness, and stale head in `tests/Kpi.Domain.Tests/Authorization/CustomRoleVersionTests.cs`
- [ ] T058 [P] [US3] Add RED API tests for capability grouping, role create/version, warning acknowledgement, stale If-Match, and cross-Organization isolation in `tests/Kpi.IntegrationTests/Api/CustomRoleApiTests.cs`
- [ ] T059 [P] [US3] Add RED end-to-end tests proving role creation does not grant the creator authority in `tests/Kpi.Web.EndToEndTests/CustomRoleJourneyTests.cs`
- [ ] T060 [US3] Implement `CustomKpiRole`, `CustomKpiRoleVersion`, warning snapshot, and immutable capability-bundle invariants in `src/Kpi.Domain/Authorization/CustomKpiRole.cs`
- [ ] T061 [US3] Implement capability catalog query, role create/version handlers, warning acknowledgement, and optimistic concurrency in `src/Kpi.Application/Authorization/CustomRoleHandlers.cs`
- [ ] T062 [P] [US3] Implement role and capability persistence with Organization-scoped uniqueness and used-version immutability in `src/Kpi.Infrastructure.Postgres/Stores/CustomRoleStore.cs` and `src/Kpi.Infrastructure.Postgres/Persistence/Configurations/CustomRoleConfiguration.cs`
- [ ] T063 [P] [US3] Add capability catalog and custom-role OpenAPI DTOs/endpoints in `src/Kpi.Web/Api/V1/SecurityController.cs`
- [ ] T064 [US3] Add grouped business-task role editor, risk/conflict warning panel, before/after version diff, and no-implicit-grant messaging in `src/Kpi.Web/Controllers/SecurityController.cs`, `src/Kpi.Web/ViewModels/Security/`, and `src/Kpi.Web/Views/Security/`
- [ ] T065 [US3] Add role-version references and compatibility checks for existing Role Assignments in `src/Kpi.Application/Authorization/RoleAssignmentReferencePolicy.cs`
- [ ] T066 [P] [US3] Add Organization isolation and role warning timeline evidence to the audit projection in `src/Kpi.Application/Auditing/AuthorizationAuditProjector.cs`
- [ ] T067 [US3] Add US3 Playwright acceptance coverage for task grouping, warning acknowledgement, versioning, and keyboard operation in `tests/Kpi.Web.EndToEndTests/CustomRoleJourneyTests.cs`
- [ ] T068 [US3] Run US3 focused/API/PostgreSQL tests and record the independent role-management acceptance evidence in `.scratch/bsc-kpi-reference/evidence.md`

## Phase 6: User Story 4 - Assign Privilege within an Explicit Data Scope (Priority: P2)

**Goal**: Govern effective Role Assignments with explicit KPI Data Scope,
independent approval, system-floor policy, runtime scope enforcement, audit, and
atomic bootstrap handoff.

**Independent Test**: Propose a UnitSubtree assignment, reject self-approval,
approve independently, allow an in-scope action, deny an out-of-scope action,
and verify the next action observes revocation or handoff changes.

- [ ] T069 [P] [US4] Add RED domain tests for `KpiDataScope` discriminators, approved-baseline UnitSubtree binding, scope containment, and assignment lifecycle in `tests/Kpi.Domain.Tests/Authorization/ScopeAndAssignmentTests.cs`
- [ ] T070 [P] [US4] Add RED application tests for security-floor merge, risky scope approval, self-elevation denial, expiration, revocation, account change, employment change, Role Assignment change, policy change, baseline change, delegation change, fresh-facts authorization, and handoff completion in `tests/Kpi.Application.Tests/Authorization/RoleAssignmentAuthorizationTests.cs`
- [ ] T071 [P] [US4] Add RED API tests for Role Assignment create/decision/revoke, stable Problem Details, If-Match, scope mismatch, and cross-Organization hiding in `tests/Kpi.IntegrationTests/Api/RoleAssignmentApiTests.cs`
- [ ] T072 [P] [US4] Add RED PostgreSQL tests for assignment ranges, decision evidence, audit atomicity, bootstrap handoff, and restart in `tests/Kpi.IntegrationTests/Database/RoleAssignmentRestartTests.cs`
- [ ] T073 [P] [US4] Add RED end-to-end tests for in-scope allow, out-of-scope deny, self-approval, expired authority, and post-revocation next-action denial in `tests/Kpi.Web.EndToEndTests/RoleAssignmentJourneyTests.cs`
- [ ] T074 [US4] Implement `KpiDataScope`, `RoleAssignment`, security floor, Organization policy, and assignment lifecycle invariants in `src/Kpi.Domain/Authorization/RoleAssignment.cs` and `src/Kpi.Domain/Authorization/SecurityPolicy.cs`
- [ ] T075 [US4] Implement role assignment request/approval/revoke handlers with capability/scope/SoD checks and audit in `src/Kpi.Application/Authorization/RoleAssignmentHandlers.cs`
- [ ] T076 [US4] Implement bootstrap handoff evaluator that requires two effective approved replacement assignments and atomically expires both principals in `src/Kpi.Application/Organizations/BootstrapHandoffEvaluator.cs`
- [ ] T077 [P] [US4] Implement current-fact scope queries and authorization resource loading without cross-action decision caching in `src/Kpi.Infrastructure.Postgres/Stores/AuthorizationFactStore.cs`
- [ ] T078 [P] [US4] Add assignment, policy, decision, bootstrap-handoff, and audit EF mappings/constraints/indexes in `src/Kpi.Infrastructure.Postgres/Persistence/Configurations/AuthorizationConfiguration.cs`
- [ ] T079 [P] [US4] Add Role Assignment, security-policy, and authorization-decision API endpoints/DTOs in `src/Kpi.Web/Api/V1/SecurityController.cs`
- [ ] T080 [US4] Add scoped assignment form, risk/scope preview, independent approval reason form, decision timeline, and safe denied state in `src/Kpi.Web/Controllers/RoleAssignmentController.cs`, `src/Kpi.Web/ViewModels/Security/`, and `src/Kpi.Web/Views/Security/`
- [ ] T081 [US4] Add initial bootstrap replacement-duty UI and immutable handoff/expiry evidence to `src/Kpi.Web/Controllers/PlatformBootstrapController.cs` and `src/Kpi.Web/Views/Security/BootstrapHandoff.cshtml`
- [ ] T082 [US4] Add US4 contract, PostgreSQL/restart, and Playwright evidence for one-assignment-pending versus two-assignment-handoff states in `tests/Kpi.IntegrationTests/Api/BootstrapHandoffContractTests.cs` and `tests/Kpi.Web.EndToEndTests/RoleAssignmentJourneyTests.cs`
- [ ] T083 [US4] Add release-blocking authorization latency/freshness matrix for account, employment, Role Assignment, policy, baseline, delegation, and revocation changes between governed actions to `tests/Kpi.IntegrationTests/Authorization/AuthorizationPerformanceTests.cs`
- [ ] T084 [US4] Add release-blocking validation/read load fixtures for 1,000 Employees, 200 Organization Units, and 200-node pages in `tests/Kpi.IntegrationTests/Performance/OrganizationAcceptanceLoadTests.cs`
- [ ] T085 [US4] Run US4 focused tests, opt-in PostgreSQL restart tests, performance envelope, and quickstart authorization scenarios; record p95 evidence in `.scratch/bsc-kpi-reference/evidence.md`

## Phase 7: User Story 5 - Resolve Approvers, Delegation, and Audit Visibility (Priority: P3)

**Goal**: Configure typed Approval Routes and Groups, independently review and
atomically activate versions, snapshot resolved approvers, constrain delegation,
and expose scope-filtered explainable timelines.

**Independent Test**: Resolve Direct Manager and fallback from an approved
baseline, submit an artifact, change the live manager, delegate within limited
scope/time, and verify immutable snapshot/timeline evidence and safe denial.

- [ ] T086 [P] [US5] Add RED domain tests for typed selector one-of validation, Direct Manager Position context/fallback, explicit Unit Head Employee, Named Group membership, and candidate evidence in `tests/Kpi.Domain.Tests/Approvals/ApprovalSelectorTests.cs`
- [ ] T087 [P] [US5] Add RED domain tests for route lifecycle, independent review, activation slot, atomic replacement, and active-route retirement rejection in `tests/Kpi.Domain.Tests/Approvals/ApprovalRouteLifecycleTests.cs`
- [ ] T088 [P] [US5] Add RED application tests for Approval Group effective membership, route resolution, delegation intersection/non-expansion, and timeline visibility in `tests/Kpi.Application.Tests/Approvals/ApprovalResolutionTests.cs`
- [ ] T089 [P] [US5] Add RED OpenAPI contract tests for route/group/snapshot schemas, typed selectors, lifecycle endpoints, concurrency, and stable 403/404/409/422 responses in `tests/Kpi.IntegrationTests/Api/ApprovalRouteContractTests.cs`
- [ ] T090 [P] [US5] Add RED end-to-end tests for route maker/editor SoD, atomic replacement activation, Direct Manager context, Named Group snapshot, delegation, and timeline filters in `tests/Kpi.Web.EndToEndTests/ApprovalRouteJourneyTests.cs`
- [ ] T091 [US5] Implement `ApprovalGroup`, effective membership, route definition/version, typed selector, activation slot, route snapshot, stage snapshot, delegation, and decision entities in `src/Kpi.Domain/Approvals/ApprovalRoute.cs` and `src/Kpi.Domain/Approvals/ApprovalDelegation.cs`
- [ ] T092 [US5] Implement Approval Group CRUD/version handlers and effective membership resolution in `src/Kpi.Application/Approvals/ApprovalGroupHandlers.cs`
- [ ] T093 [US5] Implement route submit, validate, independent decision, atomic activation/replacement, and inactive-retire handlers in `src/Kpi.Application/Approvals/ApprovalRouteHandlers.cs`
- [ ] T094 [US5] Implement typed selector resolution with approved-baseline, Position context, explicit Unit Head Employee, and frozen Named Group evidence in `src/Kpi.Application/Approvals/ApprovalSelectorResolver.cs`
- [ ] T095 [US5] Implement delegation request/approval/effective intersection and no-stage-skip enforcement in `src/Kpi.Application/Approvals/ApprovalDelegationHandlers.cs`
- [ ] T096 [US5] Implement scope-filtered timeline queries and immutable selector/decision evidence projections in `src/Kpi.Application/Auditing/TimelineQuery.cs`
- [ ] T097 [P] [US5] Add EF mappings/indexes for groups, memberships, routes, versions/reviews, activation slots, snapshots, delegations, decisions, and timeline evidence in `src/Kpi.Infrastructure.Postgres/Persistence/Configurations/ApprovalConfiguration.cs`
- [ ] T098 [P] [US5] Add Approval Group, route lifecycle, snapshot, delegation, and timeline API endpoints/DTOs from `contracts/openapi.yaml` in `src/Kpi.Web/Api/V1/ApprovalController.cs` and `src/Kpi.Web/Api/V1/AuditController.cs`
- [ ] T099 [US5] Add route/group/delegation editor and review/activation UI with typed selector validation, reason forms, and safe conflict states in `src/Kpi.Web/Controllers/ApprovalController.cs`, `src/Kpi.Web/ViewModels/Approvals/`, and `src/Kpi.Web/Views/Approvals/`
- [ ] T100 [P] [US5] Add scoped timeline page with actor/delegation/selector/reason/scope filters and no protected-placeholder leakage in `src/Kpi.Web/Controllers/AuditController.cs` and `src/Kpi.Web/Views/Audit/Timeline.cshtml`
- [ ] T101 [US5] Add Approval Group and route snapshot PostgreSQL/restart tests in `tests/Kpi.IntegrationTests/Database/ApprovalRouteRestartTests.cs`
- [ ] T102 [US5] Add full US5 Playwright journey with keyboard and 390-pixel responsive evidence in `tests/Kpi.Web.EndToEndTests/ApprovalRouteJourneyTests.cs`
- [ ] T103 [US5] Run US5 focused/API/PostgreSQL/Playwright tests and record route, delegation, and timeline evidence in `.scratch/bsc-kpi-reference/evidence.md`

## Phase 8: Mid-period Baseline Impact, Weight Allocation, and Effective Segment Contract

**Purpose**: Deliver the explicit successor-baseline boundary, immutable
impact/resolution evidence, deterministic weight-allocation preview, and the
consumer contract for Effective Segment. This phase is outside the User Story 1
MVP and does not calculate official Target, Actual, Variance, or Score results.

**Independent Test**: Approve a successor baseline inside an open KPI period,
resolve the two immutable effective segments, reject invalid or conflicting
amendment evidence, register one approved amendment idempotently, and preview
50/20/30 with a fixed 20% assignment as 40/16/24/20 with exact total 100%.

- [ ] T104 [P] Add RED domain/application tests for gapless successor baselines, exact applicability boundaries, immutable impact/resolution, and Effective Segment identity in `tests/Kpi.Application.Tests/Organizations/MidPeriodImpactTests.cs`
- [ ] T105 [P] Add RED API contract tests for deterministic proportional preview, largest-remainder tie breaks, exact 100% proof, idempotent resolution, and cross-Organization/conflict rejection in `tests/Kpi.IntegrationTests/Api/BaselineImpactContractTests.cs`
- [ ] T106 [P] Implement `BaselineChangeImpact`, `WeightAllocationInput`, `WeightAllocationPreview`, and `EffectiveSegmentContract` in `src/Kpi.Domain/Organizations/BaselineImpact.cs`, `src/Kpi.Domain/Organizations/WeightAllocationPreview.cs`, and `src/Kpi.Domain/Organizations/EffectiveSegmentContract.cs`
- [ ] T107 [P] Implement the atomic gapless successor-baseline close-plus-insert, effective-time applicability lookup, tail serialization, and concurrency conflict handling in `src/Kpi.Application/Organizations/BaselineApplicabilityService.cs`
- [ ] T108 [P] Add EF mappings, append-only constraints, indexes, and forward-only migrations for baseline impacts, impact resolutions, effective segments, and assignment-weight snapshots in `src/Kpi.Infrastructure.Postgres/Persistence/Configurations/BaselineImpactConfiguration.cs` and `src/Kpi.Infrastructure.Postgres/Migrations/`
- [ ] T109 Implement deterministic preview, impact registration, exact-retry handling, and Planning amendment evidence-reader ports in `src/Kpi.Application/Organizations/WeightAllocationPreviewService.cs`, `src/Kpi.Application/Organizations/BaselineImpactRegistrar.cs`, and `src/Kpi.Application/Organizations/IPlanningAmendmentEvidenceReader.cs`
- [ ] T110 [P] Add the versioned mid-period impact/preview/resolution API DTOs and endpoints from the OpenAPI contract in `src/Kpi.Web/Api/V1/BaselineImpactController.cs` and `specs/002-organization-authorization/contracts/openapi.yaml`
- [ ] T111 [P] Add the Razor mid-period impact and weight-preview UI with boundary/conflict/idempotency states in `src/Kpi.Web/Controllers/BaselineImpactController.cs`, `src/Kpi.Web/ViewModels/Organization/`, and `src/Kpi.Web/Views/Organization/BaselineImpact.cshtml`
- [ ] T112 Run PostgreSQL migration/restart, API, Razor, and quickstart verification for successor boundaries, immutable evidence, and exact allocation; record results in `.scratch/bsc-kpi-reference/evidence.md`

## Phase 9: User Story 6 - Organization KPI Workspace Foundation and Cross-Feature Contracts

**Purpose**: Implement only the approved-baseline, capability/scope-filtered
Organization tree and Position navigation; publish future KPI-neighborhood and
Effective Segment contracts without fabricating KPI facts.

- [ ] T113 [P] [US6] Write RED contract tests for authorized lazy tree, Unit-expand/Position-select semantics, exact Baseline Applicability Segment, URL restoration, safe out-of-scope direct URL, and no KPI fixture state in `tests/Kpi.IntegrationTests/Api/OrganizationKpiWorkspaceContractTests.cs`
- [ ] T114 [P] [US6] Write RED Application query tests proving tree nodes/actions are server-filtered by current capability plus KPI Data Scope and never traverse editable workspace in `tests/Kpi.Application.Tests/Organizations/OrganizationTreeQueryTests.cs`
- [ ] T115 [P] [US6] Write RED Playwright tests for tree keyboard navigation, Position selection, refresh/back/forward/copy URL, drawer focus, 390-pixel layout, and unavailable KPI neighborhood in `tests/Kpi.Web.EndToEndTests/OrganizationKpiWorkspaceJourneyTests.cs`
- [ ] T116 [US6] Implement `OrganizationTreeReadModel`, approved-baseline context, continuation/search, and safe action projection in `src/Kpi.Application/Organizations/OrganizationTreeQuery.cs`
- [ ] T117 [US6] Implement the authorized organization-tree endpoint and keep future KPI-neighborhood/metric schemas explicitly non-operational without adding Target/Actual/Score endpoints in `src/Kpi.Web/Api/V1/OrganizationKpiWorkspaceController.cs` and `specs/002-organization-authorization/contracts/openapi.yaml`
- [ ] T118 [US6] Add Razor tree shell, Unit/Position semantics, URL-restorable context, empty/forbidden/conflict states, and honest unavailable KPI region in `src/Kpi.Web/Controllers/OrganizationKpiWorkspaceController.cs`, `src/Kpi.Web/ViewModels/Organization/`, and `src/Kpi.Web/Views/Organization/KpiWorkspace.cshtml`
- [ ] T119 [US6] Add the approved-baseline applicability context read model used by the workspace without traversing KPI impact or result facts in `src/Kpi.Application/Organizations/BaselineApplicabilityContextQuery.cs`
- [ ] T120 [US6] Add workspace persistence/restart tests proving baseline/Position context survives Web restart without synthetic KPI facts in `tests/Kpi.IntegrationTests/Database/OrganizationKpiWorkspaceRestartTests.cs`
- [ ] T121 [US6] Run workspace quickstart, API, PostgreSQL, and Playwright tests and record target-port lock evidence in `.scratch/bsc-kpi-reference/evidence.md`

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Complete repository-level verification, documentation, performance,
security, accessibility, and handoff evidence.

- [ ] T122 [P] Update `specs/002-organization-authorization/quickstart.md` with final command/output references and known opt-in PostgreSQL prerequisites
- [ ] T123 [P] Update `specs/002-organization-authorization/contracts/openapi.yaml` examples and run the OpenAPI/ref/operationId contract validator in `tests/Kpi.IntegrationTests/Api/OpenApiContractTests.cs`
- [ ] T124 [P] Add security review tests for cross-Organization leakage, platform/bootstrap boundary, hidden timeline entries, stale concurrency, and no cross-action authorization cache in `tests/Kpi.IntegrationTests/Authorization/SecurityBoundaryTests.cs`
- [ ] T125 [P] Add accessibility regression checks for labels, focus restoration, keyboard-only operation, warning/error text, and non-color status in `tests/Kpi.Web.EndToEndTests/AccessibilityJourneyTests.cs`
- [ ] T126 [P] Add performance evidence collection and threshold assertions for SC-014 and SC-016 in `tests/Kpi.IntegrationTests/Performance/ReleaseBlockingThresholdTests.cs`
- [ ] T127 Run `./harness.cmd check`, opt-in PostgreSQL migration/restart tests, and the complete `Thinh-KPI-TEST` quickstart; record results in `.scratch/bsc-kpi-reference/evidence.md`
- [ ] T128 [Quality] Review all feature docs against `spec.md`, `plan.md`, `data-model.md`, contracts, and `quickstart.md`; fix stale scope/target-lock claims in `specs/002-organization-authorization/`
- [ ] T129 [ApprovalGate] Prepare the UI/UX, backend, API, authorization, database, restart, audit, and performance approval packet without editing `BSC-KPIs-API` or `BSC-KPIs` in `.scratch/bsc-kpi-reference/reference-approval.md`

## Requirement Traceability

The following compact map makes the primary coverage auditable without adding
requirement labels to the strict checklist format. Individual acceptance tests
must still assert the exact FR/SC identifier in their test name or evidence.

| Requirement | RED tests | Implementation/evidence tasks |
|---|---|---|
| FR-001-FR-016 | T009-T011, T025-T030, T044-T055 | T012-T024, T031-T042, T048-T054 |
| FR-017-FR-021 | T056-T059 | T005, T023, T060-T068 |
| FR-022-FR-027 | T069-T073 | T074-T085 |
| FR-028-FR-035 | T086-T090 | T091-T103 |
| FR-036-FR-041 | T009-T011, T028-T030, T044-T055, T070-T073, T086-T103 | T012-T024, T031-T042, T048-T054, T074-T103 |
| FR-042 | T028-T030, T104 | T033, T038, T107, T108, T112 |
| FR-043 | T104, T105 | T106-T110, T111-T112 |
| FR-044 | T105 | T106-T110, T111-T112 |
| FR-045 | T104 | T106-T109, T112 |
| FR-046 | T105 | T106, T109-T110, T112 |
| FR-047 | T105 | T106, T109, T112 |
| FR-048 | T025-T030 | T031, T035-T043 |
| FR-049 | T010, T019, T070, T077, T083, T113-T115, T124-T126 | T019, T077, T116-T119, T124-T126 |
| FR-050 | T027 | T036, T040 |
| SC-001 | T028, T057, T087 | T034, T061, T093, T043, T068, T103 |
| SC-002 | T029-T030 | T041-T043, including the defined human first-attempt cohort/evidence protocol |
| SC-003 | T069-T073 | T074-T085 |
| SC-004 | T070-T073, T087-T090 | T075, T093, T095, T102-T103 |
| SC-005 | T056-T059 | T060-T068 |
| SC-006 | T086-T090 | T091-T103 |
| SC-007 | T030, T046, T072, T101, T120 | T039, T043, T049-T055, T078, T101, T120-T121 |
| SC-008 | T042, T054, T067, T090, T102, T115 | T043, T055, T068, T103, T115, T121, including the defined human cohort/evidence protocol |
| SC-009 | T042, T054, T067, T090, T102, T115 | T041-T043, T052-T055, T064-T068, T099-T103, T115-T121, T125 |
| SC-010 | T029, T038, T071, T080, T113 | T038, T071, T080, T116-T119 |
| SC-011 | T104-T105 | T106-T110, T111-T112 |
| SC-012 | T104-T105 | T106-T110, T111-T112 |
| SC-013 | T105 | T106-T110, T111-T112 |
| SC-014 | T010, T019, T070, T077, T083, T126 | T019, T077, T126-T127 |
| SC-015 | T027, T036, T082 | T027, T036, T076, T082 |
| SC-016 | T034, T019, T077, T084, T116, T126-T127 | T019 (`AuthorizationDecisionService`), T034 (`StructureValidator`), T077 (`AuthorizationFactStore`), T116 (`OrganizationTreeQuery`), T126-T127 (threshold evidence) |
| FR-051-FR-057 | T113-T115 | T116-T121 |
| SC-017 | T113-T115 | T116-T121 |
| SC-018 | T115 | T118-T121 |

### Per-requirement FR ownership expansion

The compact rows above are expanded here so each Functional Requirement has an
explicit RED-test and implementation/evidence owner. Test names and evidence
entries must retain the exact FR identifier rather than relying only on a range.

| Requirement | RED tests | Implementation/evidence tasks |
|---|---|---|
| FR-001 | T009-T011, T025-T030, T044-T055 | T012-T024, T031-T042, T048-T054 |
| FR-002 | T009-T011, T025-T030, T044-T055 | T012-T024, T031-T042, T048-T054 |
| FR-003 | T009-T011, T025-T030, T044-T055 | T012-T024, T031-T042, T048-T054 |
| FR-004 | T009-T011, T025-T030, T044-T055 | T012-T024, T031-T042, T048-T054 |
| FR-005 | T009-T011, T025-T030, T044-T055 | T012-T024, T031-T042, T048-T054 |
| FR-006 | T009-T011, T025-T030, T044-T055 | T012-T024, T031-T042, T048-T054 |
| FR-007 | T009-T011, T025-T030, T044-T055 | T012-T024, T031-T042, T048-T054 |
| FR-008 | T009-T011, T025-T030, T044-T055 | T012-T024, T031-T042, T048-T054 |
| FR-009 | T009-T011, T025-T030, T044-T055 | T012-T024, T031-T042, T048-T054 |
| FR-010 | T009-T011, T025-T030, T044-T055 | T012-T024, T031-T042, T048-T054 |
| FR-011 | T009-T011, T025-T030, T044-T055 | T012-T024, T031-T042, T048-T054 |
| FR-012 | T009-T011, T025-T030, T044-T055 | T012-T024, T031-T042, T048-T054 |
| FR-013 | T009-T011, T025-T030, T044-T055 | T012-T024, T031-T042, T048-T054 |
| FR-014 | T009-T011, T025-T030, T044-T055 | T012-T024, T031-T042, T048-T054 |
| FR-015 | T009-T011, T025-T030, T044-T055 | T012-T024, T031-T042, T048-T054 |
| FR-016 | T009-T011, T025-T030, T044-T055 | T012-T024, T031-T042, T048-T054 |
| FR-017 | T056-T059 | T005, T023, T060-T068 |
| FR-018 | T056-T059 | T005, T023, T060-T068 |
| FR-019 | T056-T059 | T005, T023, T060-T068 |
| FR-020 | T056-T059 | T005, T023, T060-T068 |
| FR-021 | T056-T059 | T005, T023, T060-T068 |
| FR-022 | T069-T073 | T074-T085 |
| FR-023 | T069-T073 | T074-T085 |
| FR-024 | T069-T073 | T074-T085 |
| FR-025 | T069-T073 | T074-T085 |
| FR-026 | T069-T073 | T074-T085 |
| FR-027 | T069-T073 | T074-T085 |
| FR-028 | T086-T090 | T091-T103 |
| FR-029 | T086-T090 | T091-T103 |
| FR-030 | T086-T090 | T091-T103 |
| FR-031 | T086-T090 | T091-T103 |
| FR-032 | T086-T090 | T091-T103 |
| FR-033 | T086-T090 | T091-T103 |
| FR-034 | T086-T090 | T091-T103 |
| FR-035 | T086-T090 | T091-T103 |
| FR-036 | T009-T011, T028-T030, T044-T055, T070-T073, T086-T103 | T012-T024, T031-T042, T048-T054, T074-T103 |
| FR-037 | T009-T011, T028-T030, T044-T055, T070-T073, T086-T103 | T012-T024, T031-T042, T048-T054, T074-T103 |
| FR-038 | T009-T011, T028-T030, T044-T055, T070-T073, T086-T103 | T012-T024, T031-T042, T048-T054, T074-T103 |
| FR-039 | T009-T011, T028-T030, T044-T055, T070-T073, T086-T103 | T012-T024, T031-T042, T048-T054, T074-T103 |
| FR-040 | T009-T011, T028-T030, T044-T055, T070-T073, T086-T103 | T012-T024, T031-T042, T048-T054, T074-T103 |
| FR-041 | T009-T011, T028-T030, T044-T055, T070-T073, T086-T103 | T012-T024, T031-T042, T048-T054, T074-T103 |
| FR-042 | T028-T030, T104 | T033, T038, T107, T108, T112 |
| FR-043 | T104-T105 | T106-T110, T111-T112 |
| FR-044 | T105 | T106-T110, T111-T112 |
| FR-045 | T104 | T106-T109, T112 |
| FR-046 | T105 | T106, T109-T110, T112 |
| FR-047 | T105 | T106, T109, T112 |
| FR-048 | T025-T030 | T031, T035-T043 |
| FR-049 | T010, T019, T070, T077, T083, T113-T115, T124-T126 | T019, T077, T116-T119, T124-T126 |
| FR-050 | T027 | T036, T040 |
| FR-051 | T113-T115 | T116-T121 |
| FR-052 | T113-T115 | T116-T121 |
| FR-053 | T113-T115 | T116-T121 |
| FR-054 | T113-T115 | T116-T121 |
| FR-055 | T113-T115 | T116-T121 |
| FR-056 | T113-T115 | T116-T121 |
| FR-057 | T113-T115 | T116-T121 |

Setup, quality, and approval-gate tasks are intentionally cross-cutting rather
than FR-owned; their labels make that status explicit and prevent them from
being mistaken for uncovered business requirements.

## Dependencies & Execution Order

### Phase dependencies

- Phase 1 (Setup) has no feature dependency.
- Phase 2 (Foundational) depends on Phase 1 and blocks all stories.
- User Story 1 depends on Phase 2 and is the MVP; it establishes the approved
  structure/baseline and bootstrap authority needed by later stories.
- User Story 2 depends on US1's structure model but can be tested independently
  against an approved baseline.
- User Story 3 depends on the shared catalog from Phase 2 and approved
  Organization context from US1.
- User Story 4 depends on US1, US2, and US3 because scopes bind approved
  baselines, Employees, and exact role versions.
- User Story 5 depends on US1, US2, and US4 because selector resolution and
  timeline authorization require approved structure, Position context, and
  governed capabilities.
- Phase 8 Mid-period depends on US1-US5 so baseline, workforce, authorization,
  and approval-route evidence are available before impact registration.
- Phase 9 workspace depends on US1/US2/US4 and the Phase 8 contract for
  approved-baseline context; it does not depend on KPI result facts.
- Phase 10 depends on every desired story, mid-period, and workspace slice.

### User story completion order

`Foundational -> US1 -> US2 -> US3 -> US4 -> US5 -> Mid-period -> Workspace -> Polish`

### Parallel opportunities

- T002-T007 can run in parallel after T001.
- T009-T015 can run in parallel; T016-T023 follow the shared contracts.
- Within US1, T025-T030 are parallel RED tests; T031-T034 are parallel domain
  seams; T039 and T040 can proceed in parallel after handlers are defined.
- Within US2, T044-T047 are parallel RED tests and T049/T050 can proceed in
  parallel after the domain model is stable.
- Within US3, T056-T059 are parallel RED tests; T062, T063, and T066 can proceed
  in parallel after T060/T061 contracts are fixed.
- Within US4, T069-T073 are parallel RED tests; T077/T078 and T079 can proceed
  in parallel after T074-T076 interfaces are fixed.
- Within US5, T086-T090 are parallel RED tests; T097/T098/T100 can proceed in
  parallel after T091-T096 interfaces are fixed.
- Within Mid-period, T104-T105 are parallel RED tests; T106-T108 can proceed in
  parallel after the domain contract is fixed, T109 follows the domain and
  application seams, T110 and T111 follow the published contracts, and T112
  verifies the complete slice.
- T113-T115 are parallel RED workspace tests; T116-T120 follow the query seam.
- T122-T126 are parallel polish tasks after all story checkpoints.

## Parallel Example: MVP (User Story 1)

```text
T025 BootstrapAuthorityTests.cs
T028 StructureValidationTests.cs
T029 StructureBaselineApiTests.cs
T030 BootstrapBaselineRestartTests.cs
```

These RED tests may be written in parallel. Implementation begins only after the
test contracts are reviewed, then the remaining US1 tasks proceed in dependency
order. Mid-period RED tests are intentionally listed in the separate Phase 8.

## Implementation Strategy

### MVP first

1. Complete Setup and Foundational phases.
2. Complete US1, including bootstrap provisioning/recovery, first baseline,
   immutable audit, and PostgreSQL restart. Do not claim successor-baseline
   impact, weight allocation, or Effective Segment behavior in the MVP.
3. Stop and validate the US1 independent test and human UI/UX review gate.
4. Do not port to `BSC-KPIs-API` or `BSC-KPIs` at this checkpoint.

### Incremental delivery

1. Add US2 workforce history and primary Position behavior.
2. Add US3 task-oriented custom roles and versioning.
3. Add US4 scoped assignments, fresh authorization, and bootstrap handoff.
4. Add US5 route/group/delegation/timeline governance.
5. Add the separate Mid-period impact/weight/Effective Segment contract slice
   and verify exact retry, conflict, precision, and restart behavior.
6. Add the authorized Organization KPI Workspace foundation and future KPI
   contracts.
7. Run the complete quickstart and release-blocking performance evidence before
   requesting reference approval.

### Definition of ready for the next phase

The current phase must have RED tests converted to passing tests, API/contract
validation, PostgreSQL restart evidence, Razor/Playwright evidence, and an
updated `.scratch/bsc-kpi-reference/evidence.md` entry before the next phase
starts.
