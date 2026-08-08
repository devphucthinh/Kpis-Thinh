# Agent-first project

This repository starts with a small, deterministic engineering harness for Codex and human contributors.

## Quick start

```powershell
./harness.cmd bootstrap
./harness.cmd check
./harness.cmd status
```

On macOS or Linux, use `pwsh ./scripts/harness.ps1 <action>`.

The repository does not assume an application stack yet. When a runtime is selected, add its reproducible commands to [`.harness/harness.json`](.harness/harness.json). The harness runs commands directly from argument arrays and never evaluates shell strings.

## Repository map

- [`AGENTS.md`](AGENTS.md): durable instructions for coding agents.
- [`.harness/harness.json`](.harness/harness.json): machine-readable setup and verification steps.
- [`scripts/harness.ps1`](scripts/harness.ps1): the single local and CI entrypoint.
- [`docs/architecture.md`](docs/architecture.md): system boundaries and dependency direction.
- [`docs/quality.md`](docs/quality.md): definition of done and verification policy.
- [`docs/decisions/`](docs/decisions/): durable architecture decisions.
- [`docs/plans/`](docs/plans/): execution plans for larger changes.

## Adding a stack

1. Record the runtime and package-manager choice in a decision document.
2. Pin versions in the stack's native files.
3. Add bootstrap, lint, and test commands to `.harness/harness.json`.
4. Run `./harness.cmd check` locally; CI runs the same PowerShell entrypoint.
5. Replace the placeholder architecture description with the actual component map.
