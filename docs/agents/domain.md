# Domain docs

This is a single-context repository. Before work that needs domain knowledge, read the relevant material in this order:

1. `CONTEXT.md` at the repository root, when it exists.
2. ADRs in `docs/adr/` that affect the area being changed.

Absence of either location is normal in a new project; proceed without creating placeholder domain documents. `$domain-modeling`, reached through `$grill-with-docs` or `$improve-codebase-architecture`, creates or updates domain terms and ADRs when a real decision is resolved.

Use the vocabulary defined in `CONTEXT.md` in feature names, issues, tests, and code. Surface any conflict with an ADR explicitly instead of silently bypassing the decision.
