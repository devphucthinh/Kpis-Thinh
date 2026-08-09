# Phase 0 Research: Governed KPI Management

**Feature**: `001-kpi-management`  
**Purpose**: Resolve technical decisions needed to plan the approved behavior contract. Product scope remains governed by [spec.md](spec.md).

## Decision: One modular web application and one transactional database

**Decision**: Use a single ASP.NET Core MVC host with Domain, Application, PostgreSQL Infrastructure, and Web projects. Keep Formula, KPI governance, Period governance, Evaluation, and Audit behavior in the framework-independent Domain.

**Rationale**: The MVP needs one interactive experience, one machine-readable interface, shared transactions across version/period/evaluation/audit changes, and later extraction into a larger C# application. This is the smallest boundary set that prevents delivery and persistence details from contaminating governed behavior.

**Alternatives considered**:

- Controllers directly updating persistence: rejected because lifecycle and formula rules would be duplicated and difficult to test independently.
- Formula and KPI microservices: rejected because no independent consumer justifies deployment, authentication, and distributed transaction complexity.
- Event sourcing/CQRS framework: rejected because immutable Evaluations/Audit Records do not require replaying all business state.

## Decision: Closed in-house formula compiler and evaluator

**Decision**: Implement a tokenizer, Pratt parser, binder/type checker, typed AST, explicit serializer, and deterministic evaluator in the Domain. Add no expression-evaluation package.

**Rationale**: The approved language has a small fixed grammar, must preserve source spans and a versioned structured representation, and must never execute arbitrary code. A bounded in-house implementation makes precedence, Decimal policy, diagnostics, short circuiting, limits, and historical semantics explicit and testable.

**Alternatives considered**:

- `eval`, Roslyn scripting, expression compilation, reflection, or `DataTable.Compute`: rejected because they breach the closed-language security boundary.
- Generic formula libraries: rejected because their grammar/diagnostics/versioning would become a product dependency while still needing restrictive wrapping.

**References**: [System.Text.Json polymorphism](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism), [custom JSON serialization](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/customize-properties).

## Decision: Source-authoritative, server-generated Formula AST

**Decision**: Formula writes accept source, Formula Variables and declared result type. The server generates the typed AST. Persist `{ source, ast }` as the Formula Document and persist language/schema versions and checksum separately. Treat client-provided AST as non-authoritative.

**Rationale**: Exact source preserves author intent and spacing; the generated AST preserves historical meaning. Explicit discriminators, spans, result types, invariant Decimal strings, and safe failure for unknown versions protect round-trip behavior.

**Alternatives considered**:

- Source only: rejected because historical semantic representation could change after a parser upgrade.
- Client-authoritative AST: rejected because it bypasses validation and lets untrusted callers forge a formula meaning.
- CLR type-name serialization: rejected because it leaks implementation and makes compatibility/security harder to control.

## Decision: Deterministic Decimal policy

**Decision**: Use `System.Decimal` exclusively; accept invariant decimal literals only; reject input before parsing if it exceeds 28 significant digits or 10 fractional digits; normalize Decimal-producing operations to scale ≤10 using midpoint rounding away from zero; require `ROUND` scale to be an integer 0–10.

**Rationale**: Financial/business KPIs cannot use binary floating point. The product specifies Decimal/Boolean outcomes and deterministic rounding, including `25%`, `MOD`, and short-circuit evaluation.

**Alternatives considered**:

- `double`/`float`: rejected because binary representation changes business results.
- Implicit `Decimal.Parse` rounding of oversized input: rejected because it could silently change an authored value.

**References**: [System.Decimal guidance](https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-decimal), [Decimal parsing](https://learn.microsoft.com/en-us/dotnet/api/system.decimal.parse), [rounding modes](https://learn.microsoft.com/en-us/dotnet/api/system.midpointrounding), [Decimal remainder](https://learn.microsoft.com/en-us/dotnet/api/system.decimal.remainder).

## Decision: Layered persistence integrity in PostgreSQL

**Decision**: Use relational columns for identities, ownership, lifecycle states, version numbers, effective/period ranges, concurrency tokens and Current pointers. Use JSONB for Formula Documents, ordered variables, immutable Evaluation Inputs/outcomes/diffs, and audit summaries. Enforce durable facts with unique, foreign-key, partial-unique, range-exclusion and check constraints; enforce cross-table period-selection rules in short transactions.

**Rationale**: The system must query and protect governance facts while preserving compound historical snapshots without reconstructing them from mutable data. PostgreSQL ranges/exclusion constraints stop racing effective/period overlap writes that application pre-checks alone cannot prevent.

**Alternatives considered**:

- All state in JSONB: rejected because identity/status/range/uniqueness constraints become weak and expensive.
- All snapshots decomposed into mutable tables: rejected because formula/evaluation history becomes harder to preserve and evolve.
- Application checks only: rejected because concurrent publish/approval commands can race.

**References**: [Npgsql JSON mapping](https://www.npgsql.org/efcore/mapping/json.html), [PostgreSQL JSON](https://www.postgresql.org/docs/current/datatype-json.html), [PostgreSQL range types](https://www.postgresql.org/docs/current/rangetypes.html), [constraints](https://www.postgresql.org/docs/current/ddl-constraints.html), [partial indexes](https://www.postgresql.org/docs/current/indexes-partial.html).

## Decision: Target logical model with additive vertical migrations

**Decision**: [data-model.md](data-model.md) describes the target logical model, while schema evolution is delivered as additive vertical migrations: test-database infrastructure; Definition/Version plus the minimal audited Draft write; Version effectivity constraints; Period/Activation; Evaluation/Current/Supersession; then audit query/permission/cross-slice verification. No migration contains product or demo data, and no applied migration rewrites immutable Evaluation or Audit history.

**Rationale**: The target model must remain clear without requiring a risky one-shot migration before each vertical behavior is verifiable. This order makes the required database enforcement appear at the same time as the governed user behavior it protects.

**Alternatives considered**:

- One full-schema initial migration: rejected because it hides slice dependencies and postpones meaningful persistence verification.
- Schema migration demo data: rejected because local UX samples must never become Production content.

## Decision: Optimistic editing plus short transactional locks

**Decision**: Use PostgreSQL `xmin` as an opaque optimistic concurrency token for editable Definition metadata, Draft KPI Versions and Draft KPI Period Plans. Use short transactions and row/definition/activation locks for transitions that must serialize: publish hand-off, period approval/activation, correction, and reconciliation.

**Rationale**: User editing needs a clear stale-write response; cross-row lifecycle changes need more than a client token. Constraints remain the final authority for range and Current-result integrity.

**Alternatives considered**:

- Last-write-wins: rejected because it violates the no-silent-overwrite requirement.
- Application-generated tokens only: possible but easier to omit on a transition; PostgreSQL `xmin` is a native fit for this PostgreSQL-specific MVP.

**References**: [Npgsql concurrency tokens](https://www.npgsql.org/efcore/modeling/concurrency.html), [EF Core concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency), [EF Core transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions).

## Decision: Append-only Audit Records have separate protections

**Decision**: Every governed command writes its business change and Audit Record in one transaction. The first governed persistence slice creates the Audit Record table, runtime `SELECT`/`INSERT` restriction, and update/delete-rejecting trigger; each later governed slice writes its audit fact atomically. The final audit slice adds query/UI/permission and cross-slice verification. Tombstones for permitted hard-deletes retain logical identity and snapshot fields rather than requiring a foreign key to deleted content.

**Rationale**: Application logs cannot satisfy a user-queryable Audit Record contract. Both application and database protection are necessary so the normal runtime cannot mutate history after commit.

**Alternatives considered**:

- Audit interface convention only: rejected because a future write path could mutate rows.
- External immutable audit sink: deferred; it exceeds this local MVP and requires operational ownership.

**References**: [PostgreSQL privileges](https://www.postgresql.org/docs/current/ddl-priv.html), [GRANT](https://www.postgresql.org/docs/current/sql-grant.html), [triggers](https://www.postgresql.org/docs/current/sql-createtrigger.html).

## Decision: One idempotent reconciliation command

**Decision**: `ReconcileKpiLifecycle` is the one Application orchestration command. It invokes due Version effectivity/predecessor-retirement and Period scheduled-to-active/active-to-closed transitions once on startup and periodically thereafter through an injected clock. State-qualified updates append Audit Records only when a row actually changes; the hosted worker only calls this seam and owns no lifecycle policy.

**Rationale**: This handles downtime and repeat invocation without putting business rules into a timer or future cloud scheduler.

**Alternatives considered**:

- Timer-owned lifecycle rules: rejected because startup and future scheduler behavior would diverge.
- External scheduler now: rejected because deployment/infrastructure is out of scope.

**Reference**: [Hosted services](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services).

## Decision: Demo persona is an input seam, not authentication

**Decision**: Before persona-dependent HTTP, MVC, or browser work, establish the shared `ActorContext`/current-actor capability ports and authoritative Application checks. The Web project supplies only a development persona provider and refuses to start with persona switching outside Development. Command-level checks enforce separation of duty regardless of UI state.

**Rationale**: The spec excludes production authentication but requires demonstrable governance and protection from UI-only bypass.

**Alternatives considered**:

- Treat the persona selector as authentication: rejected as unsafe and explicitly out of scope.
- Skip actor checks until real login exists: rejected because approval/ownership/evaluation behavior needs them now.

**References**: [ASP.NET Core authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/introduction), [policy authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies).

## Decision: Test real persistence and use the repository harness

**Decision**: Unit-test Domain/Application seams; run PostgreSQL integration tests for JSONB, range, permissions and concurrency; use a web integration host for delivery contracts; add one browser smoke journey. First scaffold the solution/projects, initialize reviewed package lockfiles once through the canonical harness, then enforce locked bootstrap and prove `lint`, `test`, and `check` execute the intended projects before adding the first public-seam RED test. Expose all setup/check commands through `.harness/harness.json` and `harness.cmd`.

**Rationale**: Fakes cannot prove PostgreSQL-specific integrity or browser interaction, while a single smoke journey prevents over-investing in UI automation.

**Reference**: [ASP.NET Core integration tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests).

## Approved Technical Decisions

The active specification has no unresolved product ambiguity or outstanding technical approval. Canonical Formula/Evaluation snapshots use invariant Decimal strings across the approved domain; the MVP has no relational numeric projection. The MVP also uses distinct local schema-migration and restricted runtime database roles, with credentials supplied only outside the repository.
