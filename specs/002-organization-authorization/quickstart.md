# Quickstart Validation: Organization and Authorization Foundation

This guide is the post-implementation proof path for the feature. It does not
replace `harness.cmd`, does not silently create a database, and does not mark
human UI/UX review complete.

## Prerequisites

- Windows PowerShell and .NET SDK `9.0.315` selected by `global.json`.
- PostgreSQL local/test database permitted by the repository migrator policy.
- Separate non-secret process/user settings:
  - `ConnectionStrings__KpiMigration` for `Kpi.Migrator`.
  - `ConnectionStrings__KpiRuntime` for `Kpi.Web`.
- Chromium provisioned by the canonical bootstrap step.
- Active Git branch `feature/bsc-kpi-reference-implementation`.

Do not paste credentials into this file, `appsettings*.json`, launch settings,
test source, screenshots, or Git history.

## 1. Verify toolchain and repository

```powershell
dotnet --version
./harness.cmd status
./harness.cmd bootstrap
```

Expected:

- `dotnet --version` resolves to `9.0.315` under the repository policy.
- The harness identifies the permitted reference branch.
- Locked restore/build and pinned Playwright provisioning complete.
- No database schema is changed by `status` or `bootstrap`.

## 2. Apply the feature schema explicitly

After setting the migration connection in the current process or user
environment, run:

```powershell
./harness.cmd migrate
```

Expected migration evidence:

- Organization/workspace/revision/baseline/applicability plus bootstrap
  principal/recovery-decision/handoff tables exist.
- Role, role-version, capability link, scoped assignment, policy, Approval
  Group/effective membership, independently reviewed route,
  artifact-type activation slot, delegation, impact, and extended audit tables
  exist.
- `baseline_change_impacts` and `baseline_impact_resolutions` are append-only;
  a named unique constraint permits at most one resolution per impact.
- The named baseline effective-range exclusion constraint rejects overlap; the
  unique open tail plus serialized atomic predecessor-close/successor-insert
  path rejects gaps and concurrent branches.
- Audit update/delete protection remains active.
- Re-running `migrate` is idempotent through the migration ledger.

Web startup, `bootstrap`, and `check` must still perform no schema writes.

## 3. Run automated verification

Default deterministic verification:

```powershell
./harness.cmd check
```

Opt-in real PostgreSQL tests use the repository's existing environment contract:

```powershell
$env:KPI_POSTGRES_TESTS = "1"
./harness.cmd check
```

Expected focused behavior:

- Bootstrap tests prove atomic/idempotent provisioning, two distinct fixed
  non-delegable duties, first-baseline separation, two-person time-bounded
  recovery, one-principal replacement, and atomic two-assignment handoff.
- Domain tests reject complete cycle paths, invalid effective intervals,
  multiple primary Positions, non-expanding delegation violations, and invalid
  proportional allocation.
- Application matrix tests distinguish missing capability, out-of-scope,
  expired authority, disabled account, separation of duty, and unresolved
  approver. A new governed action after revoke, scope/baseline change, or
  handoff observes current committed facts; no cross-action cache is accepted.
- Baseline-gate matrix tests allow KPI Dictionary authoring before a baseline,
  deny every representative baseline-dependent operation, then allow those
  operations after the first baseline starts.
- API tests prove stable 400/403/404/409/422 Problem Details and do not reveal
  cross-Organization resources; route-definition and role-version stale heads
  return stable 409 responses.
- Route governance tests prove maker/editor review and activation denial,
  independently approved activation, stale activation-slot conflict, atomic
  replacement, blocked active-route retirement, typed selector validation, and
  frozen Position/Approval Group evidence.
- Workspace tests prove approved-baseline lazy tree reads, Unit expand-only and
  Position-select semantics, scope-filtered direct URLs, restorable URL state,
  and an honest no-KPI-provider state.
- Baseline-impact contract tests prove missing/unapproved/cross-Organization/
  baseline-mismatched amendment evidence cannot resolve an impact; exact
  approved evidence creates one resolution and Audit Record, exact retry is
  idempotent, conflicting/concurrent evidence cannot replace it, and simulated
  failure rolls back the consumer transaction marker, resolution, and Audit
  Record atomically. Later Planning must prove its real approval in that slot.
- PostgreSQL tests prove bootstrap provisioning/recovery/handoff, approved
  baselines, role versions, assignments, route snapshots, delegations,
  impacts/resolutions, and Audit Records survive a fresh DbContext and Web restart.
- Release-blocking load tests prove validation of 1,000 Employees/200 Units in
  at most 2 seconds, fresh authorization in at most 50 ms p95 after resource
  facts are loaded, and paged admin/tree reads returning at most 200 nodes in at
  most 500 ms p95 under the recorded local acceptance load.
- Playwright tests cover keyboard and 390-pixel journeys.

Clear only the process-scoped opt-in after the run if needed:

```powershell
Remove-Item Env:KPI_POSTGRES_TESTS -ErrorAction SilentlyContinue
```

## 4. Run Web with the approved test profile

Ensure the runtime connection is available, then start exactly the existing
profile requested for reference validation:

```powershell
dotnet run --project src/Kpi.Web/Kpi.Web.csproj --launch-profile Thinh-KPI-TEST
```

Open [http://localhost:5080](http://localhost:5080).

Expected startup behavior:

- Environment is Development.
- `Kpi:PersistenceProfile` is `Postgres`.
- Web uses only `ConnectionStrings:KpiRuntime` and does not apply migrations.
- Missing runtime connection fails explicitly; it never falls back to InMemory.

## 5. Validate the primary UI/API journey

Use explicit Development platform personas plus distinct Employee/account
identities. The development platform adapter must be selected explicitly and
must never be a fallback for production identity integration.

### 0. Bootstrap provision and recovery

1. As Platform Provisioner, create an Organization with distinct setup and
   independent-approval subject IDs. Confirm the request cannot provide its own
   capabilities and retrying the same idempotency key/payload returns the same
   Organization.
2. Try identical principal subjects and the same key with a changed payload.
   Confirm stable 422 and 409 outcomes with no partial Organization.
3. Before handoff, request recovery of one principal with reason/expiry. Confirm
   one approval, duplicate administrator approval, a Bootstrap Principal acting
   as platform approver, rejection, and expiry change no authority.
4. With a fresh request, approve as two distinct eligible Platform Security
   Administrators. Confirm exactly the unavailable duty is replaced, the other
   principal is unchanged, and immutable decision/audit evidence is visible.

### A. Structure and baseline

1. Create one root, at least two child Organization Units, Positions, four
   Employees, Position Assignments, and direct reporting relationships.
2. Try to move a unit beneath its descendant. Confirm submission is blocked and
   the complete cycle path is focused/displayed.
3. Try overlapping primary Position Assignments. Confirm both conflicting
   assignments are identified.
4. Query the baseline-eligibility matrix before approval. Confirm Dictionary
   authoring is allowed and every representative dependent operation is denied
   with `baseline_missing`.
5. As the active setup Bootstrap Principal, correct the structure, validate,
   and submit with an effective start and reason.
6. Attempt approval as that submitter. Confirm HTTP/UI denial
   `authorization.separation-of-duty` and an Audit Record.
7. Approve as the distinct independent-approval Bootstrap Principal. Confirm
   one immutable baseline, its route/timeline evidence, and zero Role
   Assignments in its frozen structure/workforce snapshot.
8. Query the matrix after the baseline start and confirm all representative
   dependent operations are allowed with the exact baseline ID.
9. Submit a successor whose start is after the tail start. Confirm approval
   atomically closes the predecessor at the exact successor start and opens the
   successor with no gap or overlap.
10. Attempt an out-of-order successor and two concurrent approvals from the same
    chain tail. Confirm deterministic 409/422 diagnostics and one surviving
    contiguous chain.

### B. Custom role and scoped assignment

1. Create a role containing both a maker and approver task. Confirm the UI shows
   a warning but permits creation after explicit acknowledgement.
2. Create a second version with a changed capability bundle. Confirm existing
   assignments still reference version 1.
3. From the same role-head token, submit two competing version requests. Confirm
   the first advances the head and the second returns
   `role.version.stale-head` with HTTP 409 rather than branching.
4. Propose a UnitSubtree assignment for another Employee. Confirm the preview
   explains risk/scope and requires independent approval when applicable.
5. Attempt self-elevation, then approve using an independent actor.
6. Execute one governed action inside the subtree and one outside it. Confirm
   the first succeeds and the second returns a safe scope explanation.
7. For initial replacement assignments, confirm the first effective assignment
   leaves both Bootstrap Principals active. Approve/effect the second duty's
   replacement and confirm one immutable handoff references the exact two
   assignments and expires both principals atomically.
8. Revoke or narrow one ordinary assignment, then execute a new action. Confirm
   the next action is denied from current facts; a prior allow decision is not reused.

### C. Route and delegation

1. Create an internal Approval Group, add effective-dated Employee memberships,
   and confirm overlapping membership for the same group/Employee is rejected.
2. Create a route draft with Direct Manager primary selector, Position Holder
   fallback, required capability, and scope relation. Add stages using an
   explicit Employee plus Unit for Organization Unit Head and the internal
   group for Named Group; confirm invalid field combinations fail validation.
3. Validate and submit the frozen route version. Attempt review and activation
   as its maker/editor and confirm `authorization.separation-of-duty`.
4. Approve as a different eligible actor with a reason. Confirm approval alone
   does not bypass the activation capability, then activate as an eligible
   non-maker using the route-head and activation-slot tokens.
5. Create a new version using `If-Match`, then retry from the stale token and
   confirm HTTP 409 without an implicit route branch.
6. Submit a multi-Position Employee artifact with Position context. Confirm
   Direct Manager uses that Position. Submit a context-free supported artifact
   and confirm the primary-Position fallback is labeled; invalid supplied
   Position context must fail rather than silently fall back.
7. Submit through Named Group, then change live membership. Confirm the stored
   membership/candidate snapshot remains unchanged.
8. Prepare and independently approve a replacement. Attempt to retire the only
   active route directly and confirm `approval.route.replacement-required`;
   activate the replacement and confirm prior retirement plus target activation
   commit atomically. A concurrent stale slot request returns HTTP 409 and
   leaves the original active route routable.
9. Change the live manager after submission. Confirm the stored route is
   unchanged.
10. Create a time/scope-limited delegation and decide as the delegate. Confirm
    both original and acting identities appear in the timeline; retry outside
    the interval or scope and confirm denial with no stage skip.

### D. Mid-period impact and weight preview

This section is a separate post-US5 contract slice. It is not part of the US1
MVP checkpoint; run it only after the baseline, workforce, authorization, and
approval-route prerequisites have passed.

1. Approve a replacement baseline effective inside an open KPI period.
2. Confirm the prior baseline applies before the boundary, the new baseline
   applies after it, and the immutable impact reads as `Detected`/requires
   re-cascade without an acknowledgement or resolve field to mutate.
3. Request the proportional preview with old weights `50`, `20`, `30`, one new
   fixed weight `20`, and precision `0`.
4. Confirm ordered final weights `40`, `16`, `24`, `20`, exact total `100`, and
   deterministic repeat output.
5. Run a case with fractional remainders. Confirm residual recipients follow
   largest remainder, then prior order, then stable assignment ID.
6. Through the automated Planning-consumer contract fixture, try to register
   missing, unapproved, cross-Organization, and wrong-baseline amendment
   evidence. Confirm every case is rejected without resolution or Audit Record.
7. Register exact independently approved amendment evidence. Confirm one
   immutable `BaselineImpactResolution`, derived `Resolved` status, and one
   Audit Record. Retry the exact reference and confirm the existing result is
   returned without another write; try a different/concurrent reference and
   confirm `baseline_impact.already_resolved`.
8. Simulate a failure before the shared unit-of-work commit. Confirm the test
   consumer marker, resolution, and Audit Record all roll back. Record that the
   later Planning feature must repeat this with its real amendment approval.
9. Inspect the Effective Segment contract and confirm it identifies baseline,
   downstream plan revision, weight snapshot, and Aggregation Policy version
   without claiming an official KPI result. Record the later Planning/Evaluation
   acceptance obligations separately.

### E. Organization KPI Workspace foundation

1. Open **Không gian KPI theo cơ cấu** at an instant with an applicable approved
   baseline. Confirm the page shows the exact Baseline Applicability Segment,
   not a fabricated KPI Effective Segment.
2. Expand Organization Unit nodes and confirm they never select or aggregate
   KPI data. Select an in-scope Position and confirm Position, baseline,
   effective time, branch, and search state are reflected in the URL.
3. Refresh, use back/forward, and open a copied URL. Confirm the same authorized
   Position is restored.
4. Attempt a direct URL for an out-of-scope Position. Confirm a safe forbidden/
   not-found experience without hidden ancestry or Employee leakage.
5. Confirm the foundation detail region states that KPI neighborhood providers
   are not yet available and renders no mock Target, Actual, Variance, score,
   weights, or KPI Effective Segment.
6. Repeat with keyboard only and a 390-pixel viewport. Confirm the **Chọn vị
   trí** drawer restores focus and every node's Unit-versus-Position semantics
   remain understandable without color.

### F. Release-blocking acceptance envelope

Use the versioned deterministic seed/load profile declared by the test harness;
record machine/OS, build/commit, database profile, warm-up, sample count,
concurrency, and p50/p95/max in the evidence ledger.

1. Validate a complete structure containing 1,000 Employees and 200
   Organization Units. Confirm the complete deterministic validation result is
   returned in at most 2 seconds.
2. Run governed next-action decisions after resource facts are loaded, including
   allow/deny and a committed revocation between actions. Confirm p95 is at most
   50 ms and the revoked action is denied.
3. Run paged administration and authorized lazy-tree queries whose response page
   contains at most 200 nodes. Confirm p95 is at most 500 ms and protected node
   counts/identities do not leak.

Failure of any threshold blocks release; it cannot be waived as informational.

## 6. Prove restart persistence

1. Record IDs/hashes for bootstrap principals/recovery decisions/handoff, the
   approved baseline and applicability chain, role
   versions, Approval Group/memberships, route reviews/activation slot/versions/
   snapshot, assignment, delegation, impact/resolution, and representative
   Audit Records.
2. Stop the Web process normally.
3. Start the same `Thinh-KPI-TEST` profile again.
4. Query the records through UI/API and compare IDs, revision/hash, effective
   ranges, decisions, reasons, scopes, and timeline ordering.

Expected: all governed history is unchanged and current authorization reflects
the requested effective instant and current committed authority, not the restart
time or a prior action's decision.

## 7. Human acceptance gate

### Quantitative first-attempt evidence protocol

Use one versioned task script, the same seeded Organization, and the same short
orientation for every participant. A **first attempt** starts when the task is
revealed after orientation and ends on success, an unrecoverable error, direct
task-specific help, developer intervention, or data repair. Facilitators may
observe and time the attempt but cannot tell the participant what control or
step to use. Playwright, facilitator rehearsal, repeat attempts, and attempts by
the implementer do not enter the human numerator or denominator.

An attempt is valid when the participant matches the assigned persona, the
approved seed/checklist passes before task reveal, and no external outage makes
the environment unavailable. Task-specific help, developer intervention, data
repair, product/API failure, navigation failure, and unrecoverable validation
count as failed valid attempts; they are never exclusions. Exclusion is limited
to a duplicate participant, failed pre-task seed check, or independently
verified external infrastructure outage and requires a recorded reason plus
product-owner disposition.

For **SC-002**:

- recruit at least 10 representative authorized Organization Administrators;
- ask each participant to complete the same organization-to-approved-baseline
  journey from the approved starting state;
- pass only when `successful first attempts / valid first attempts >= 0.90`
  (minimum passing observation: `9/10`).

For **SC-008**:

- recruit at least 20 representative participants: at least five Organization
  Administrators, five Security Administrators, five approvers, and five
  auditors;
- assign each participant the primary journey for that persona under the same
  orientation/assistance rule;
- report results per persona and overall; pass only when the overall ratio is
  at least `0.90` (minimum passing observation: `18/20`).

Record in `.scratch/bsc-kpi-reference/evidence.md`: build/commit, script
version, anonymized participant ID, persona, journey, attempt number, start/end,
success/failure, assistance/data-repair flags, failure reason, numerator,
denominator, ratio, observer, and product-owner disposition. Excluded attempts
remain listed with the exclusion reason and do not silently change the
denominator.

The product owner must review:

- platform provisioning, fixed bootstrap profiles, first-baseline separation,
  two-person recovery evidence, and atomic governed handoff;
- desktop and 390-pixel organization tree/edit/validation;
- Microsoft 365 Admin Center-style business-task role editor;
- privilege/scope preview and independent approval flow;
- delegation labels and complete explanation timeline;
- independent route review, atomic replacement, typed selector evidence, and
  Approval Group history;
- Organization KPI Workspace tree/Position navigation, URL restoration, honest
  provider boundary, and 390-pixel drawer;
- mid-period impact and deterministic weight preview;
- immutable approved-amendment impact resolution evidence and its negative,
  idempotent, conflict, rollback, and restart cases;
- SC-002 and SC-008 evidence ledgers meeting the declared cohorts and ratios;
- keyboard operation, focus, warnings, and non-color error evidence;
- PostgreSQL restart proof;
- all three release-blocking performance-envelope results with the declared
  load profile and p95 evidence.

Record approval in the reference evidence ledger. Do not edit
`BSC-KPIs-API` or `BSC-KPIs` until UI/UX, backend, API, authorization, database,
restart, and audit evidence are approved end to end.

## 8. Final repository evidence

```powershell
./harness.cmd status
./harness.cmd check
git diff --check
git status --short --branch
```

Expected: all harness checks pass, only intended feature artifacts/code are
present, target repositories remain unchanged, and no credential/build output
is tracked.
