# ADR 0002: Use a modular ASP.NET Core KPI application

- Status: Accepted
- Date: 2026-08-09

## Context

The repository has a governed KPI specification that must run locally as a
server-rendered web application, persist durable history in PostgreSQL, and be
portable into a larger C#/.cshtml application later. Formula execution and
lifecycle rules must remain independent of delivery and persistence concerns.

## Decision

Use one .NET 10 ASP.NET Core MVC host with four production boundaries:

- `Kpi.Domain` owns formula compilation/evaluation and KPI lifecycle invariants;
- `Kpi.Application` owns commands, actor capabilities, clock, transactions, and
  persistence ports;
- `Kpi.Infrastructure.Postgres` owns EF Core/Npgsql mappings, migrations and
  PostgreSQL adapters;
- `Kpi.Web` owns MVC/API delivery, localization, development personas and the
  reconciliation worker.

The dependency direction is `Web → Application → Domain`, with Infrastructure
allowed to reference Application and Domain. Domain never references ASP.NET,
EF Core, Npgsql, or harness code. PostgreSQL uses relational governance facts
and JSONB immutable formula/evaluation snapshots. The MVP excludes production
identity providers, external connectors, microservices, event sourcing, and
arbitrary expression execution.

## Consequences

- The formula engine is testable without a web host or database.
- One transaction can commit business state and its Audit Record together.
- A future host can replace the development actor provider behind the same
  command capability contract.
- PostgreSQL and browser verification are declared through the repository
  harness, keeping local and CI behavior identical.
