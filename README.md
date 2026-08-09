# Agent-first project

This repository starts with a small, deterministic engineering harness for Codex and human contributors.

## Quick start

```powershell
./harness.cmd bootstrap
./harness.cmd migrate
./harness.cmd check
./harness.cmd status
```

On macOS or Linux, use `pwsh ./scripts/harness.ps1 <action>`.

The repository uses the approved .NET 10 ASP.NET Core MVC and PostgreSQL KPI
stack. The harness runs commands directly from argument arrays and never
evaluates shell strings. `migrate` is explicit and is the only action that can
write schema; `bootstrap` and `check` remain safe with respect to PostgreSQL
schema.

The migrator reads `ConnectionStrings:KpiMigration`; the Web process reads
`ConnectionStrings:KpiRuntime` when `Kpi:PersistenceProfile` is `Postgres`.
Development may use the explicit `InMemoryTest` profile in
`appsettings.Development.json`; it is not a production fallback.

The KPI Management prototype is available under `src/`; its human/agent integration workflow is documented in [`HUONG_DAN_TICH_HOP_KPI.txt`](HUONG_DAN_TICH_HOP_KPI.txt).

On Windows, double-click [`run-kpi.bat`](run-kpi.bat) after setup to bootstrap the repository, start the local InMemory demo, and open `http://localhost:5080`. For durable PostgreSQL runtime persistence, open a new terminal after configuring `ConnectionStrings__KpiRuntime` and run `run-kpi.bat postgres`.

## Repository map

- [`AGENTS.md`](AGENTS.md): durable instructions for coding agents.
- [`.harness/harness.json`](.harness/harness.json): machine-readable setup and verification steps.
- [`scripts/harness.ps1`](scripts/harness.ps1): the single local and CI entrypoint.
- [`docs/architecture.md`](docs/architecture.md): system boundaries and dependency direction.
- [`docs/quality.md`](docs/quality.md): definition of done and verification policy.
- [`docs/decisions/`](docs/decisions/): durable architecture decisions.
- [`docs/plans/`](docs/plans/): execution plans for larger changes.

## Stack and migration boundary

1. Read [ADR 0002](docs/decisions/0002-kpi-application-stack.md) and
   [the architecture boundary](docs/architecture.md).
2. Run `./harness.cmd bootstrap` to restore the locked toolchain.
3. Configure non-secret migration and (for a durable Web run) runtime
   connection settings, then run `./harness.cmd migrate` against an allowed
   local/test database.
4. Run `./harness.cmd check`; CI executes the same PowerShell harness path.
5. Never add schema writes to Web startup, `bootstrap`, or `check`.
