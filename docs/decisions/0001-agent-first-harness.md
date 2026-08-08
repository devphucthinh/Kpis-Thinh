# ADR 0001: Use a repository-local agent harness

- Status: Accepted
- Date: 2026-08-08

## Context

The repository starts empty and needs a stable environment for coding agents and human contributors before application technology is selected.

## Decision

Use a repository-local, technology-neutral harness with:

- concise agent instructions in `AGENTS.md`;
- machine-readable commands in `.harness/harness.json`;
- one PowerShell entrypoint for local and CI execution;
- durable architecture, quality, decision, and plan documents;
- direct process execution from argument arrays instead of evaluated shell strings.

## Consequences

- A future stack can be added without replacing the workflow contract.
- Local and CI verification share one entrypoint.
- The initial harness verifies its own structure but cannot lint or test application code until stack-specific steps are configured.
- Windows PowerShell 5.1 is sufficient through `harness.cmd`; PowerShell 7 (`pwsh`) is used on macOS, Linux, and CI.
