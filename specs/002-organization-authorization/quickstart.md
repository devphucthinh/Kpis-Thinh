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

- Organization/workspace/revision/baseline/applicability tables exist.
- Role, role-version, capability link, scoped assignment, policy, versioned
  route, delegation, impact, and extended audit tables exist.
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

- Domain tests reject complete cycle paths, invalid effective intervals,
  multiple primary Positions, non-expanding delegation violations, and invalid
  proportional allocation.
- Application matrix tests distinguish missing capability, out-of-scope,
  expired authority, disabled account, separation of duty, and unresolved
  approver.
- Baseline-gate matrix tests allow KPI Dictionary authoring before a baseline,
  deny every representative baseline-dependent operation, then allow those
  operations after the first baseline starts.
- API tests prove stable 400/403/404/409/422 Problem Details and do not reveal
  cross-Organization resources; route-definition and role-version stale heads
  return stable 409 responses.
- PostgreSQL tests prove approved baselines, role versions, assignments, route
  snapshots, delegations, impacts, and Audit Records survive a fresh DbContext
  and Web restart.
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

Use distinct Development personas backed by distinct Employee/account
identities.

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
5. Correct the structure, validate, submit with an effective start and reason.
6. Attempt approval as submitter. Confirm HTTP/UI denial
   `authorization.separation-of-duty` and an Audit Record.
7. Approve as a different eligible actor. Confirm one immutable baseline and
   its route/timeline evidence.
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

### C. Route and delegation

1. Create a route draft with Direct Manager primary selector, Position Holder
   fallback, required capability, and scope relation; validate and activate it.
2. Create a new version using `If-Match`, then retry from the stale token and
   confirm HTTP 409 without an implicit route branch.
3. Submit an artifact through the active route and inspect the frozen
   baseline/selector evidence.
4. Change the live manager after submission. Confirm the stored route is
   unchanged.
5. Create a time/scope-limited delegation and decide as the delegate.
6. Confirm both original and acting identities appear in the timeline.
7. Retry outside the interval or scope; confirm denial and no stage skip.

### D. Mid-period impact and weight preview

1. Approve a replacement baseline effective inside an open KPI period.
2. Confirm the prior baseline applies before the boundary, the new baseline
   applies after it, and the impact is unresolved/requires re-cascade.
3. Request the proportional preview with old weights `50`, `20`, `30`, one new
   fixed weight `20`, and precision `0`.
4. Confirm ordered final weights `40`, `16`, `24`, `20`, exact total `100`, and
   deterministic repeat output.
5. Run a case with fractional remainders. Confirm residual recipients follow
   largest remainder, then prior order, then stable assignment ID.
6. Inspect the Effective Segment contract and confirm it identifies baseline,
   downstream plan revision, weight snapshot, and Aggregation Policy version
   without claiming an official KPI result. Record the later Planning/Evaluation
   acceptance obligations separately.

## 6. Prove restart persistence

1. Record IDs/hashes for the approved baseline and applicability chain, role
   versions, route versions/snapshot, assignment, delegation, impact, and
   representative Audit Records.
2. Stop the Web process normally.
3. Start the same `Thinh-KPI-TEST` profile again.
4. Query the records through UI/API and compare IDs, revision/hash, effective
   ranges, decisions, reasons, scopes, and timeline ordering.

Expected: all governed history is unchanged and current authorization reflects
the requested effective instant, not the restart time alone.

## 7. Human acceptance gate

The product owner must review:

- desktop and 390-pixel organization tree/edit/validation;
- Microsoft 365 Admin Center-style business-task role editor;
- privilege/scope preview and independent approval flow;
- delegation labels and complete explanation timeline;
- mid-period impact and deterministic weight preview;
- keyboard operation, focus, warnings, and non-color error evidence;
- PostgreSQL restart proof.

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
