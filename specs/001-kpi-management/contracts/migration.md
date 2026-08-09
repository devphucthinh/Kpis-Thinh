# Migration command contract

**Feature**: `001-kpi-management`  
**Boundary**: repository harness → `Kpi.Migrator` → PostgreSQL

## Canonical invocation

Windows:

```powershell
.\harness.cmd bootstrap
.\harness.cmd migrate
```

macOS/Linux:

```bash
pwsh ./scripts/harness.ps1 bootstrap
pwsh ./scripts/harness.ps1 migrate
```

`migrate` is the only documented schema-writing command. It is deliberately
separate from `bootstrap`, `check`, and Web startup.

The command needs only `ConnectionStrings:KpiMigration`; the Web runtime
connection is intentionally not consulted by the migrator.

## Configuration

Non-secret settings are loaded from the normal .NET configuration hierarchy:

| Key | Required | Meaning |
|---|---:|---|
| `ConnectionStrings:KpiMigration` | yes | Privileged connection used only by the migrator. |
| `Kpi:DatabaseName` | yes | Declared local database; default `kpi_lab`. |
| `Kpi:TestDatabaseName` | yes | Declared integration database; default `kpi_lab_test`. |
| `Kpi:MigrationRole` | yes | Expected migration role name; default `kpi_migrator`. |

Environment-variable spelling uses the .NET double-underscore form, for
example `ConnectionStrings__KpiMigration`. Passwords and complete connection
strings must come from user secrets or the process environment and must never
be committed.

## Preconditions

- PostgreSQL is reachable and the target database already exists.
- The connection is authenticated as the migration role (or an explicitly
  configured local equivalent).
- The connected database name is exactly `Kpi:DatabaseName` or
  `Kpi:TestDatabaseName`; arbitrary database names are rejected before a
  transaction starts.
- `bootstrap` has completed so the pinned SDK, lockfiles, and migrator build
  are available.

The command does not create databases or roles. Those one-time administrator
operations remain explicit pgAdmin/PostgreSQL setup steps.

## Observable behavior

1. Load and validate configuration without printing secrets.
2. Open the migration connection and validate its database name.
3. Create `kpi_schema_migrations` if absent.
4. Read applied IDs/checksums in manifest order.
5. Apply each missing SQL slice in order and insert its ID/checksum in the same
   transaction.
6. Skip an already applied slice only when its checksum matches.
7. Commit once; on any SQL/checksum/cancellation failure, roll back the whole
   invocation and exit non-zero.
8. Print a concise summary containing target database, applied IDs, skipped IDs,
   and elapsed time. Never print a password or full connection string.

## Failure contract

| Condition | Stable outcome |
|---|---|
| Missing migration connection | Non-zero exit; `MIGRATION_CONFIGURATION_MISSING`. |
| Target database not in local/test allow-list | Non-zero exit; `MIGRATION_TARGET_NOT_ALLOWED`; no transaction mutation. |
| Applied ID checksum differs | Non-zero exit; `MIGRATION_CHECKSUM_MISMATCH`; no commit. |
| SQL/permission/connectivity failure | Non-zero exit; `MIGRATION_APPLY_FAILED`; transaction rolled back. |
| All entries already applied | Zero exit; no product-table mutation; all entries reported skipped. |

## Verification in pgAdmin4

After a successful command, connect to the exact target database and run:

```sql
select id, checksum, applied_at
from public.kpi_schema_migrations
order by id;

select table_name
from information_schema.tables
where table_schema = 'public'
  and table_name in (
    'organizations', 'actors', 'kpi_definitions', 'kpi_versions',
    'kpi_periods', 'kpi_period_activations', 'kpi_period_amendments',
    'kpi_evaluations', 'audit_records'
  )
order by table_name;
```

The six manifest IDs must appear once each after the complete MVP schema is
implemented. The command is idempotent, so running it again should report the
same six IDs as skipped.

The console summary reports only the target database, migration IDs, and
elapsed time; it never prints a connection string or password.

## Explicit non-goals

- No schema migration on Web process startup.
- No destructive drop/recreate for `kpi_lab`.
- No down migrations.
- No demo/company/persona seed rows inside schema migrations.
- No runtime-role use for schema changes.
