# Implementation Plan: Governed KPI Management

**Branch**: `main` (active feature label: `001-kpi-management`) | **Date**: 2026-08-09 | **Spec**: [spec.md](spec.md)

**Input**: Approved behavior specification, canonical KPI terminology, repository constitution, repository architecture/quality policy, and the completed Grill findings.

## Summary

Build one locally runnable, modular web application for governed KPI authoring, review, period planning, evaluation, correction, and audit. The core is a framework-independent Domain with a closed formula engine and explicit lifecycle invariants. A thin Application layer coordinates commands, transactions, actor context, clock, and persistence ports. A single Web host provides the Vietnamese-first interactive experience and the minimum machine-readable interface; PostgreSQL provides durable relational constraints and immutable snapshots.

The design deliberately uses neither microservices nor a generic workflow/event-sourcing framework. One deployable application and one transactional database are sufficient for the approved MVP and keep later extraction into the larger application practical.

## Technical Context

**Language/Version**: C# 14 on .NET 10 LTS, pinned by `global.json` after bootstrap.

**Primary Dependencies**: ASP.NET Core MVC for server-rendered UI and minimal HTTP delivery; Entity Framework Core plus Npgsql for persistence; System.Text.Json for explicit formula serialization; Bootstrap-compatible markup and small vanilla JavaScript modules; xUnit, ASP.NET Core integration-test host, and Playwright for verification.

**Storage**: PostgreSQL 18.x. Relational columns hold identities, state, dates, ownership, concurrency and queryable governance facts; JSONB holds immutable structured formula/evaluation snapshots where their exact structure is part of history.

**Testing**: xUnit unit/application tests, PostgreSQL integration tests, HTTP/UI integration tests, and one high-value Playwright workflow. After the harness proves the intended test projects are executed, each behavior slice starts with a public-seam RED test, turns minimally GREEN, then runs the relevant harness command. Delivery-level acceptance evidence is added only after the behavior exists; merely registering a suite is not GREEN evidence.

**Target Platform**: Windows local development through `harness.cmd`; Linux CI through the same PowerShell harness entrypoint; a browser for the local interactive experience.

**Project Type**: One server-rendered web application with a versioned machine-readable interface.

**Performance Goals**: At least 95% of in-limit formula validation feedback is visible within one second; formula evaluation is bounded to 500 milliseconds and 10,000 evaluated nodes; reconciliation performs only due state transitions and is safe to repeat.

**Constraints**: One seeded company; manual inputs only; Gregorian calendar in `Asia/Ho_Chi_Minh`; Decimal/Boolean formula values; no arbitrary formula execution; no production authentication; no committed credentials; the repository harness is the only setup/verification interface.

**Scale/Scope**: MVP supports up to 100 Formula Variables, 10,000 source characters, AST depth 32, and one active version per KPI Definition at any instant. Multi-company administration, employee assignment, providers, dashboards, and external inputs remain future extensions.

## Constitution Check

### Pre-design gate

| Constitution principle | Plan response | Result |
|---|---|---|
| Discoverable Repository Context | Uses `AGENTS.md`, `CONTEXT.md`, `docs/architecture.md`, `docs/quality.md`, the constitution, active spec, and Grill design; records technical decisions in this feature directory. | Pass |
| One Deterministic Verification Path | Adds every setup, formatting, lint, test, browser, and full-check command to `.harness/harness.json`; no parallel verification workflow. | Pass |
| Behavior-First Vertical Slices | Establishes harness/test-project proof before the first RED, then uses public command/formula seams and a RED-to-GREEN vertical-slice sequence. | Pass |
| Explicit Boundaries and Decisions | Keeps Domain independent of Web/EF; requires ADR 0002 before the runtime stack is introduced. | Pass |
| Minimal, Safe, Reviewable Change | Uses one deployable application, a single database, no credentials in Git, and no speculative service/bus/event-sourcing infrastructure. | Pass |

### Architecture gate

The current repository contains only the harness. The proposed dependency direction is compatible with `docs/architecture.md`: application code never depends on the harness, while the harness invokes declared application commands. Before implementation, `docs/architecture.md` and ADR 0002 must replace the stack-neutral placeholder with this approved direction.

### Post-design gate

The detailed design below preserves the same five principles. No waiver or complexity exception is required. The constitution must be rechecked after task generation and again before implementation is declared complete.

## Project Structure

```text
specs/001-kpi-management/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
└── contracts/
    ├── formula.md
    ├── application-operations.md
    └── http-api.md

src/
├── Kpi.Domain/                    Formula, Kpis, Periods, Evaluations, Auditing
├── Kpi.Application/               commands, queries, actor/clock/store ports
├── Kpi.Infrastructure.Postgres/   EF mappings, migrations, PostgreSQL adapters
└── Kpi.Web/                       MVC/API delivery, localization, demo persona, worker

tests/
├── Kpi.Domain.Tests/
├── Kpi.Application.Tests/
├── Kpi.IntegrationTests/
└── Kpi.Web.EndToEndTests/
```

`Kpi.Domain` has no reference to application framework, database or delivery projects. Test projects reference only the modules they exercise; PostgreSQL and Web tests use the outer boundaries rather than testing private methods.

## System and Module Boundaries

### System boundaries

```text
Browser / machine-readable clients
        │
        ▼
Kpi.Web ─────────────────── delivery, localization, demo persona selection,
        │                    MVC pages, HTTP transport, scheduled trigger
        ▼
Kpi.Application ─────────── commands, queries, actor/clock/storage ports,
        │                    transaction orchestration, error mapping
        ▼
Kpi.Domain ──────────────── KPI governance, Formula, Period, Evaluation,
        │                    Audit semantics and framework-free invariants
        │
        ▲
Kpi.Infrastructure.Postgres persistence adapters, migrations, PostgreSQL
                             constraints, runtime-role audit protection
        │
        ▼
PostgreSQL
```

Dependencies are one-way:

```text
Kpi.Web -> Kpi.Application -> Kpi.Domain
Kpi.Infrastructure.Postgres -> Kpi.Application + Kpi.Domain
Kpi.Domain -> no Web, ORM, database, framework, or delivery dependency
```

### Minimum modules

| Module | Responsibility and ownership | Must not own | Allowed dependencies |
|---|---|---|---|
| `Kpi.Domain.Formula` | Formula Variable rules, tokenization, parsing, binding/type checking, typed AST, deterministic Decimal/Boolean evaluation, diagnostics and limits. | UI editor state, transport, persistence, actor lookup, I/O, scheduling. | BCL only. |
| `Kpi.Domain.Kpis` | KPI Definition identity, KPI Version lifecycle, effective ranges, archive/restore, ownership-transfer semantics. | Database queries, HTTP authorization, rendering, audit storage. | Domain Formula and common value objects. |
| `Kpi.Domain.Periods` | KPI Period Plan lifecycle, exact version selections, activation/amendment semantics and period invariants. | Timers, database scans, UI state. | Domain Kpis/common value objects. |
| `Kpi.Domain.Evaluations` | Immutable KPI Evaluation attempts, Current KPI Evaluation selection, Superseding Evaluation diff/reason rules. | Formula parsing implementation, persistence mutation, display formatting. | Domain Formula/Periods/common value objects. |
| `Kpi.Domain.Auditing` | Audit Record shape and governed-action facts. | Technical logging, mutable audit repositories. | Domain common value objects. |
| `Kpi.Application` | Public commands/queries, actor capability checks, separation of duty, transaction boundaries, and the single `ReconcileKpiLifecycle` orchestration seam over storage/clock/current-actor ports. | Web concerns, EF entities, SQL, formula internals, or duplicated lifecycle policy in the worker. | Kpi.Domain only. |
| `Kpi.Infrastructure.Postgres` | EF mappings, PostgreSQL migrations, storage-port adapters, constraints, transaction implementation, database permission setup. | Lifecycle policy or formula semantics. | Kpi.Application + Kpi.Domain + persistence packages. |
| `Kpi.Web` | MVC pages, transport models, localization, diagnostics presentation, development-only persona selector, hosted trigger that invokes `ReconcileKpiLifecycle`. | Direct lifecycle or formula policy, direct table updates, production identity implementation. | Kpi.Application + Infrastructure composition root. |

The Formula module is intentionally a deep module: callers see compile/evaluate contracts, never a trusted AST constructor or an execution callback.

## Public Interfaces and TDD Seams

Public seams are behavior-oriented; tests must not call private parser, controller, ORM, or view internals.

| Seam | Public behavior | Main test boundary |
|---|---|---|
| `FormulaCompiler.Compile` | Converts source, Formula Variables, and declared result type into a typed, versioned Formula Compilation or diagnostics. | Domain unit test. |
| `FormulaEvaluator.Evaluate` | Produces Decimal/Boolean success or structured Failure from a compiled formula and non-null Evaluation Inputs. | Domain unit test with deterministic budget/time source. |
| KPI Definition/Version commands | Create Draft, edit Draft, submit, approve/reject, publish, retire, clone, archive/restore, transfer ownership. | Application command test with fake actor/clock/store. |
| KPI Period commands | Create/edit plan, select exact version, submit, approve/reject/cancel/amend. | Application command test with fake clock; PostgreSQL integration for overlap constraints. |
| `ReconcileKpiLifecycle` | Reconciles due Version effectivity/predecessor retirement and Period scheduled-to-active/active-to-closed transitions in one idempotent Application orchestration seam. | Application command test with fake clock; PostgreSQL transaction and restart/catch-up integration tests. |
| Evaluation commands | Create official attempt, resolve Current KPI Evaluation, create a Superseding Evaluation. | Application command test plus PostgreSQL transaction test. |
| Audit query | Returns immutable ordered Audit Records filtered by entity/actor/type/date. | Integration test through query port/HTTP contract. |
| Delivery contracts | Validate formula/Test Run, governed commands, read history, localized ProblemDetails, concurrency response. | Web integration test; one browser smoke journey. |

Application command inputs include `ActorContext`, command data, supplied concurrency token when editing, and a correlation identifier. Domain failures remain typed results; delivery maps them only at its outer boundary.

## Domain Model and Invariants

The canonical language remains `CONTEXT.md`; this model does not rename it.

### Aggregate boundaries

| Boundary | Contains | Core invariants |
|---|---|---|
| **KPI Definition** aggregate | KPI Definition metadata and KPI Version metadata/content. | Immutable company-scoped KPI Code; sequential version numbers; only Draft content editable; a Definition retains stable identity; effective ranges do not overlap; one currently effective Published KPI Version. |
| **KPI Period Plan** aggregate | KPI Period, exact version selections, review decision, amendments, resolved activations. | Planner cannot self-approve; dates/selections freeze at approval; cadence and duplicate-definition rules; no illegal overlap; state transitions are explicit. |
| **KPI Period Activation evaluation stream** | KPI Period Activation, immutable KPI Evaluation attempts and Current pointer/flag. | Official evaluation only when Active; every attempt immutable; only successful attempt becomes Current; Superseding Evaluation keeps predecessor, full new input snapshot, diff and reason. |
| **Audit Record** append-only stream | Audit Record facts keyed to company/entity/action/correlation. | No update/delete behavior; every governed state change has actor, time, event, context and required reason. |
| **Organization/Actor** references | One seeded company and capability-bearing actors. | Company scope is always retained; demo personas are an adapter input, not production identity. |

### Value objects

- `KpiCode`: canonical uppercase company-unique identifier.
- `FormulaVariableDefinition`: code, localized display metadata, Decimal/Boolean type, required flag, compatible optional default, display order.
- `FormulaDocument`: exact source and server-generated typed AST snapshot.
- `FormulaValue`: closed Decimal or Boolean value; Decimal uses invariant text at transport/snapshot boundaries.
- `EvaluationOutcome`: `Success(FormulaValue)` or `Failure(code, localization arguments, optional SourceSpan)`; never successful Null.
- `EffectiveRange` and `PeriodInterval`: half-open ranges `[start, end)`.
- `ActorContext`: actor identity, company scope, demonstrated capability, environment mode, correlation id.
- `VersionSelection`, `EvaluationInputSnapshot`, `EvaluationCorrectionDiff`, and `ConcurrencyToken`.

### Lifecycle rules

```text
KPI Version: Draft -> InReview -> Approved -> Published -> Retired
                         \-> Rejected -> Draft

KPI Period: Draft -> InReview -> Scheduled -> Active -> Closed
                    \-> Rejected -> Draft
             Draft/InReview/Scheduled -> Cancelled
```

- Publish is the only operation that assigns `effective_from` and moves an approved version into Published.
- `ReconcileKpiLifecycle` is the only Application orchestration seam for due lifecycle work. It invokes Version effectivity/predecessor retirement and Period scheduled-to-active/active-to-closed policy together; the hosted worker only invokes this seam and contains no lifecycle policy.
- Publishing a successor closes the predecessor range at the successor start; lifecycle reconciliation retires the predecessor at that instant. The hand-off is one transaction.
- Approval delegates can decide but cannot edit submitted content; a Period Planner cannot approve their own plan.
- Approved Period selections are immutable. An amendment is a separate reviewed proposal; it never overwrites the approved plan.
- Closing blocks ordinary evaluation but permits a governed correction of an existing successful evaluation with the same KPI Version, complete new inputs and mandatory reason.

## Important Data Flows

| Flow | Boundaries crossed | Validation/invariant checkpoints |
|---|---|---|
| Create/update KPI Definition | Web → Application command → Definition aggregate → store + Audit Record | Company-scoped immutable code; Draft-only content; concurrency token; actor capability. |
| Create KPI Version | Web → Application → Definition aggregate → store + audit | Sequential number, predecessor/change summary, required content, Draft status. |
| Define formula and variables | Web editor → formula validation seam → Draft command → Formula module → store | Variable uniqueness/order/default type; declared result type; source span diagnostics; client AST never trusted. |
| Validate/Test Run | Web/API → Formula compile/evaluate → response | Formula safety limits; non-null inputs/defaults; Test Run returns result only, opens no evaluation/audit transaction. |
| Review/publish/effect version | Web/API → Application actor check → Definition aggregate → transaction + audit | Not self-editing; approver role; approved-only publish; range exclusion; predecessor hand-off. |
| Create/approve period | Web/API → Application → Period aggregate → transaction + audit | Cadence/version eligibility, no duplicate definition, no illegal overlap, planner/approver separation, freeze at approval. |
| Reconcile KPI lifecycle | Hosted trigger/future scheduler → `ReconcileKpiLifecycle` → Version and Period policy → store transaction + audit | State-qualified Version and Period transitions; atomically create activations; idempotent repeat/downtime catch-up. |
| Official KPI Evaluation | Web/API → Application → Formula engine + evaluation stream → transaction + audit relation | Active activation, complete inputs/defaults, exact Formula Document/version snapshot, success/failure immutable, Current only on success. |
| Correct/Supersede evaluation | Web/API → Application → evaluation stream → transaction + audit relation | Existing successful evaluation, same KPI Version, mandatory reason, full new input snapshot, server-derived diff, predecessor retained. |
| Read audit/history | Web/API → Application query → store read model | Ordered immutable Audit Records, filters, history links; no write path. |

## Persistence and Consistency Strategy

### Persistence model

The list below is the target logical data model, not an instruction to deliver all schema objects in one migration. The additive migration order is defined in [Migrations and configuration](#migrations-and-configuration); each vertical slice introduces only the schema and enforcement needed for its verified behavior.

Keep governance facts relational:

- `organizations`, `actors`;
- `kpi_definitions`, `kpi_versions`;
- `kpi_periods`, `kpi_period_activations`, `kpi_period_amendments`;
- `kpi_evaluations`;
- `audit_records`.

Keep historical structured values in JSONB snapshots:

- `FormulaDocument` with exact source and generated AST;
- ordered Formula Variable schema;
- Evaluation Input snapshot, Evaluation Failure details, Evaluation Correction Diff;
- concise Audit Record change summary and deletion tombstone snapshot.

JSONB object-key formatting is not a round-trip guarantee. Exact source remains its own string field within the Formula Document; ordered lists remain JSON arrays; semantic structure is verified after reload rather than comparing raw JSON text.

### Database-enforced facts

- unique `(organization_id, kpi_code)`;
- unique `(kpi_definition_id, version_number)`;
- unique `(kpi_period_id, kpi_definition_id)` activation;
- foreign keys that retain Version, Activation, Evaluation and predecessor history;
- partial unique index: at most one successful `Current KPI Evaluation` per activation;
- exclusion constraint over half-open effective ranges for each KPI Definition;
- exclusion constraint over same-company, same-cadence KPI Period intervals;
- check constraints for valid non-empty ranges and known states;
- update/delete-rejecting trigger on `audit_records`, with runtime database role granted only `SELECT`/`INSERT` there.

The same-KPI-Definition-across-overlapping-Period rule spans activation and period rows. Enforce it inside the approval transaction under a definition-scoped lock plus an integration concurrency test, rather than adding a difficult cross-table trigger.

### Transaction boundaries

One explicit transaction includes the business transition and its Audit Record for:

- create/edit/submit/review/publish/retire/archive/restore/ownership transfer;
- period plan change/review/approval/cancellation/amendment;
- scheduled→active and active→closed reconciliation;
- official evaluation, correction, Current replacement, and diff creation.

Test Run begins no persistence transaction. Query operations are read-only.

Audit arrives with the first governed mutation rather than as a final feature: Draft create/update, submit/reject/return, approval, publish, archive/restore/delete, ownership transfer, Period actions, official Evaluation and correction each commit their resulting Audit Record in the same transaction. The later audit slice adds query/UI/permission coverage and cross-slice proof; it does not postpone business audit.

### Decimal persistence decision

Formula and Evaluation snapshots store Decimal as invariant strings so the entire approved 28-significant-digit domain is preserved. `numeric(28,10)` is not used as the authoritative result storage because it permits only 18 integer digits when scale 10 is reserved. If a later queryable numeric projection is required, it must either reject values outside that 18-integer-digit range or receive explicit approval for a wider precision; it must never silently reduce historical values.

## Concurrency and Integrity

| Race | Protection |
|---|---|
| Concurrent Draft change | PostgreSQL `xmin` optimistic concurrency token on editable Definition metadata, Draft Version and Draft Period; stale token returns `CONCURRENCY_CONFLICT` and no overwrite. |
| Concurrent version review/publish | Lock Definition row and execute at serializable/repeatable-read boundary; effective-range exclusion constraint is the final guard. |
| Concurrent period approval/activation | Lock Period row; conditional state update ensures only one caller transitions/reports audit; range constraints reject overlap. |
| Same Definition in overlapping periods | Definition-scoped lock plus eligibility query in the approval transaction; integration test runs two approvals concurrently. |
| Simultaneous evaluation/correction | Lock Activation/current-success row; insert attempts immutably; partial unique index ensures one Current success; failure never changes Current. |
| Multiple corrections of same evaluation | Correction command locks the target activation and validates the predecessor/current chain, then inserts a distinct successor with correlation/audit facts. |
| Audit mismatch | Business state and Audit Record share one transaction; runtime role cannot mutate audit after commit. |

Every write accepts an opaque concurrency value where the resource is user-editable. Database constraint violations are translated into stable domain/application conflicts, not leaked as driver errors.

## Formula Execution Boundary

Use a closed, in-house formula subsystem; add no expression-evaluation package.

1. Tokenizer creates tokens with zero-based UTF-16 `SourceSpan`.
2. A Pratt parser implements exactly the approved precedence, parentheses, postfix percentage, unary `-`/`NOT`, binary operators, `IF`, `ROUND`, `ABS`, and `MOD`.
3. A binder/type checker resolves only declared Formula Variables, checks function arity/type rules and declared result type, and produces a typed AST.
4. A deterministic evaluator consumes the typed AST and closed `FormulaValue` union. It uses only `System.Decimal` and Boolean values, short-circuits `IF`/`AND`/`OR`, counts nodes, and checks a monotonic 500 ms budget.
5. An explicit JSON converter serializes stable AST discriminators; source is authoritative, AST is server-generated read data, and unknown AST version/discriminator fails safely.

No source is compiled into host code, reflected over, executed by a scripting runtime, or given callbacks/I/O access. Formula Test Run and official Evaluation invoke the same compiler/evaluator; the Application command chooses whether an immutable Evaluation is persisted.

Stable failure families include formula source/parse/bind/type, input/default, Decimal/rounding/overflow, budget/timeout, lifecycle/eligibility, governance/self-approval, concurrency, and persistence integrity failures. Delivery localizes messages while preserving English codes and SourceSpan data.

## Authorization and Governance

All Application commands receive an `ActorContext` and enforce capabilities before invoking state transition methods. This makes the browser/UI incapable of granting authority by itself.

- KPI Creator: creates Definitions and owns/edits Draft Version content.
- KPI Policy Approver: approves/rejects Versions and transfers ownership, but does not edit submitted content.
- KPI Period Planner: prepares/submits Period Plans.
- KPI Period Approver: approves/rejects a different planner's submitted plan, without editing it.
- KPI Evaluator: creates official Evaluations and governed corrections, not formula changes.
- KPI Administrator: reads governance/history but cannot modify creator-owned KPI content.

Before any persona-dependent HTTP, MVC, or browser work, the shared Web foundation establishes the `CurrentActor` port, authoritative Application capability checks, and the Development-only `DevelopmentPersonaProvider`. Startup fails if persona switching is enabled outside Development. A future real identity/authorization adapter implements the same port; it does not bypass command-level checks or lifecycle reconciliation.

## Error Handling and Delivery Boundaries

### Failure categories

| Category | Examples | Delivery behavior |
|---|---|---|
| Validation | invalid KPI Code, variable/default, formula syntax/type/limits | stable validation code, localized message, field/source span where relevant |
| Lifecycle/governance conflict | forbidden transition, unauthorized actor, self approval, publish before approval | stable conflict/forbidden code; no partial state change |
| Eligibility/range conflict | overlapping effective range, ineligible version, overlapping period | conflict with clear reason and affected rule |
| Evaluation failure | missing input, divide by zero, overflow, timeout | immutable Failure for official attempt; transient Failure for Test Run |
| Correction conflict | non-success predecessor, version mismatch, missing reason, concurrent correction | conflict/validation code with prior state unchanged |
| Concurrency | stale `xmin` token or conflicting concurrent write | opaque concurrency conflict; caller reloads current state |
| Infrastructure | unavailable database, configuration or unexpected exception | safe generic error and correlated technical log; no domain status claimed |

The Web host exposes the minimum HTTP surface needed for the spec: formula validation/Test Run, governed Definition/Version/Period/Evaluation commands, audit/history reads, and localized error responses. Transport request/response models are separate from commands; no client can submit a trusted AST or invoke domain transitions through direct persistence.

## UX, Localization, and Demo Data

- Use server-rendered Vietnamese-first screens for KPI list/detail, Draft editor, review queue, Period plan, Evaluation history/correction, and Audit timeline.
- The Draft editor has distinct regions for ordered Formula Variables, source/insert/autocomplete actions, syntax reference/diagnostics/generated AST, and transient Test Run result.
- Status badges and action confirmations visibly distinguish Draft/In Review/Approved/Published/Retired and Draft/In Review/Scheduled/Active/Closed/Cancelled.
- Test Run has a clearly non-persistent label and never appears in official Evaluation history.
- History shows current versus superseded/failed attempts, correction reason, input diff and result diff.
- `vi-VN` is the default; core `en-US` resources ship immediately; formula keywords, codes and machine-readable property names remain canonical English.
- Development-only, idempotent application seeding creates one company, the six agreed personas, and `REVENUE_ACHIEVEMENT` with representative Decimal/Boolean variables. It is visibly development-only, lives outside schema migrations, and never runs in Production.

## Migrations, Configuration, and Observability

### Migrations and configuration

- Migrations are additive vertical slices, not one full-schema initial migration. The target logical model remains [data-model.md](data-model.md), while the migration sequence is: (1) test-database infrastructure only; (2) Definition/Version persistence plus the minimal `audit_records` protection needed by Draft authoring; (3) effective-range constraints and Version-governance enforcement; (4) Period/Activation persistence and constraints; (5) Evaluation/Current/Supersession persistence and constraints; (6) audit query, permissions, and cross-slice integrity proof. Each applied migration is forward-only and immutable history is never rewritten.
- Schema migrations contain schema, constraints, triggers, indexes, and any unavoidable static production reference data only. For this MVP they contain no product or demo data.
- `Development` composition provides an idempotent development-only seeder for the company, six personas, and sample KPI. It is not a migration, never runs in Production, and does not require production data to exist.
- Formula language/AST schema versions allow old snapshots to remain readable. Decimal JSON snapshots retain the canonical invariant strings; no relational numeric projection is introduced in the MVP.
- Separate a privileged schema-migration credential from the limited runtime credential. Store both only in user secrets/environment variables, never `.env` or Git.
- The first governed persistence slice creates append-only audit protection: the runtime role receives only `SELECT`/`INSERT` on `audit_records`; the table rejects `UPDATE`/`DELETE`; ownership, DDL, and truncation remain unavailable to the runtime role. The migration role alone creates or changes this protection. The final audit slice verifies those restrictions end-to-end.
- Bootstrap creates/updates schema only for explicitly configured local/test databases. Integration tests use a distinct `kpi_lab_test` database and validate its name before any drop/recreate step.

### Observability versus product audit

- **Audit Record**: required business evidence, append-only and user-queryable; it is not a substitute for logs.
- **Technical logs**: structured correlation id, actor id, command name, result code and unexpected exception detail; do not log Formula Inputs or credentials by default.
- **Diagnostics**: formula codes/source spans returned to users; technical exception details stay server-side.
- **Metrics**: formula validation/evaluation duration, formula-limit rejections, reconciliation transitions, conflict counts and failed hosted reconciliation runs. Metrics do not replace Audit Records.

## Verification Strategy and Harness Integration

### Tests mapped to boundaries

- Formula unit tests: token spans, grammar/precedence, variable/type rules, Decimal policy, every operator/function, short circuit, limits, diagnostics, source/AST serialization.
- Domain/application tests: all lifecycle paths, self-approval rejection, effective hand-off, period eligibility/freeze/amendment, Current selection, correction diff and no-Test-Run-persistence behavior using fake actor/clock.
- PostgreSQL integration tests: migration from empty database, JSONB semantic round trip, exact Decimal strings, range/unique/foreign-key invariants, `xmin` stale writes, transactional audit, append-only role/trigger behavior, and concurrent command races.
- Web integration tests: transport validation, localized error mapping, no client AST authority, persona safety, and visible state/history contracts.
- Browser test: one persona-separated author → review → publish → period → activate → evaluate → correct → audit/archive/restore path.

The harness is extended, never bypassed:

| Harness action | Planned responsibility |
|---|---|
| `bootstrap` | During scaffolding, perform the one-time deterministic lockfile initialization through this declared harness action, review and commit every project lockfile, then enforce `dotnet restore --locked-mode` on all recurring runs. It also installs the Playwright browser when absent, checks required local configuration, and applies only explicit local/test migrations. |
| `format` | Verify formatting. |
| `lint` | Formatting, static analysis and build without restore after locked bootstrap has succeeded. |
| `test` | Existing branch-policy test plus the intended Domain, Application, PostgreSQL integration and browser smoke projects, all without restore; harness wiring proves the projects are actually executed before the first behavior RED test. |
| `check` | Contract check, locked bootstrap, lint, and test in that order; the Windows definition of done and the command CI invokes. |

No command is added outside `.harness/harness.json`; CI continues to execute the PowerShell harness entrypoint. The one-time lockfile initialization is a bootstrap transition, not a user-facing parallel verification command; after reviewed lockfiles exist, bootstrap must never rewrite them.

## Dependencies and Technology Choices

| Dependency | Why it is needed | Requirement enabled | Simpler alternative rejected |
|---|---|---|---|
| .NET 10 / C# 14 | Matches the approved future host direction and provides the web/runtime platform. | Interactive journey, localization, hosted reconciliation, maintainable extraction. | A second runtime would create an unnecessary integration boundary. |
| ASP.NET Core MVC | Server-rendered pages plus minimal HTTP interface in one host. | Local interactive UX, machine-readable reads, localized errors. | Separate SPA/API adds two deployable surfaces without MVP value. |
| PostgreSQL 18.x | Enforceable range/partial/foreign-key constraints, JSONB snapshots and transactions. | Immutable history, effective/period integrity, audit protection. | In-memory or fake storage cannot prove durable invariants. |
| EF Core + Npgsql | Migrations, mappings, transactions and PostgreSQL integration with the chosen host. | Persistence ports and reproducible schema evolution. | Raw SQL everywhere duplicates mapping/transaction plumbing. |
| System.Text.Json | Explicit, versioned Formula AST and Decimal string serialization. | Exact source/structured representation contract and safe unknown-type rejection. | Implicit serializer polymorphism leaks CLR shape and weakens version control. |
| Bootstrap-compatible HTML + vanilla JavaScript | Accessible responsive UI and a small formula-editor enhancement surface. | Draft editor, lifecycle pages and local UI demonstration. | SPA framework is unnecessary scope/dependency. |
| xUnit + integration host + Playwright | Public-seam, persistence and end-to-end confidence. | Specification acceptance and browser workflow. | Unit tests alone cannot prove PostgreSQL or UI behavior. |

No parser, scripting, generic expression evaluator, message bus, CQRS framework, event store, external scheduler, identity provider, notification provider, or external data connector is introduced.

## Vertical Implementation Strategy

These are planning slices only; `$speckit-tasks` will turn the approved plan into ordered tasks. Each slice is a public, behavior-proving vertical increment and adds its required Audit Record atomically with the governed write. Delivery/browser tests follow behavior rather than acting as a substitute for it.

1. **Scaffold and harness proof** — pin SDK/packages, create solution/module/test boundaries and architecture ADR, initialize reviewed package lockfiles through `bootstrap`, switch recurring bootstrap to locked restore, wire `lint`/`test`/`check` to the intended projects, and prove the canonical harness executes them. Only then introduce the first behavior RED test.
2. **Formula and Draft authoring** — Formula Variable validation, tokenizer, parser, typed AST, serialization and deterministic evaluator; Definition/Version Draft persistence in the second additive migration; Draft create/update audit in the same transaction; then source-authoritative validation/Test Run delivery and editor behavior.
3. **Version governance and effectivity** — submit/reject/return/approve/publish, archive/restore/delete and ownership transfer, with their audit facts; third additive migration adds effective ranges, constraints and predecessor hand-off enforcement; `ReconcileKpiLifecycle` performs the due effectivity/retirement transition.
4. **Period planning and activation** — fourth additive migration adds Period/Activation persistence; Period lifecycle, exact version selection, separation of duty, overlap rules and amendments, all with audit; the same lifecycle seam handles scheduled-to-active/active-to-closed work and atomically creates activations.
5. **Official Evaluation and correction** — fifth additive migration adds Evaluation/Current/Supersession persistence; official evaluation, immutable outcomes, Current selection, correction diffs/reasons and audit; Test Run stays transient.
6. **Audit completion and durable integrity** — final additive audit slice supplies audit/history query/UI authorization, database-role/trigger verification, concurrency and cross-slice transaction evidence. It verifies the audit protections introduced with the first governed mutation; it does not introduce audit late.
7. **Shared delivery foundation and screens** — establish development-only persona safety and authoritative capability enforcement before persona-dependent HTTP/MVC/browser work; then add localized contracts and the authoring, governance, period, evaluation/correction, and audit screens in the order behavior becomes available.
8. **Acceptance and operational finish** — add a delivery-level end-to-end workflow only after each component behavior is green; complete harness/CI parity, integration guide, measurable acceptance evidence, and the required final constitution recheck.

## Requirement Traceability

| Spec area | Design coverage |
|---|---|
| User Stories 1–5 | Formula module/authoring UI; Version governance; Period governance; Evaluation stream; Audit/history UI and query boundary. |
| FR-001–015 | KPI Definition aggregate, actor context, Version lifecycle, effective-range exclusion, archive/delete/ownership transaction and audit. |
| FR-016–031 | Formula Variable value object, closed Formula module, source-authoritative AST, Decimal policy, diagnostics, Test Run command. |
| FR-032–040 | KPI Period Plan aggregate, version selection eligibility, separate approver check, range constraints, amendment, reconciliation command. |
| FR-041–047 and FR-059 | Activation evaluation stream, immutable attempts, Current partial unique invariant, correction transaction/diff and post-close correction rule. |
| FR-048–049 | Audit Record model, application transaction integration, append-only database protection and filtered read query. |
| FR-050–056 | Development-only actor provider, MVC/HTTP delivery, localization, UI status/history, integration guide and error contract. |
| FR-057–058 | Stable scoped identities, persisted exact Version/Activation references and prevention of breaking frozen selection. |
| AC-001–016 / SC-001–012 | Verification matrix above, harness-owned tests, browser smoke journey, round-trip/concurrency/reconciliation and guide evidence. |

No active specification requirement is uncovered.

## Human-Approved Technical Decisions

1. **Canonical Decimal storage**: the approved 28-significant-digit / 10-fractional-digit formula domain is preserved in canonical invariant Decimal strings in Formula and Evaluation JSON snapshots. `numeric(28,10)` and any relational numeric projection are excluded from the MVP because they could reduce the approved range. A later projection requires a separate explicit decision and must never silently reduce historical values.
2. **Local database roles**: the approved MVP uses a privileged schema-migration role and limited runtime role to make append-only audit permissions meaningful. Their passwords and provisioning remain a user-controlled local setup action, never repository content.
