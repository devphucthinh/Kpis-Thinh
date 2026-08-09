# Quickstart Validation Guide

**Feature**: `001-kpi-management`  
**Purpose**: Define the future runnable validation path after implementation. Until the solution exists, use this document as the expected end-to-end evidence rather than attempting ad-hoc setup commands.

## Prerequisites

1. Work from `main`; repository branch policy rejects any branch name containing `codex`.
2. Install the SDK version pinned in `global.json` once implementation adds it.
3. Have a local PostgreSQL 18.x instance available.
4. Configure only local/user-secret or environment-variable database credentials:
   - a schema-migration credential;
   - a limited runtime credential;
   - a separate test connection targeting exactly `kpi_lab_test`.
5. Do not commit `.env`, passwords, tokens, keys, generated dependencies, database dumps, or browser binaries.

## Canonical Commands

The repository harness is the only setup and verification contract:

```powershell
.\harness.cmd bootstrap
.\harness.cmd status
.\harness.cmd check
```

After implementation, `bootstrap` performs the one-time reviewed package-lock initialization during scaffolding, then uses locked dependency restoration on recurring runs; it also performs only explicitly configured safe local/test schema setup and prepares the browser test runtime. `check` runs repository policy, locked bootstrap, formatting/static checks, and unit/application/integration/browser tests through the same configuration used by CI.

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
6. As **KPI Period Planner**, create a matching-cadence Period Plan, select the exact eligible Version, and submit it. As **KPI Period Approver**, approve it. Confirm dates/selections are frozen and same-person approval is rejected.
7. Advance the controlled clock or reconciliation boundary. Confirm Scheduled becomes Active once and later Closed once; repeat reconciliation to confirm no duplicate Audit Records.
8. As **KPI Evaluator**, enter official values that produce 25. Reload and confirm exact source, ordered inputs, outcome and Current KPI Evaluation history.
9. Correct `revenue` to produce 30, supplying a reason. Confirm the original attempt remains, changed inputs/result are shown, and 30 is Current.
10. Cause a later Failure such as division by zero. Confirm it remains history and does not replace the Current successful result.
11. As **KPI Administrator**, inspect Audit history for create/review/publish/period/evaluation/correction. Confirm monitoring does not expose editing actions.
12. Archive and restore a Definition with history. Separately hard-delete an unused never-submitted Draft and confirm its Audit tombstone remains.

## Expected Safety Evidence

- Test Run never persists an Evaluation or changes Current.
- A stale concurrency token never overwrites a newer Draft/Plan.
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
