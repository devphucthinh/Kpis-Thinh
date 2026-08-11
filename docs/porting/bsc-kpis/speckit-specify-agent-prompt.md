# Primary Agent Prompt: Run Speckit Specify from an Approved Input Package

## Role boundary

The primary repository agent owns the first `$speckit-specify` run. An external
requirements bot owns the interview and supplies a product-owner-approved
`SPECKIT_INPUT_PACKAGE`. The external bot does not run Specify or modify this
repository.

Do not start this workflow until the product owner has pasted the complete input
package into the current task.

## Required repository state

- Repository: `Kpis-Thinh`.
- Active branch: `feature/bsc-kpi-reference-implementation`.
- Feature directory: `specs/002-organization-authorization`.
- `BSC-KPIs-API` and `BSC-KPIs` remain read-only.

If the active branch differs, stop and report the mismatch. Preserve every
unrelated working-tree change. Use the active reference branch; this Specify run
does not create another Git branch.

## Read before execution

Read these sources in order:

1. `AGENTS.md`
2. `.specify/memory/constitution.md`
3. `README.md`
4. `docs/architecture.md`
5. `docs/quality.md`
6. `CONTEXT.md`
7. `docs/porting/bsc-kpis/kpi-and-period-lifecycle-spec.md`
8. `docs/plans/2026-08-11-bsc-kpi-reference-first-delivery.md`, especially
   Tasks 1 and 2
9. `docs/porting/bsc-kpis/implementation-agent-prompt.md`
10. The complete `SPECKIT_INPUT_PACKAGE` supplied in the current task

The lifecycle specification remains the source of truth for already approved
product decisions. The input package may make an unresolved feature detail more
specific, but it cannot silently reverse an approved program decision.

## Input gate

Before invoking `$speckit-specify`, verify that the input package contains:

- the exact `# SPECKIT_INPUT_PACKAGE` marker;
- the expected feature name, directory, and source branch;
- business purpose, actors, approved scope, journeys, negative scenarios,
  exclusions, assumptions, measurable outcomes, and raw Specify input;
- a `Source contradictions` section containing only `- None`; and
- a `Deferred or unresolved decisions` section containing only `- None`.

If a required section is missing, ask the product owner to return the package to
the external bot for completion. If the package contradicts an approved source,
show the conflicting statements and wait for a product-owner decision. Do not
repair, expand, or guess a rejected input package.

## Run Specify

After the input gate passes:

1. Check `.specify/extensions.yml` and process enabled hooks exactly as required
   by the local `$speckit-specify` skill.
2. Run `$speckit-specify` for exactly one feature.
3. Set:

   ```text
   SPECIFY_FEATURE_DIRECTORY=specs/002-organization-authorization
   ```

4. Use only the package's `Raw Specify input` as the natural-language command
   input. Use the other package sections and repository sources to validate
   coverage and consistency.
5. Resolve the active specification template and create:
   - `specs/002-organization-authorization/spec.md`
   - `specs/002-organization-authorization/checklists/requirements.md`
6. Persist the feature directory in `.specify/feature.json` as required by the
   local Specify workflow.
7. Validate and revise the specification for at most three iterations using the
   generated requirements checklist.
8. Process enabled post-Specify hooks before reporting completion.

## Specification rules

- Describe WHAT users need and WHY it matters.
- Keep `spec.md` technology-agnostic.
- Use the domain terms in `CONTEXT.md` consistently.
- Make each functional requirement independently testable.
- Use measurable, user-focused success criteria.
- Distinguish approved requirements from documented assumptions.
- Preserve the exact feature boundary; one Specify invocation creates one
  feature only.
- Use at most three `[NEEDS CLARIFICATION: ...]` markers and only when the
  approved package still contains a material ambiguity that cannot be resolved
  from repository sources.

## Review gate

After `spec.md` and its checklist are complete:

1. Report `SPECIFY_FEATURE_DIRECTORY` and `SPEC_FILE`.
2. Report checklist pass/fail totals and any clarification markers.
3. Summarize assumptions separately from approved requirements.
4. Ask the product owner to review the specification.
5. Stop at this review gate. The next workflow begins only after explicit
   product-owner approval.

At this gate, leave application code and both target repositories unchanged.
Do not continue to `$speckit-clarify`, `$speckit-plan`, `$speckit-tasks`, or
implementation. Do not commit or push the generated specification until the
product owner approves it.

## Completion criterion

This workflow is complete when the product owner can review one validated,
self-contained Organization and Authorization Foundation specification created
from the external bot's approved input package, with no implementation work
started.
