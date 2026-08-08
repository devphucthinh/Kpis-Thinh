# Architecture

## Current state

This repository currently contains the engineering harness only; no application runtime or deployable component has been selected.

## Boundaries

- `.harness/` is declarative configuration for reproducible setup and checks.
- `scripts/` contains thin, deterministic entrypoints used locally and in CI.
- `docs/` contains durable context, decisions, quality policy, and execution plans.
- Application code will live outside these harness directories and must expose its lifecycle through `.harness/harness.json`.

## Dependency direction

The harness may invoke application tooling. Application code must not depend on harness implementation details. CI and contributors both invoke the same harness entrypoint so there is one verification path.

## Updating this document

When an application stack is introduced, replace this placeholder with:

- a component and data-flow map;
- public interfaces and ownership boundaries;
- dependency direction and forbidden dependencies;
- runtime, persistence, and deployment topology;
- links to the decisions that explain non-obvious constraints.
