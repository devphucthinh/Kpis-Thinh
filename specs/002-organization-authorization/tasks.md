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
- [ ] T003 [P] Add deterministic Development platform and Employee/account persona fixtures without credentials in `tests/Kpi.IntegrationTests/Fixtures/DevelopmentIdentityFixture.cs`
- [ ] T004 [P] Add the `Thinh-KPI-TEST` PostgreSQL launch/profile assertions in `tests/Kpi.IntegrationTests/Composition/TestProfileContractTests.cs`
- [ ] T005 [P] Add the feature capability-code and stable-problem-code inventory test scaffold in `tests/Kpi.Application.Tests/Authorization/CapabilityCatalogContractTests.cs`
- [ ] T006 Add the feature evidence ledger and performance-measurement schema to `.scratch/bsc-kpi-reference/evidence.md`
- [ ] T007 [P] Add OpenAPI/ref/operationId validation to `tests/Kpi.IntegrationTests/Api/OpenApiContractTests.cs`
- [ ] T008 Run `./harness.cmd bootstrap` and record the clean baseline build/provisioning result in `.scratch/bsc-kpi-reference/evidence.md`

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Provide persistence, transactions, authorization, audit, and
identity seams required by every story. No story implementation may start until
this phase is complete.

- [ ] T009 Write RED unit tests for Organization-scoped identity, half-open UTC effective intervals, revision tokens, and stable status/capability codes in `tests/Kpi.Domain.Tests/Organizations/SharedValueObjectTests.cs`
- [ ] T010 Write RED Application tests proving every governed action calls the same authorization seam and does not reuse a decision across actions in `tests/Kpi.Application.Tests/Authorization/AuthorizationFreshnessTests.cs`
- [ ] T011 Write RED database tests for Organization foreign-key isolation, append-only facts, `xmin` concurrency, and migration/runtime connection separation in `tests/Kpi.IntegrationTests/Database/OrganizationAuthorizationSchemaTests.cs`
- [ ] T012 [P] Define the immutable audit event/value objects and append-only writer port in `src/Kpi.Domain/Auditing/AuditRecord.cs` and `src/Kpi.Application/Persistence/IAuditWriter.cs`
- [ ] T013 [P] Define the Organization aggregate identity, time-zone, unit-of-work, and concurrency ports in `src/Kpi.Domain/Organizations/Organization.cs` and `src/Kpi.Application/Persistence/IOrganizationUnitOfWork.cs`
- [ ] T014 [P] Define `IAuthorizationDecision`, current-fact loading ports, and stable decision codes in `src/Kpi.Application/Authorization/IAuthorizationDecision.cs` and `src/Kpi.Application/Authorization/AuthorizationDecision.cs`
- [ ] T015 [P] Define platform actor and explicit Development identity-adapter ports in `src/Kpi.Application/Organizations/PlatformIdentity.cs` and `src/Kpi.Application/Persistence/IPlatformIdentityReader.cs`
- [ ] T016 Configure EF Core mappings for Organization-scoped keys, effective intervals, JSONB immutable evidence, and concurrency tokens in `src/Kpi.Infrastructure.Postgres/Persistence/Configurations/OrganizationAuthorizationConfiguration.cs`
- [ ] T017 Add forward-only migrator scripts for shared audit, Organization heads, effective-range indexes, and append-only database protections in `src/Kpi.Migrator/Migrations/`
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
break-glass recovery, and expose the baseline-impact/effective-segment seams.

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
- [ ] T031 [P] [US1] Add RED tests for gapless baseline successor transactions and before/after applicability lookup in `tests/Kpi.Application.Tests/Organizations/BaselineApplicabilityTests.cs`
- [ ] T032 [P] [US1] Add RED contract tests for baseline impact, deterministic proportional weight preview, largest-remainder rounding, exact 100-percent proof, one-per-impact resolution, exact retry, cross-Organization rejection, and Effective Segment publication in `tests/Kpi.IntegrationTests/Api/BaselineImpactContractTests.cs`

### Implementation for User Story 1

- [ ] T033 [P] [US1] Implement `BootstrapPrincipal`, `BootstrapRecoveryRequest`, and `BootstrapRecoveryDecision` domain entities and invariants in `src/Kpi.Domain/Organizations/BootstrapAuthority.cs`
- [ ] T034 [P] [US1] Implement Organization, structure workspace/revision, unit, Position, Employee, Position Assignment, and reporting relationship domain entities in `src/Kpi.Domain/Organizations/OrganizationStructure.cs`
- [ ] T035 [P] [US1] Implement immutable Structure Baseline and Baseline Applicability Segment state transitions in `src/Kpi.Domain/Organizations/OrganizationStructureBaseline.cs`
- [ ] T036 [P] [US1] Implement deterministic structure validation and cycle/interval diagnostics in `src/Kpi.Domain/Organizations/StructureValidator.cs`
- [ ] T037 [P] [US1] Implement baseline impact, immutable resolution, `WeightAllocationInput`, deterministic `WeightAllocationPreview`, and `EffectiveSegmentContract` domain facts/value objects in `src/Kpi.Domain/Organizations/BaselineImpact.cs`, `src/Kpi.Domain/Organizations/WeightAllocationPreview.cs`, and `src/Kpi.Domain/Organizations/EffectiveSegmentContract.cs`
- [ ] T038 [US1] Implement bootstrap provisioning, fixed grant profiles, and idempotency in `src/Kpi.Application/Organizations/ProvisionOrganizationHandler.cs`
- [ ] T039 [US1] Implement bootstrap recovery request/decision/application with two distinct Platform Security Administrators in `src/Kpi.Application/Organizations/BootstrapRecoveryHandler.cs`
- [ ] T040 [US1] Implement first-baseline submission/independent approval and bootstrap evidence capture in `src/Kpi.Application/Organizations/SubmitStructureBaselineHandler.cs` and `src/Kpi.Application/Organizations/DecideStructureBaselineHandler.cs`
- [ ] T041 [US1] Implement atomic successor baseline close-plus-insert, applicability lookup, and baseline gate in `src/Kpi.Application/Organizations/BaselineApplicabilityService.cs` and `src/Kpi.Application/Organizations/ApprovedBaselineGate.cs`
- [ ] T042 [US1] Implement deterministic largest-remainder allocation preview and baseline impact creation/Planning evidence-reader registration with exact retry, conflict handling, and shared-unit-of-work registration in `src/Kpi.Application/Organizations/WeightAllocationPreviewService.cs`, `src/Kpi.Application/Organizations/BaselineImpactRegistrar.cs`, and `src/Kpi.Application/Organizations/IPlanningAmendmentEvidenceReader.cs`
- [ ] T043 [US1] Add EF mappings and migration tables for bootstrap principals/recovery/decisions, structure revisions, baseline segments, impacts/resolutions, and immutable snapshots in `src/Kpi.Infrastructure.Postgres/Persistence/Configurations/OrganizationStructureConfiguration.cs` and `src/Kpi.Migrator/Migrations/`
- [ ] T044 [US1] Add platform bootstrap/recovery and organization structure/baseline endpoints and DTOs from `contracts/bootstrap-authority.md` and `contracts/openapi.yaml` in `src/Kpi.Web/Api/Platform/BootstrapController.cs` and `src/Kpi.Web/Api/V1/OrganizationStructureController.cs`
- [ ] T045 [US1] Add Razor provisioning, recovery, structure editor, validation, baseline review, baseline timeline, and impact-preview pages in `src/Kpi.Web/Controllers/OrganizationController.cs`, `src/Kpi.Web/Controllers/PlatformBootstrapController.cs`, `src/Kpi.Web/ViewModels/`, and `src/Kpi.Web/Views/Organization/`
- [ ] T046 [US1] Add the first-baseline and bootstrap/recovery Playwright journey, including keyboard/390-pixel evidence, in `tests/Kpi.Web.EndToEndTests/OrganizationAuthorizationJourneyTests.cs`
- [ ] T047 [US1] Run `./harness.cmd check`, opt-in PostgreSQL migration/restart tests, and the US1 quickstart sections; record MVP baseline/recovery evidence in `.scratch/bsc-kpi-reference/evidence.md`

**Checkpoint**: MVP is independently usable only when provisioning, first
baseline, bootstrap recovery, immutable audit, PostgreSQL restart, and the US1
acceptance scenarios pass.

## Phase 4: User Story 2 - Govern Employees, Positions, and Effective Assignments (Priority: P1)

**Goal**: Maintain employment and sign-in status independently, support multiple
Positions per Employee, and enforce exactly one applicable primary Position.

**Independent Test**: Create one Employee with multiple non-overlapping Position
Assignments, change employment and account status independently, and verify
historical/current authorization facts and conflict diagnostics.

- [ ] T048 [P] [US2] Add RED domain tests for Employee employment intervals, account status, multiple Positions, primary assignment, and effective-range overlap in `tests/Kpi.Domain.Tests/Organizations/WorkforceAssignmentTests.cs`
- [ ] T049 [P] [US2] Add RED application/API tests for Employee/Position/Position Assignment CRUD, stale revision, disabled account, ended employment, and historical lookup in `tests/Kpi.IntegrationTests/Api/WorkforceAssignmentApiTests.cs`
- [ ] T050 [P] [US2] Add RED PostgreSQL/restart tests for independent Employee, account, Position, and assignment persistence in `tests/Kpi.IntegrationTests/Database/WorkforceRestartTests.cs`
- [ ] T051 [P] [US2] Add RED tests for Organization-tree manager/Position context used by later route and workspace queries in `tests/Kpi.Application.Tests/Organizations/PositionContextQueryTests.cs`
- [ ] T052 [US2] Implement workforce aggregate commands, effective-dated Position Assignment validation, and primary selection in `src/Kpi.Application/Organizations/WorkforceCommandHandlers.cs`
- [ ] T053 [US2] Implement workforce persistence stores and effective-range queries in `src/Kpi.Infrastructure.Postgres/Stores/WorkforceStore.cs`
- [ ] T054 [US2] Add workforce EF mappings, constraints, and indexes without mutating approved baseline snapshots in `src/Kpi.Infrastructure.Postgres/Persistence/Configurations/WorkforceConfiguration.cs`
- [ ] T055 [US2] Add workforce API DTOs/endpoints and safe historical/current response projections in `src/Kpi.Web/Api/V1/WorkforceController.cs`
- [ ] T056 [US2] Add server-rendered Employee, Position, Position Assignment, and reporting relationship forms with validation summaries in `src/Kpi.Web/Controllers/WorkforceController.cs`, `src/Kpi.Web/ViewModels/Workforce/`, and `src/Kpi.Web/Views/Workforce/`
- [ ] T057 [US2] Wire Employee/account/employment checks into `AuthorizationDecisionService` so ended employment and disabled accounts deny interactive actions independently in `src/Kpi.Application/Authorization/AuthorizationDecisionService.cs`
- [ ] T058 [US2] Add US2 Playwright coverage for multiple Positions, primary selection, status changes, keyboard navigation, and safe conflict messages in `tests/Kpi.Web.EndToEndTests/WorkforceJourneyTests.cs`
- [ ] T059 [US2] Run US2 focused tests, PostgreSQL restart verification, and the corresponding quickstart scenarios; record evidence in `.scratch/bsc-kpi-reference/evidence.md`

## Phase 5: User Story 3 - Define Custom Roles from Atomic Capabilities (Priority: P2)

**Goal**: Provide Microsoft 365 Admin Center-style business-task capability
administration with warnings, immutable role versions, Organization isolation,
and no implicit self-grant.

**Independent Test**: Create a risky maker/approver role after acknowledging a
warning, create a second version without moving existing assignments, and verify
role creation grants nothing.

- [ ] T060 [P] [US3] Add RED catalog tests for complete fixed capability IDs, risks, allowed scope kinds, conflict warnings, and no user-created capability names in `tests/Kpi.Application.Tests/Authorization/CapabilityCatalogTests.cs`
- [ ] T061 [P] [US3] Add RED domain tests for custom role identity, immutable versions, warnings, Organization uniqueness, and stale head in `tests/Kpi.Domain.Tests/Authorization/CustomRoleVersionTests.cs`
- [ ] T062 [P] [US3] Add RED API tests for capability grouping, role create/version, warning acknowledgement, stale If-Match, and cross-Organization isolation in `tests/Kpi.IntegrationTests/Api/CustomRoleApiTests.cs`
- [ ] T063 [P] [US3] Add RED end-to-end tests proving role creation does not grant the creator authority in `tests/Kpi.Web.EndToEndTests/CustomRoleJourneyTests.cs`
- [ ] T064 [US3] Implement `CustomKpiRole`, `CustomKpiRoleVersion`, warning snapshot, and immutable capability-bundle invariants in `src/Kpi.Domain/Authorization/CustomKpiRole.cs`
- [ ] T065 [US3] Implement capability catalog query, role create/version handlers, warning acknowledgement, and optimistic concurrency in `src/Kpi.Application/Authorization/CustomRoleHandlers.cs`
- [ ] T066 [US3] Implement role and capability persistence with Organization-scoped uniqueness and used-version immutability in `src/Kpi.Infrastructure.Postgres/Stores/CustomRoleStore.cs` and `src/Kpi.Infrastructure.Postgres/Persistence/Configurations/CustomRoleConfiguration.cs`
- [ ] T067 [US3] Add capability catalog and custom-role OpenAPI DTOs/endpoints in `src/Kpi.Web/Api/V1/SecurityController.cs`
- [ ] T068 [US3] Add grouped business-task role editor, risk/conflict warning panel, before/after version diff, and no-implicit-grant messaging in `src/Kpi.Web/Controllers/SecurityController.cs`, `src/Kpi.Web/ViewModels/Security/`, and `src/Kpi.Web/Views/Security/`
- [ ] T069 [US3] Add role-version references and compatibility checks for existing Role Assignments in `src/Kpi.Application/Authorization/RoleAssignmentReferencePolicy.cs`
- [ ] T070 [US3] Add Organization isolation and role warning timeline evidence to the audit projection in `src/Kpi.Application/Auditing/AuthorizationAuditProjector.cs`
- [ ] T071 [US3] Add US3 Playwright acceptance coverage for task grouping, warning acknowledgement, versioning, and keyboard operation in `tests/Kpi.Web.EndToEndTests/CustomRoleJourneyTests.cs`
- [ ] T072 [US3] Run US3 focused/API/PostgreSQL tests and record the independent role-management acceptance evidence in `.scratch/bsc-kpi-reference/evidence.md`

## Phase 6: User Story 4 - Assign Privilege within an Explicit Data Scope (Priority: P2)

**Goal**: Govern effective Role Assignments with explicit KPI Data Scope,
independent approval, system-floor policy, runtime scope enforcement, audit, and
atomic bootstrap handoff.

**Independent Test**: Propose a UnitSubtree assignment, reject self-approval,
approve independently, allow an in-scope action, deny an out-of-scope action,
and verify the next action observes revocation or handoff changes.

- [ ] T073 [P] [US4] Add RED domain tests for `KpiDataScope` discriminators, approved-baseline UnitSubtree binding, scope containment, assignment lifecycle, and proportional weight-preview value objects in `tests/Kpi.Domain.Tests/Authorization/ScopeAndAssignmentTests.cs`
- [ ] T074 [P] [US4] Add RED application tests for security-floor merge, risky scope approval, self-elevation denial, expiration, revocation, account change, employment change, Role Assignment change, policy change, baseline change, delegation change, fresh-facts authorization, and handoff completion in `tests/Kpi.Application.Tests/Authorization/RoleAssignmentAuthorizationTests.cs`
- [ ] T075 [P] [US4] Add RED API tests for Role Assignment create/decision/revoke, stable Problem Details, If-Match, scope mismatch, and cross-Organization hiding in `tests/Kpi.IntegrationTests/Api/RoleAssignmentApiTests.cs`
- [ ] T076 [P] [US4] Add RED PostgreSQL tests for assignment ranges, decision evidence, audit atomicity, bootstrap handoff, and restart in `tests/Kpi.IntegrationTests/Database/RoleAssignmentRestartTests.cs`
- [ ] T077 [P] [US4] Add RED end-to-end tests for in-scope allow, out-of-scope deny, self-approval, expired authority, and post-revocation next-action denial in `tests/Kpi.Web.EndToEndTests/RoleAssignmentJourneyTests.cs`
- [ ] T078 [US4] Implement `KpiDataScope`, `RoleAssignment`, security floor, Organization policy, and assignment lifecycle invariants in `src/Kpi.Domain/Authorization/RoleAssignment.cs` and `src/Kpi.Domain/Authorization/SecurityPolicy.cs`
- [ ] T079 [US4] Implement role assignment request/approval/revoke handlers with capability/scope/SoD checks and audit in `src/Kpi.Application/Authorization/RoleAssignmentHandlers.cs`
- [ ] T080 [US4] Implement bootstrap handoff evaluator that requires two effective approved replacement assignments and atomically expires both principals in `src/Kpi.Application/Organizations/BootstrapHandoffEvaluator.cs`
- [ ] T081 [US4] Implement current-fact scope queries and authorization resource loading without cross-action decision caching in `src/Kpi.Infrastructure.Postgres/Stores/AuthorizationFactStore.cs`
- [ ] T082 [US4] Add assignment, policy, decision, bootstrap-handoff, and audit EF mappings/constraints/indexes in `src/Kpi.Infrastructure.Postgres/Persistence/Configurations/AuthorizationConfiguration.cs`
- [ ] T083 [US4] Add Role Assignment, security-policy, and authorization-decision API endpoints/DTOs in `src/Kpi.Web/Api/V1/SecurityController.cs`
- [ ] T084 [US4] Add scoped assignment form, risk/scope preview, independent approval reason form, decision timeline, and safe denied state in `src/Kpi.Web/Controllers/RoleAssignmentController.cs`, `src/Kpi.Web/ViewModels/Security/`, and `src/Kpi.Web/Views/Security/`
- [ ] T085 [US4] Add initial bootstrap replacement-duty UI and immutable handoff/expiry evidence to `src/Kpi.Web/Controllers/PlatformBootstrapController.cs` and `src/Kpi.Web/Views/Security/BootstrapHandoff.cshtml`
- [ ] T086 [US4] Add US4 contract, PostgreSQL/restart, and Playwright evidence for one-assignment-pending versus two-assignment-handoff states in `tests/Kpi.IntegrationTests/Api/BootstrapHandoffContractTests.cs` and `tests/Kpi.Web.EndToEndTests/RoleAssignmentJourneyTests.cs`
- [ ] T087 [US4] Add release-blocking authorization latency/freshness matrix for account, employment, Role Assignment, policy, baseline, delegation, and revocation changes between governed actions to `tests/Kpi.IntegrationTests/Authorization/AuthorizationPerformanceTests.cs`
- [ ] T088 [US4] Add release-blocking validation/read load fixtures for 1,000 Employees, 200 Organization Units, and 200-node pages in `tests/Kpi.IntegrationTests/Performance/OrganizationAcceptanceLoadTests.cs`
- [ ] T089 [US4] Run US4 focused tests, opt-in PostgreSQL restart tests, performance envelope, and quickstart authorization scenarios; record p95 evidence in `.scratch/bsc-kpi-reference/evidence.md`

## Phase 7: User Story 5 - Resolve Approvers, Delegation, and Audit Visibility (Priority: P3)

**Goal**: Configure typed Approval Routes and Groups, independently review and
atomically activate versions, snapshot resolved approvers, constrain delegation,
and expose scope-filtered explainable timelines.

**Independent Test**: Resolve Direct Manager and fallback from an approved
baseline, submit an artifact, change the live manager, delegate within limited
scope/time, and verify immutable snapshot/timeline evidence and safe denial.

- [ ] T090 [P] [US5] Add RED domain tests for typed selector one-of validation, Direct Manager Position context/fallback, explicit Unit Head Employee, Named Group membership, and candidate evidence in `tests/Kpi.Domain.Tests/Approvals/ApprovalSelectorTests.cs`
- [ ] T091 [P] [US5] Add RED domain tests for route lifecycle, independent review, activation slot, atomic replacement, and active-route retirement rejection in `tests/Kpi.Domain.Tests/Approvals/ApprovalRouteLifecycleTests.cs`
- [ ] T092 [P] [US5] Add RED application tests for Approval Group effective membership, route resolution, delegation intersection/non-expansion, and timeline visibility in `tests/Kpi.Application.Tests/Approvals/ApprovalResolutionTests.cs`
- [ ] T093 [P] [US5] Add RED OpenAPI contract tests for route/group/snapshot schemas, typed selectors, lifecycle endpoints, concurrency, and stable 403/404/409/422 responses in `tests/Kpi.IntegrationTests/Api/ApprovalRouteContractTests.cs`
- [ ] T094 [P] [US5] Add RED end-to-end tests for route maker/editor SoD, atomic replacement activation, Direct Manager context, Named Group snapshot, delegation, and timeline filters in `tests/Kpi.Web.EndToEndTests/ApprovalRouteJourneyTests.cs`
- [ ] T095 [US5] Implement `ApprovalGroup`, effective membership, route definition/version, typed selector, activation slot, route snapshot, stage snapshot, delegation, and decision entities in `src/Kpi.Domain/Approvals/ApprovalRoute.cs` and `src/Kpi.Domain/Approvals/ApprovalDelegation.cs`
- [ ] T096 [US5] Implement Approval Group CRUD/version handlers and effective membership resolution in `src/Kpi.Application/Approvals/ApprovalGroupHandlers.cs`
- [ ] T097 [US5] Implement route submit, validate, independent decision, atomic activation/replacement, and inactive-retire handlers in `src/Kpi.Application/Approvals/ApprovalRouteHandlers.cs`
- [ ] T098 [US5] Implement typed selector resolution with approved-baseline, Position context, explicit Unit Head Employee, and frozen Named Group evidence in `src/Kpi.Application/Approvals/ApprovalSelectorResolver.cs`
- [ ] T099 [US5] Implement delegation request/approval/effective intersection and no-stage-skip enforcement in `src/Kpi.Application/Approvals/ApprovalDelegationHandlers.cs`
- [ ] T100 [US5] Implement scope-filtered timeline queries and immutable selector/decision evidence projections in `src/Kpi.Application/Auditing/TimelineQuery.cs`
- [ ] T101 [US5] Add EF mappings/indexes for groups, memberships, routes, versions/reviews, activation slots, snapshots, delegations, decisions, and timeline evidence in `src/Kpi.Infrastructure.Postgres/Persistence/Configurations/ApprovalConfiguration.cs`
- [ ] T102 [US5] Add Approval Group, route lifecycle, snapshot, delegation, and timeline API endpoints/DTOs from `contracts/openapi.yaml` in `src/Kpi.Web/Api/V1/ApprovalController.cs` and `src/Kpi.Web/Api/V1/AuditController.cs`
- [ ] T103 [US5] Add route/group/delegation editor and review/activation UI with typed selector validation, reason forms, and safe conflict states in `src/Kpi.Web/Controllers/ApprovalController.cs`, `src/Kpi.Web/ViewModels/Approvals/`, and `src/Kpi.Web/Views/Approvals/`
- [ ] T104 [US5] Add scoped timeline page with actor/delegation/selector/reason/scope filters and no protected-placeholder leakage in `src/Kpi.Web/Controllers/AuditController.cs` and `src/Kpi.Web/Views/Audit/Timeline.cshtml`
- [ ] T105 [US5] Add Approval Group and route snapshot PostgreSQL/restart tests in `tests/Kpi.IntegrationTests/Database/ApprovalRouteRestartTests.cs`
- [ ] T106 [US5] Add full US5 Playwright journey with keyboard and 390-pixel responsive evidence in `tests/Kpi.Web.EndToEndTests/ApprovalRouteJourneyTests.cs`
- [ ] T107 [US5] Run US5 focused/API/PostgreSQL/Playwright tests and record route, delegation, and timeline evidence in `.scratch/bsc-kpi-reference/evidence.md`

## Phase 8: Organization KPI Workspace Foundation and Cross-Feature Contracts

**Purpose**: Implement only the approved-baseline, capability/scope-filtered
Organization tree and Position navigation; publish future KPI-neighborhood and
Effective Segment contracts without fabricating KPI facts.

- [ ] T108 [P] Write RED contract tests for authorized lazy tree, Unit-expand/Position-select semantics, exact Baseline Applicability Segment, URL restoration, safe out-of-scope direct URL, and no KPI fixture state in `tests/Kpi.IntegrationTests/Api/OrganizationKpiWorkspaceContractTests.cs`
- [ ] T109 [P] Write RED Application query tests proving tree nodes/actions are server-filtered by current capability plus KPI Data Scope and never traverse editable workspace in `tests/Kpi.Application.Tests/Organizations/OrganizationTreeQueryTests.cs`
- [ ] T110 [P] Write RED Playwright tests for tree keyboard navigation, Position selection, refresh/back/forward/copy URL, drawer focus, 390-pixel layout, and unavailable KPI neighborhood in `tests/Kpi.Web.EndToEndTests/OrganizationKpiWorkspaceJourneyTests.cs`
- [ ] T111 Implement `OrganizationTreeReadModel`, approved-baseline context, continuation/search, and safe action projection in `src/Kpi.Application/Organizations/OrganizationTreeQuery.cs`
- [ ] T112 Implement the authorized organization-tree endpoint and validate/bind the existing future KPI-neighborhood and `EffectiveSegmentContract` schemas without adding operational Target/Actual/score endpoints in `src/Kpi.Web/Api/V1/OrganizationKpiWorkspaceController.cs` and `specs/002-organization-authorization/contracts/openapi.yaml`
- [ ] T113 Add Razor tree shell, Unit/Position semantics, URL-restorable context, empty/forbidden/conflict states, and honest unavailable KPI region in `src/Kpi.Web/Controllers/OrganizationKpiWorkspaceController.cs`, `src/Kpi.Web/ViewModels/Organization/`, and `src/Kpi.Web/Views/Organization/KpiWorkspace.cshtml`
- [ ] T114 Add Effective Segment consumer contract/provider interface and baseline-impact read integration in `src/Kpi.Application/Organizations/IEffectiveSegmentProvider.cs` and `src/Kpi.Application/Organizations/BaselineImpactQuery.cs`
- [ ] T115 Add workspace persistence/restart tests proving baseline/Position context survives Web restart without synthetic KPI facts in `tests/Kpi.IntegrationTests/Database/OrganizationKpiWorkspaceRestartTests.cs`
- [ ] T116 Run workspace quickstart, API, PostgreSQL, and Playwright tests and record target-port lock evidence in `.scratch/bsc-kpi-reference/evidence.md`

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Complete repository-level verification, documentation, performance,
security, accessibility, and handoff evidence.

- [ ] T117 [P] Update `specs/002-organization-authorization/quickstart.md` with final command/output references and known opt-in PostgreSQL prerequisites
- [ ] T118 [P] Update `specs/002-organization-authorization/contracts/openapi.yaml` examples and run the OpenAPI/ref/operationId contract validator in `tests/Kpi.IntegrationTests/Api/OpenApiContractTests.cs`
- [ ] T119 [P] Add security review tests for cross-Organization leakage, platform/bootstrap boundary, hidden timeline entries, stale concurrency, and no cross-action authorization cache in `tests/Kpi.IntegrationTests/Authorization/SecurityBoundaryTests.cs`
- [ ] T120 [P] Add accessibility regression checks for labels, focus restoration, keyboard-only operation, warning/error text, and non-color status in `tests/Kpi.Web.EndToEndTests/AccessibilityJourneyTests.cs`
- [ ] T121 [P] Add performance evidence collection and threshold assertions for SC-014 and SC-016 in `tests/Kpi.IntegrationTests/Performance/ReleaseBlockingThresholdTests.cs`
- [ ] T122 Run `./harness.cmd check`, opt-in PostgreSQL migration/restart tests, and the complete `Thinh-KPI-TEST` quickstart; record results in `.scratch/bsc-kpi-reference/evidence.md`
- [ ] T123 Review all feature docs against `spec.md`, `plan.md`, `data-model.md`, contracts, and `quickstart.md`; fix stale scope/target-lock claims in `specs/002-organization-authorization/`
- [ ] T124 Prepare the UI/UX, backend, API, authorization, database, restart, audit, and performance approval packet without editing `BSC-KPIs-API` or `BSC-KPIs` in `.scratch/bsc-kpi-reference/reference-approval.md`

## Requirement Traceability

The following compact map makes the primary coverage auditable without adding
requirement labels to the strict checklist format. Individual acceptance tests
must still assert the exact FR/SC identifier in their test name or evidence.

| Requirement range | Primary task coverage |
|---|---|
| FR-001–FR-016 | T009–T024, T025–T059 |
| FR-017–FR-021 | T005, T023, T060–T072 |
| FR-022–FR-027 | T073–T089 |
| FR-028–FR-035 | T090–T107 |
| FR-036–FR-043 | T009–T047, T075, T091–T107 |
| FR-044–FR-047 | T032, T037, T042, T045, T073, T114 |
| FR-048–FR-050 | T025–T046, T073–T089 |
| SC-001–SC-010 | T025–T122 across the corresponding story checkpoints |
| SC-011–SC-013 | T032, T037, T042, T045, T073, T114 |
| SC-014 | T010, T019, T074, T081, T087, T121 |
| SC-015 | T027, T039, T086 |
| SC-016 | T088, T121, T122 |

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
- Phase 8 workspace depends on US1/US2/US4 for approved baseline, Position, and
  current capability/scope filtering; it does not depend on later KPI Planning,
  Cascade, Actual, or Evaluation facts.
- Phase 9 depends on every desired story and workspace slice.

### User story completion order

`Foundational -> US1 -> US2 -> US3 -> US4 -> US5 -> Workspace -> Polish`

### Parallel opportunities

- T002-T007 can run in parallel after T001.
- T009-T015 can run in parallel; T016-T023 follow the shared contracts.
- Within US1, T025-T032 are parallel RED tests; T033-T037 are parallel domain
  seams; T043 and T044 can proceed in parallel after handlers are defined.
- Within US2, T048-T051 are parallel RED tests and T053/T054 can proceed in
  parallel after the domain model is stable.
- Within US3, T060-T063 are parallel RED tests; T066, T067, and T070 can proceed
  in parallel after T064/T065 contracts are fixed.
- Within US4, T073-T077 are parallel RED tests; T081/T082 and T083 can proceed
  in parallel after T078-T080 interfaces are fixed.
- Within US5, T090-T094 are parallel RED tests; T101/T102/T104 can proceed in
  parallel after T095-T100 interfaces are fixed.
- T108-T110 are parallel RED workspace tests; T111-T115 follow the query seam.
- T117-T121 are parallel polish tasks after all story checkpoints.

## Parallel Example: MVP (User Story 1)

```text
T025 BootstrapAuthorityTests.cs
T028 StructureValidationTests.cs
T029 StructureBaselineApiTests.cs
T030 BootstrapBaselineRestartTests.cs
T031 BaselineApplicabilityTests.cs
T032 BaselineImpactContractTests.cs
```

These RED tests may be written in parallel. Implementation begins only after the
test contracts are reviewed, then T033-T046 proceed in dependency order.

## Implementation Strategy

### MVP first

1. Complete Setup and Foundational phases.
2. Complete US1, including bootstrap provisioning/recovery, first baseline,
   immutable audit, PostgreSQL restart, and baseline-impact seams.
3. Stop and validate the US1 independent test and human UI/UX review gate.
4. Do not port to `BSC-KPIs-API` or `BSC-KPIs` at this checkpoint.

### Incremental delivery

1. Add US2 workforce history and primary Position behavior.
2. Add US3 task-oriented custom roles and versioning.
3. Add US4 scoped assignments, fresh authorization, and bootstrap handoff.
4. Add US5 route/group/delegation/timeline governance.
5. Add the authorized Organization KPI Workspace foundation and future KPI
   contracts.
6. Run the complete quickstart and release-blocking performance evidence before
   requesting reference approval.

### Definition of ready for the next phase

The current phase must have RED tests converted to passing tests, API/contract
validation, PostgreSQL restart evidence, Razor/Playwright evidence, and an
updated `.scratch/bsc-kpi-reference/evidence.md` entry before the next phase
starts.
