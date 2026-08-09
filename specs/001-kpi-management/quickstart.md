# Quickstart Validation Guide

**Feature**: `001-kpi-management`  
**Purpose**: Run and verify the implemented KPI Management prototype through
the repository harness and the explicit migration boundary.

## Prerequisites

1. Work from `main`; repository branch policy rejects any branch name containing `codex`.
2. Install the SDK version pinned in `global.json`.
3. Have a local PostgreSQL 18.x instance available.
4. Create the local databases (`kpi_lab` and, for integration tests,
   `kpi_lab_test`) and configure only local/user-secret or environment-variable
   database credentials:
   - `ConnectionStrings:KpiMigration` for the schema-migration credential;
   - `ConnectionStrings:KpiRuntime` for the limited application credential;
   - a separate test connection targeting exactly `kpi_lab_test` when the
     opt-in PostgreSQL test profile is enabled.
5. Do not commit `.env`, passwords, tokens, keys, generated dependencies, database dumps, or browser binaries.

## Canonical Commands

The repository harness is the only setup and verification contract:

```powershell
.\harness.cmd bootstrap
.\harness.cmd migrate
.\harness.cmd status
.\harness.cmd check
```

`bootstrap` uses locked dependency restoration and prepares the browser test
runtime but does not write PostgreSQL. `migrate` is the explicit, idempotent
schema action: it validates the target allow-list, applies the six forward-only
manifest slices through `Kpi.Migrator`, and records the checksum ledger. `check`
runs repository policy, locked bootstrap, formatting/static checks, and all test
projects through the same configuration used by CI; it does not require
migration credentials unless the PostgreSQL integration profile is deliberately
enabled.

For a PowerShell session, set the migration connection without writing it to a
file:

```powershell
$env:ConnectionStrings__KpiMigration = 'Host=localhost;Port=5432;Database=kpi_lab;Username=kpi_migrator;Password=<local-secret>'
.\harness.cmd migrate
```

For durable Web persistence, also set
`ConnectionStrings__KpiRuntime` and `Kpi__PersistenceProfile=Postgres` before
launching. The checked-in Development profile is `InMemoryTest`, which is safe
for UI exploration and never runs migrations.

Then open pgAdmin4 on `kpi_lab` and verify `kpi_schema_migrations` plus the
product tables using the queries in [migration.md](contracts/migration.md).
Run [run-kpi.bat](../../run-kpi.bat) only after the migration succeeds.

To open the local application after bootstrap, the integration guide created during implementation will supply the exact documented launch URL and command. This command is for running the interactive app; it does not replace `harness.cmd check` as verification.

## Demonstration Data

In Development only, select these seeded personas:

- Nguyễn An — KPI Creator
- Trần Bình — KPI Policy Approver
- Lê Chi — KPI Period Planner
- Phạm Dũng — KPI Period Approver
- Hoàng Giang — KPI Evaluator
- Đỗ Hà — KPI Administrator

An idempotent Development composition seeder, outside schema migrations, creates the company and a `REVENUE_ACHIEVEMENT` example KPI. Production never receives demo data, and persona switching must fail outside Development.

## Principal Validation Journey

1. As **KPI Creator**, create `REVENUE_ACHIEVEMENT` or a separate KPI Definition with an immutable code, name and description.
2. Create a Draft KPI Version with Decimal `revenue`/`target` and Boolean `active` Formula Variables. Give optional variables compatible defaults where appropriate.
3. Enter and validate:

   ```text
   IF(revenue > target AND active, ROUND(revenue / target * 100, 2), 0)
   ```

   Confirm syntax guidance, diagnostics/source spans when deliberately invalid, and generated AST preview.
4. Run a Formula Test Run with manual inputs. Confirm an outcome is visible and no official Evaluation/history row is added after reload.
5. Submit the Version. As **KPI Policy Approver**, approve it with a comment; publish it with an effective date. Confirm a creator cannot self-approve or edit submitted content.
6. As **KPI Period Planner**, create a matching-cadence Period Plan, select the exact eligible Version, and submit it. As a separate **KPI Period Approver**, reject it with a comment. Confirm it is read-only in Rejected; return as the Planner to Draft, revise, and resubmit while rejection evidence remains visible.
7. As the separate **KPI Period Approver**, approve the resubmitted Period. Confirm dates/selections are frozen and same-person approval is rejected without state or Audit mutation.
8. While the Period is Scheduled, propose a reasoned Amendment with a complete candidate interval/selection snapshot. Approve it as the separate Period Approver. Confirm the original approved plan remains unchanged, a new immutable effective revision is visible, and an Active/Closed/Cancelled Period rejects a new Amendment.
9. Advance the controlled clock or reconciliation boundary. Confirm Scheduled becomes Active once using the latest approved effective revision and later becomes Closed once; repeat reconciliation to confirm no duplicate transitions or Audit Records.
10. As **KPI Evaluator**, enter official values that produce 25. Reload and confirm exact source, ordered inputs, outcome and Current KPI Evaluation history.
11. Correct `revenue` to produce 30, supplying a reason. Confirm the original attempt remains, changed inputs/result are shown, and 30 is Current.
12. Cause a later Failure such as division by zero. Confirm it remains history and does not replace the Current successful result.
13. As **KPI Administrator**, inspect Audit history for create/review/publish/period/amendment/evaluation/correction. Confirm monitoring does not expose editing actions.
14. Archive and restore a Definition with history. Separately hard-delete an unused never-submitted Draft and confirm its Audit tombstone remains.

## Expected Safety Evidence

- Test Run never persists an Evaluation or changes Current.
- A stale concurrency token never overwrites a newer Draft/Plan.
- A wrong capability or self-conflicting decision changes neither governed state nor Audit history.
- A stale Amendment base revision cannot replace a newer approved effective revision.
- An effective range or Period overlap is rejected before it creates an illegal schedule.
- A Formula Failure has a stable code and never fabricates a Null success.
- A submitted AST cannot replace the server-generated Formula AST.
- Runtime credentials cannot update or delete an Audit Record.

## Troubleshooting Boundaries

- If `harness.cmd status` cannot find the pinned SDK or PostgreSQL connection, fix only the documented local prerequisite; do not add a second setup script.
- If a test needs destructive database setup, confirm its configured database name is exactly `kpi_lab_test` before dropping/recreating it.
- If a persona selector appears outside Development, treat startup failure as expected safety behavior and correct configuration rather than bypassing it.
- If a Formula/version snapshot cannot be read because its schema version is unknown, surface a safe diagnostic; do not reinterpret it with a newer formula engine.

## References

- [Behavior specification](spec.md)
- [Technical plan](plan.md)
- [Data model](data-model.md)
- [Formula contract](contracts/formula.md)
- [Application operations](contracts/application-operations.md)
- [HTTP delivery contract](contracts/http-api.md)
