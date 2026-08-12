# Prompt: Post-Plan Gate Review for Organization and Authorization

Copy the prompt below into an independent reviewing agent while its working
directory is the root of `Kpis-Thinh`.

---

You are the independent post-plan gate reviewer for feature
`002-organization-authorization` in repository `Kpis-Thinh`.

Review the committed HEAD of branch
`feature/bsc-kpi-reference-implementation` and decide whether the synchronized
Specification Kit plan is ready for `$speckit-tasks`. Review only. Do not edit
files, generate tasks, implement code, create migrations, commit, push, merge,
or modify `BSC-KPIs-API` or `BSC-KPIs`.

Treat repository artifacts as authoritative. Chat history is not evidence.

## Read in this order

1. `AGENTS.md`
2. `README.md`
3. `CONTEXT.md`
4. `docs/architecture.md`
5. `docs/quality.md`
6. `.specify/memory/constitution.md`
7. `docs/porting/bsc-kpis/implementation-agent-prompt.md`
8. `docs/plans/2026-08-11-bsc-kpi-reference-first-delivery.md`
9. `docs/decisions/0002-kpi-application-stack.md`
10. `docs/decisions/0003-capability-scope-authorization-and-effective-baselines.md`
11. `docs/superpowers/specs/2026-08-12-organization-kpi-workspace-design.md`
12. `specs/002-organization-authorization/spec.md`
13. `specs/002-organization-authorization/checklists/requirements.md`
14. `specs/002-organization-authorization/plan.md`
15. `specs/002-organization-authorization/research.md`
16. `specs/002-organization-authorization/data-model.md`
17. Every file under `specs/002-organization-authorization/contracts/`
18. `specs/002-organization-authorization/quickstart.md`
19. Relevant files under `src/`, `tests/`, `.harness/`, `global.json`,
    `Directory.Build.props`, and `Directory.Packages.props` only where needed
    to verify feasibility or a claimed repository convention.

## Review obligations

Build a traceability matrix for every `FR-001` through `FR-050` and `SC-001`
through `SC-016`. Each row must cite the exact Plan, Research, Data Model,
Contract, Quickstart, or future-feature ownership evidence and be marked
`Covered`, `Partial`, `Contradicted`, or `Missing`.

Audit these high-risk decisions explicitly:

1. Approval Route Versions follow `Draft -> PendingApproval -> Approved ->
   Active`, or terminate as `Rejected/Retired`; validation is not approval.
2. Every route version is reviewed independently. Its creator/editor cannot
   approve or activate it, even when a Custom Role contains all capabilities.
3. `OrganizationUnitHead` identifies Unit plus explicit Employee and validates
   eligibility against the applicable approved baseline.
4. `DirectManager` uses artifact Position context; primary Position fallback
   is permitted only when Position context is absent, and the source is frozen.
5. `NamedGroup` uses an Organization-scoped internal Approval Group with
   effective-dated membership and freezes the resolved member/candidate set.
6. One activation slot per Organization and artifact type makes replacement
   activation plus prior retirement atomic. Direct retirement of the active
   route is blocked.
7. OpenAPI operations, typed selector schemas, lifecycle statuses, concurrency
   tokens, immutable review/snapshot evidence, and stable 403/404/409/422
   responses agree with the spec and data model.
8. Feature 002 implements only the approved-baseline, capability/scope-filtered
   Organization tree, Unit-expand/Position-select behavior, URL restoration,
   baseline-applicability context, Razor shell, keyboard behavior, and
   390-pixel drawer.
9. Real one-edge KPI neighborhood data, assignments, the three weight kinds,
   Target, Actual, Variance, score, KPI Effective Segments, and whole-period
   results remain assigned to their named Planning/Cascade/Actual/Evaluation
   owners. No fixture or frontend calculation may satisfy feature-002
   acceptance.
10. `EffectiveSegmentContract` is a published consumer contract without an
    official result and remains distinct from `BaselineApplicabilitySegment`.
11. The plan remains .NET 9/SDK `9.0.315`, MVC/Razor-first, PostgreSQL-backed,
    and consistent with the migrator/runtime connection split and canonical
    harness.
12. `TARGET LOCK` remains active: both production repositories are read-only
    until the reference UI/UX, backend, API, database, restart, and audit gate
    is explicitly approved.
13. FR-043 has one deterministic in-process Planning/Foundation contract: the
    impact remains immutable, resolution is a separate one-per-impact fact,
    status is derived, exact retry is idempotent, conflicting/cross-Organization
    evidence is rejected, and Planning approval + resolution + audit share one
    unit of work. OpenAPI exposes read evidence but no bypassing resolve write.
14. SC-002 and SC-008 define human first-attempt cohorts, standardized
    orientation/assistance rules, numerator, denominator, minimum sample sizes,
    evidence fields, and the exact `>= 0.90` pass calculation. Automated tests
    cannot substitute for human outcomes.
15. Provisioning creates two distinct temporary Bootstrap Principals with fixed,
    non-delegable grants; the first baseline contains structure/workforce only;
    post-baseline governed Role Assignments replace both duties; one immutable
    atomic handoff expires both principals only after both replacements exist.
16. Break-glass recovery is time-bounded, approved by two distinct eligible
    Platform Security Administrators, replaces only the unavailable principal,
    excludes either Bootstrap Principal, and leaves authority unchanged for
    partial, duplicate, rejected, expired, or stale attempts.
17. Authorization is evaluated from current committed facts for every governed
    action; no cross-action decision cache exists. SC-014 proves the next action
    observes change and the 50 ms p95 threshold remains release-blocking.
18. SC-016 uses exactly 1,000 Employees and 200 Organization Units for the
    release-blocking validation/read envelope; tree/admin pages return at most
    200 nodes and satisfy the declared p95 threshold.

Check for stale alternatives, ambiguous state transitions, missing aggregate
owners, fake feature-002 acceptance claims, cross-Organization leakage,
unprotected concurrency, and any implementation choice an agent would be
forced to invent.

## Evidence and finding rules

- Cite repository-relative `file:line` evidence for every finding.
- Classify findings as `P0 Blocker`, `P1 High`, `P2 Medium`, or `P3 Low`.
- Tie each finding to an `FR-*`, `SC-*`, constitution principle, architecture
  rule, or approved decision.
- Explain the concrete implementation/porting risk and the smallest artifact
  correction.
- Do not report a later feature's explicitly assigned implementation as a
  feature-002 defect.
- Do report any contract, invariant, state, field, owner, or test seam missing
  from the current planning artifacts.
- Do not treat a green harness as proof of unimplemented future behavior.

## Required output

Return one Markdown document with exactly these sections:

1. `Verdict` — exactly one of `APPROVE`, `APPROVE WITH CHANGES`, or `BLOCK`,
   followed by a two-sentence rationale.
2. `Findings` — actionable findings ordered by severity; write `None` when
   there are no findings.
3. `Requirement Traceability` — every `FR-001..FR-050` and
   `SC-001..SC-016`.
4. `Approval Route Decision Traceability` — one row for high-risk decisions
   1-7 above across Spec, Plan, Data Model, OpenAPI, UI/Quickstart, and Tests.
5. `Organization KPI Workspace Boundary` — one row for decisions 8-10 with
   current owner, published contract, later owner, and status.
6. `Impact Resolution and Quantitative Evidence` — trace decisions 13-14
   through Spec, Plan, Data Model, contracts, Quickstart, and Tests.
7. `Constitution and Architecture Check` — pass/fail with evidence for every
   relevant rule.
8. `Open Questions` — only decisions impossible to derive from repository
   evidence; otherwise `None`.
9. `Recommended Next Step` — choose exactly one: correct the listed planning
   artifacts and rerun this review; return to `$speckit-clarify`; or proceed to
   `$speckit-tasks`.

Completion criterion: all requirements, high-risk decisions, feature
boundaries, constitution gates, and findings are accounted for with repository
evidence, and the recommended next step follows from the verdict.

---
