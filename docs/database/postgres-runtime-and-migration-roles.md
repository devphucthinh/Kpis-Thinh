# PostgreSQL local roles

The prototype accepts a non-secret `ConnectionStrings:Kpi` value from user
secrets or the environment. It never stores credentials in the repository.

- `kpi_migrator` owns schema creation and forward-only migration application.
- `kpi_runtime` owns only the application DML needed by the feature and must not
  update, delete, truncate, or alter `audit_records`.
- Destructive integration setup is allowed only against `kpi_lab_test`; the
  configuration validator rejects other targets.
- Development seed data is composition-root behavior, not migration data, and
  is disabled outside the Development environment.

For a local machine, set the connection string through .NET user-secrets or an
environment variable before using the PostgreSQL adapter. Do not add a
password, `.env` file, dump, or generated migration output to Git.
