# Agent Prompt: Specify the Organization and Authorization Foundation

## Purpose

Use this prompt with the agent that owns the first `$speckit-specify` run for
the BSC–KPI reference program. This run creates one business specification. It
does not implement product code, produce a technical plan, or unlock either
target repository.

## Required repository state

- Repository: `Kpis-Thinh`.
- Active branch: `feature/bsc-kpi-reference-implementation`.
- Feature directory: `specs/002-organization-authorization`.
- `BSC-KPIs-API` and `BSC-KPIs` remain read-only.

If the active branch differs, stop and report the mismatch. Preserve every
unrelated working-tree change. Do not create another Git branch for this
Specify run.

## Read before writing

Read these sources in order and treat them as progressively more specific:

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

The lifecycle specification is the source of truth for approved product
decisions. The implementation plan supplies phase boundaries, not additional
business scope. Record any material contradiction between sources instead of
silently choosing one.

## Invoke Specify

Run `$speckit-specify` for exactly one feature and set:

```text
SPECIFY_FEATURE_DIRECTORY=specs/002-organization-authorization
```

Use the feature description below as the command input.

---

Create the Organization and Authorization Foundation for the BSC–KPI reference
system.

The organization must be established and approved before users can define an
operational strategic plan, Annual BSC plan, KPI plan, cascade, or evaluation.
The product is intended to support multiple companies, while the first release
operates one company and keeps the company boundary explicit for future use.

An authorized administrator must be able to define an effective-dated,
generic organization-unit hierarchy rather than a fixed department model. The
system must detect invalid parent relationships and cycles, explain the exact
problem, and prevent approval of an invalid hierarchy. The administrator must
also define positions, employees, reporting relationships, and effective-dated
position assignments. One employee may hold multiple positions, with exactly
one primary position whenever an active assignment requires a primary
position. A person's employment status and sign-in account status are governed
independently.

The completed structure becomes an approved Organization Structure Baseline.
Until that baseline is approved, downstream BSC–KPI planning and operation are
blocked with a clear explanation. Changes after approval create a traceable new
revision or effective-dated amendment and do not rewrite the baseline used by
existing governed plans.

Authorization uses a fixed catalog of atomic KPI Capabilities and separates
capabilities from role names. An authorized security administrator can create
Organization-specific Custom KPI Roles by selecting any capability combination.
Potentially dangerous or conflicting combinations are allowed at design time
with visible warnings. A role's capability bundle is immutable after use;
changing the bundle creates a new role version so existing assignments remain
explainable.

Role assignments are effective-dated and scoped to the Organization, an
Organization Unit subtree, assigned business responsibilities, or the actor's
own data. Viewing and acting on data require both the applicable capability and
data scope. Creating or editing a role does not grant its capabilities to the
editor. Privileged role assignment and role elevation require independent
approval. Runtime separation of duty prevents a person from approving their
own submission, elevation, exception, or other governed action even when a
custom role contains both maker and approver capabilities.

Approver selection follows governed organization data and may resolve a direct
manager, unit head, position holder, named person or group, or an actor with a
required capability and scope. The resolved approval route is snapshotted when
submitted so later organization changes do not rewrite history. Effective-dated
delegation may temporarily substitute an approver within an explicit scope and
period while preserving the original and delegated identities.

Authorized users must be able to understand role warnings, assignment impact,
approval decisions, delegation, and rejected self-elevation from the product
interface. Audit and approval timelines are visible only when the viewer's
capability and organization scope allow access. Every accepted or rejected
governed action preserves who acted, on whose behalf, when, within which scope,
the reason, and the affected revision.

The primary acceptance journey is: define the company structure, create units
and positions, register employees and reporting lines, approve a structure
baseline, create a custom role, assign it within a scope, attempt and reject
self-elevation, obtain independent approval, and confirm that the approved
actor can perform only the actions and view only the data allowed by their
capabilities and scope.

Include negative and boundary scenarios for hierarchy cycles, overlapping or
expired position assignments, missing primary positions, incomplete baselines,
conflicting role warnings, unauthorized access, out-of-scope access, expired
delegation, no eligible approver, and concurrent changes to a submitted or
approved revision.

This feature ends at the approved organization baseline and runtime
authorization foundation. Strategic plans, perspectives, strategy maps, KPI
definitions and versions, KPI plan items, targets, cascades, actual submissions,
scoring, dashboards, pilots, exports, and work in `BSC-KPIs-API` or `BSC-KPIs`
are outside this feature.

---

## Specification rules

- Describe user and business outcomes: WHAT is required and WHY it matters.
- Keep `spec.md` technology-agnostic. Architecture, framework, API, database,
  migration, and source-file choices belong to the later planning phase.
- Use the domain terms defined in `CONTEXT.md` consistently.
- Make every functional requirement independently testable.
- Define measurable, user-focused success criteria.
- State reasonable assumptions explicitly.
- Use at most three `[NEEDS CLARIFICATION: ...]` markers, only for decisions that
  materially change scope, security, or user experience and have no safe
  default in the approved sources.
- Create and validate
  `specs/002-organization-authorization/checklists/requirements.md` as required
  by `$speckit-specify`.

## Hard stop

After `spec.md` and its requirements checklist are complete:

1. Report the exact files created or modified.
2. Report checklist pass/fail totals and every remaining clarification.
3. Summarize assumptions separately from approved requirements.
4. Ask the product owner to review the specification.
5. Stop. Do not run `$speckit-clarify`, `$speckit-plan`, `$speckit-tasks`, or an
   implementation skill. Do not commit, push, edit application code, or change
   either target repository.

Completion means the product owner can review one self-contained specification
for the Organization and Authorization Foundation without implementation work
having started.
