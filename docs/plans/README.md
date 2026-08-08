# Execution plans

Create a plan here for changes that span multiple components, introduce a dependency or migration, or cannot be safely completed in one short edit.

Use a descriptive filename such as `2026-08-08-add-api-service.md` and keep it current while work is active.

## Plan template

```markdown
# Outcome

Describe the user-visible result and acceptance criteria.

## Context and constraints

List affected boundaries, relevant decisions, and risks.

## Steps

- [ ] Implement the smallest vertical slice.
- [ ] Add or update automated verification.
- [ ] Update durable documentation.
- [ ] Run `./harness.cmd check` (or `pwsh ./scripts/harness.ps1 check` on macOS/Linux).

## Evidence

Record commands run and outcomes.
```
