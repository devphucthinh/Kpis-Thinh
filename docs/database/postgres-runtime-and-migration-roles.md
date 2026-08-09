# PostgreSQL local roles

The prototype accepts two non-secret connection values from user secrets or the
environment: `ConnectionStrings:KpiRuntime` for application reads/writes and
`ConnectionStrings:KpiMigration` for the explicit migrator. It never stores
credentials in the repository.

- `kpi_migrator` owns schema creation and forward-only migration application.
- `kpi_runtime` owns only the application DML needed by the feature and must not
  update, delete, truncate, or alter `audit_records`.
- Only `./harness.cmd migrate` may invoke the migration role. Web startup,
  `bootstrap`, and `check` are schema-read-only.
- Destructive integration setup is allowed only against `kpi_lab_test`; the
  configuration validator rejects other targets.
- Development seed data is composition-root behavior, not migration data, and
  is disabled outside the Development environment.

For a local machine, set both values through .NET user-secrets or environment
variables when running the durable Web profile. The migrator can run with only
the migration value. Use `Kpi:PersistenceProfile=Postgres` for the Web profile;
the checked-in Development configuration deliberately selects `InMemoryTest`
until a runtime connection is supplied. Do not add a password, `.env` file,
dump, or generated migration output to Git.
