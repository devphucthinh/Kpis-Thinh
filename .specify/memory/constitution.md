<!--
Sync Impact Report
- Version change: 1.1.0 -> 1.1.1
- Modified principles: none
- Modified sections: Repository Constraints corrects the approved runtime from .NET 10 to .NET 9
- Added sections: none
- Removed sections: none
- Follow-up TODOs: none
-->

# IDEA Constitution

## Core Principles

### I. Discoverable Repository Context

`AGENTS.md`, `docs/architecture.md`, `docs/quality.md`, and relevant ADRs are
the starting context for every change. Durable decisions and domain terminology
MUST be recorded in repository documents rather than left only in chat history.
This keeps human and agent work reproducible across sessions.

### II. One Deterministic Verification Path

`./harness.cmd` is the canonical local verification interface on Windows, and
`pwsh ./scripts/harness.ps1 <action>` is its macOS/Linux equivalent. Setup,
lint, test, and CI commands MUST be declared in `.harness/harness.json`; new
tooling MUST extend this contract instead of creating an undocumented path.

### III. Behavior-First Vertical Slices

Behavior changes MUST be implemented through the smallest meaningful vertical
slice. Tests verify public behavior at agreed seams, go red before the minimal
implementation goes green, and remain independent of implementation details.
Focused checks run during development; the full harness runs before completion.

### IV. Explicit Boundaries and Decisions

Components MUST preserve the dependency direction recorded in
`docs/architecture.md`. A choice that constrains future structure, runtime,
data, security, or deployment MUST be captured as an ADR. Interfaces expose a
small, clear surface and avoid leaking harness implementation details into
application code.

### V. Minimal, Safe, Reviewable Change

Changes MUST be the smallest coherent solution to the approved requirement.
Credentials, local environment files, generated dependencies, and build output
MUST NOT be committed. A change is ready only when its documentation is current,
the relevant checks pass, and human approval gates have been met.

## Repository Constraints

The repository uses the approved .NET 9 ASP.NET Core MVC host, modular Domain /
Application / Infrastructure.Postgres boundaries, and PostgreSQL persistence
recorded in ADR 0002. Native version pins and reproducible commands live in the
solution files and `.harness/harness.json`. PostgreSQL schema changes are made
only through the explicit `./harness.cmd migrate` action; `bootstrap` and
`check` do not mutate database schema. Local work items live under `.scratch/`;
repository-scoped agent skills live under `.agents/skills`.

The schema command consumes `ConnectionStrings:KpiMigration`, while the Web
runtime consumes `ConnectionStrings:KpiRuntime` only under an explicit Postgres
profile. InMemory is permitted solely for the named development/test profile
and is never an implicit production fallback.

## Development Workflow

For material feature work, use the sequence: clarify intent, create a Spec Kit
specification, clarify ambiguity, create a technical plan and tasks, analyze
cross-artifact consistency, obtain human approval, implement in vertical slices,
review the diff, and run the harness. Use Matt Pocock skills for interviewing,
domain modeling, TDD, debugging, and review; use Spec Kit for specification
artifacts. Do not run two implementation orchestrators on the same feature.

## Governance

This constitution governs repository engineering decisions and supersedes lower
level workflow documents when they conflict. `AGENTS.md` remains the operational
entrypoint and MUST stay consistent with this constitution. Amendments require a
documented rationale, review of affected templates and workflow artifacts, and
a semantic version bump: MAJOR for incompatible principle changes, MINOR for a
new or materially expanded principle, PATCH for clarification only. Every plan
and review MUST check compliance before work is considered complete.

**Version**: 1.1.1 | **Ratified**: 2026-08-08 | **Last Amended**: 2026-08-11
