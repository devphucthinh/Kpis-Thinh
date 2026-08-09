# Architecture

## Runtime topology

The MVP is one ASP.NET Core MVC process and one PostgreSQL database. The host
serves Vietnamese-first `.cshtml` pages and `/api/v1` JSON contracts. A hosted
reconciliation trigger calls the same Application operation used by tests; it
does not contain lifecycle policy.

```text
Browser / API client
        │
        ▼
Kpi.Web (MVC, API, localization, Development persona)
        │
        ▼
Kpi.Application (commands, queries, capability checks, transactions, ports)
        │
        ▼
Kpi.Domain (Formula, KPI, Period, Evaluation and Audit invariants)
        ▲
        │
Kpi.Infrastructure.Postgres (EF Core/Npgsql adapters and migrations)
        │
        ▼
PostgreSQL (relational governance facts + JSONB immutable snapshots)
```

## Boundaries

- `Kpi.Domain` is framework- and persistence-independent. It owns the closed
  formula language, typed AST, Decimal evaluator, aggregate state transitions,
  immutable Evaluation and Audit facts.
- `Kpi.Application` is the only command boundary. It receives `ActorContext`,
  enforces capabilities and separation of duty before mutation, and commits
  governed state with Audit Records through ports.
- `Kpi.Infrastructure.Postgres` may reference Domain and Application. It owns
  EF mappings, JSONB serialization, PostgreSQL constraints, safe forward-only
  migrations and least-privilege role setup.
- `Kpi.Web` may reference Application and Infrastructure. Controllers map
  transport contracts and views; they do not duplicate formula or lifecycle
  rules. Development persona switching is rejected outside Development.
- `.harness/` and `scripts/` are orchestration only. Application code never
  references harness implementation details.

## Forbidden dependencies and scope

Domain must not reference ASP.NET Core, EF Core, Npgsql, MVC, Razor, or the
harness. The MVP does not introduce microservices, a message bus, generic
workflow/event-sourcing frameworks, a SPA, production identity integration,
external connectors, or arbitrary code evaluation.

## Persistence and data flow

Relational columns protect company scope, identifiers, lifecycle status,
effective/period ranges, concurrency tokens, revision numbers and Current
evaluation pointers. JSONB stores exact Formula Documents, ordered variable
snapshots, Evaluation inputs/outcomes/diffs and audit summaries. Formula source
is authoritative on writes; the server generates and versions the AST. Official
evaluations and Audit Records are immutable and transactional.

## Verification

`harness.cmd` is the only setup and verification interface. It restores locked
packages, runs formatting/build/static checks, executes all test projects, and
enforces the `main` branch policy. See [ADR 0002](decisions/0002-kpi-application-stack.md)
and the feature plan for the rationale and migration sequence.
