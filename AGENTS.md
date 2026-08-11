# Agent operating guide

## Mission

Keep this repository easy for an unfamiliar agent to understand, change, and verify. Prefer executable checks and discoverable repository knowledge over long prompt instructions.

## Start here

1. Read `README.md`, `docs/architecture.md`, and `docs/quality.md`.
2. Inspect the working tree before editing; preserve unrelated user changes.
3. For work that spans multiple components or has meaningful design risk, create or update a plan under `docs/plans/`.
4. Keep architecture decisions under `docs/decisions/` when a choice will constrain future work.

## Canonical commands

- Environment setup: `./harness.cmd bootstrap`
- Database schema migration (explicit local/test action): `./harness.cmd migrate`
- Full verification: `./harness.cmd check`
- Focused checks: `./harness.cmd lint` and `./harness.cmd test`
- Harness diagnostics: `./harness.cmd status`

On macOS or Linux, invoke `pwsh ./scripts/harness.ps1 <action>` instead.

`migrate` is the only schema-writing action. `bootstrap` and `check` do not
apply database migrations. Do not invent alternate setup or test paths. Extend
`.harness/harness.json` so local work and CI stay identical.

The migrator uses `ConnectionStrings:KpiMigration`; Web persistence uses only
`ConnectionStrings:KpiRuntime` under the explicit `Kpi:PersistenceProfile=Postgres`
profile. `InMemoryTest` is a development/test-only profile and must never be a
silent runtime fallback.

## Agent skills

### Issue tracker

Use local Markdown work items under `.scratch/<feature-slug>/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Use the repository's five canonical triage roles. See `docs/agents/triage-labels.md`.

### Domain docs

Before work that needs domain context, read the relevant `CONTEXT.md` and `docs/adr/` decisions. See `docs/agents/domain.md`.

## Git workflow

- Work and push directly on `main`.
- Treat every local or remote-tracking branch name containing `codex` as invalid; never create one.
- Run `./harness.cmd check` before committing or pushing. The branch policy in `.harness/harness.json` is enforced by the repository contract.

### BSC–KPI reference-first exception

For implementation, testing, review, or porting of the approved BSC–KPI system,
read `docs/porting/bsc-kpis/implementation-agent-prompt.md` before changing code.
That workflow is the product-owner-approved exception to direct-main work: use
`feature/bsc-kpi-reference-implementation` in this repository and keep
`BSC-KPIs-API` plus `BSC-KPIs` read-only until its explicit target-port gate is
released.

For the first BSC–KPI specification, the external requirements bot must read
`docs/porting/bsc-kpis/speckit-input-agent-prompt.md` and return only its
structured input package. The primary repository agent then reads
`docs/porting/bsc-kpis/speckit-specify-agent-prompt.md`, runs exactly one
`$speckit-specify`, and stops at the specification review gate. Specification
approval does not release either target repository.

## Change discipline

- Make the smallest coherent change that satisfies the request.
- Keep dependency direction consistent with `docs/architecture.md`.
- Add or update tests for behavior changes once an application stack exists.
- Update durable documentation in the same change as the behavior it describes.
- Never commit credentials, local `.env` files, generated dependencies, or build output.
- Avoid command strings that require shell evaluation; use explicit executables and argument arrays.

## Definition of done

A change is complete when the relevant behavior is implemented, documentation is current, and `./harness.cmd check` succeeds on Windows (or the equivalent `pwsh` command on macOS/Linux). If a check cannot run, report exactly which check and why.
